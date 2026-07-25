[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$InputPath,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [ValidateSet("glb", "obj")][string]$TargetFormat = "glb",
    [string]$BlenderPath = "",
    [double]$Scale = 1.0,
    [switch]$SkipApplyTransforms,
    [switch]$SkipTriangulation,
    [int]$TimeoutSeconds = 300,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$input = Get-Item -LiteralPath $InputPath -Force -ErrorAction Stop
if ($input.PSIsContainer) { throw "InputPath must be a file." }
if (($input.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "InputPath must not be a symlink or reparse point."
}
if ($input.Extension.ToLowerInvariant() -notin @(".blend", ".fbx", ".obj", ".glb", ".gltf")) {
    throw "Unsupported Blender input format '$($input.Extension)'."
}
if (-not [double]::IsFinite($Scale) -or $Scale -le 0) { throw "Scale must be finite and positive." }
if ($TimeoutSeconds -lt 10 -or $TimeoutSeconds -gt 3600) { throw "TimeoutSeconds must be between 10 and 3600." }

if ([string]::IsNullOrWhiteSpace($BlenderPath)) {
    $command = Get-Command blender -ErrorAction SilentlyContinue
    if ($command) { $BlenderPath = $command.Source }
}

$result = [ordered]@{
    status = ""
    input = $input.Name
    output = [IO.Path]::GetFileName($OutputPath)
    targetFormat = $TargetFormat
    scale = $Scale
    applyTransforms = -not $SkipApplyTransforms
    triangulate = -not $SkipTriangulation
    blender = if ($BlenderPath) { [IO.Path]::GetFileName($BlenderPath) } else { $null }
}

if ([string]::IsNullOrWhiteSpace($BlenderPath) -or -not (Test-Path -LiteralPath $BlenderPath)) {
    if ($DryRun) {
        $result.status = "SKIPPED"
        $result.reason = "Blender executable was not found; no installation was attempted."
        $result | ConvertTo-Json
        exit 0
    }
    throw "Blender executable was not found. Supply -BlenderPath or install Blender explicitly."
}

$outputFull = [IO.Path]::GetFullPath($OutputPath)
if ([IO.Path]::GetExtension($outputFull).TrimStart('.').ToLowerInvariant() -ne $TargetFormat) {
    throw "OutputPath extension must match TargetFormat '$TargetFormat'."
}

$script = Join-Path $PSScriptRoot "blender\convert_model_asset.py"
$reportPath = "$outputFull.report.json"
$logRoot = Join-Path $repositoryRoot ".tmp\asset-conversion"
[void](New-Item -ItemType Directory -Path $logRoot -Force)
$jobId = [Guid]::NewGuid().ToString("N")
$stdout = Join-Path $logRoot "$jobId.stdout.log"
$stderr = Join-Path $logRoot "$jobId.stderr.log"

$blenderArguments = @(
    "--background",
    "--factory-startup",
    "--disable-autoexec",
    "--python", "`"$script`"",
    "--",
    "--input", "`"$($input.FullName)`"",
    "--output", "`"$outputFull`"",
    "--format", $TargetFormat,
    "--scale", $Scale.ToString([Globalization.CultureInfo]::InvariantCulture),
    "--apply-transforms", $(if ($SkipApplyTransforms) { "false" } else { "true" }),
    "--triangulate", $(if ($SkipTriangulation) { "false" } else { "true" }),
    "--report", "`"$reportPath`""
) -join " "

$result.commandLine = "blender --background --factory-startup --disable-autoexec --python convert_model_asset.py -- <redacted local paths>"
if ($DryRun) {
    $result.status = "DRY_RUN_READY"
    $result | ConvertTo-Json
    exit 0
}

$watchdog = Join-Path $repositoryRoot "tools\TestProcessWatchdog\bin\Release\net8.0\TestProcessWatchdog.exe"
if (-not (Test-Path -LiteralPath $watchdog)) {
    dotnet build (Join-Path $repositoryRoot "tools\TestProcessWatchdog\TestProcessWatchdog.csproj") -c Release | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Unable to build the bounded process watchdog." }
}

& $watchdog --file $BlenderPath --args $blenderArguments --workdir $repositoryRoot `
    --timeout $TimeoutSeconds --stdout $stdout --stderr $stderr
$exitCode = $LASTEXITCODE
if ($exitCode -eq 124) { throw "Blender conversion timed out after $TimeoutSeconds seconds. Logs: $stdout ; $stderr" }
if ($exitCode -ne 0) { throw "Blender conversion failed with exit code $exitCode. Logs: $stdout ; $stderr" }
if (-not (Test-Path -LiteralPath $outputFull)) { throw "Blender reported success but produced no output." }

$inputSha = (Get-FileHash -LiteralPath $input.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
$outputSha = (Get-FileHash -LiteralPath $outputFull -Algorithm SHA256).Hash.ToLowerInvariant()
$blenderReport = if (Test-Path -LiteralPath $reportPath) {
    Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
} else { $null }

$result.status = "PASS"
$result.inputSha256 = $inputSha
$result.outputSha256 = $outputSha
$result.blenderVersion = $blenderReport.blenderVersion
$result.meshCount = $blenderReport.meshCount
$result.triangleCount = $blenderReport.triangleCount
$result.materialCount = $blenderReport.materialCount
$result.textureCount = $blenderReport.textureCount
$result.warnings = @($blenderReport.warnings)
$result | ConvertTo-Json -Depth 5
