[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)][string]$InputPath,
    [Parameter(Mandatory = $true)][string]$SetId,
    [Parameter(Mandatory = $true)][string]$Role,
    [Parameter(Mandatory = $true)][ValidateSet("Chess2D", "Chess3D", "Rubik")][string]$App,
    [Parameter(Mandatory = $true)][string]$LicenseFile,
    [Parameter(Mandatory = $true)][string]$SourceMetadata,
    [string]$OutputRoot = "",
    [ValidateSet("obj", "glb")][string]$TargetFormat = "glb",
    [switch]$DryRun,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Token([string]$Value, [string]$Name) {
    if ($Value -notmatch '^[A-Za-z0-9._-]{1,128}$') {
        throw "$Name contains unsupported characters."
    }
}

function Resolve-LocalFile([string]$Path, [string]$Name) {
    if ($Path -match '^[A-Za-z][A-Za-z0-9+.-]*://') {
        throw "$Name must be a local file, not a URL."
    }
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer) { throw "$Name must be a file." }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Name must not be a symlink or reparse point."
    }
    return $item
}

function Assert-ContainedPath([string]$Candidate, [string]$Root, [string]$Name) {
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $candidateFull = [IO.Path]::GetFullPath($Candidate)
    if (-not $candidateFull.StartsWith($rootFull, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Name escapes the allowed root."
    }
    return $candidateFull
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repositoryRoot "assets\models"
}
$outputRootFull = [IO.Path]::GetFullPath($OutputRoot)
$input = Resolve-LocalFile $InputPath "InputPath"
$license = Resolve-LocalFile $LicenseFile "LicenseFile"
if ($license.Length -eq 0) { throw "LicenseFile is empty." }
if ([string]::IsNullOrWhiteSpace($SourceMetadata)) { throw "SourceMetadata is required." }
Assert-Token $SetId "SetId"
if ($Role -notmatch '^[A-Za-z][A-Za-z0-9.-]{1,127}$') { throw "Role is invalid." }

$extension = $input.Extension.TrimStart('.').ToLowerInvariant()
if ($extension -notin @("obj", "glb")) {
    throw "Unsupported input extension '$($input.Extension)'. Archives and authoring formats require an explicit conversion phase."
}
if ($TargetFormat -ne $extension) {
    throw "Import does not convert '$extension' to '$TargetFormat'. Run the approved conversion adapter first."
}

$setDestination = Assert-ContainedPath (Join-Path $outputRootFull $SetId) $outputRootFull "Set destination"
if ((Test-Path -LiteralPath $setDestination) -and -not $Force) {
    throw "Destination set '$SetId' already exists. Use -Force only after reviewing the replacement."
}

$stagingRoot = Join-Path $repositoryRoot ".tmp\asset-import"
$staging = Join-Path $stagingRoot ([Guid]::NewGuid().ToString("N"))
$stagedSet = Join-Path $staging $SetId
$assetDirectory = Join-Path $stagedSet "assets"
$backup = $null

try {
    [void](New-Item -ItemType Directory -Path $assetDirectory -Force)
    $runtimeName = "$Role.$extension"
    $stagedAsset = Join-Path $assetDirectory $runtimeName
    Copy-Item -LiteralPath $input.FullName -Destination $stagedAsset
    Copy-Item -LiteralPath $license.FullName -Destination (Join-Path $stagedSet "LICENSE.txt")

    $inputSha = (Get-FileHash -LiteralPath $input.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $stagedSha = (Get-FileHash -LiteralPath $stagedAsset -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($inputSha -ne $stagedSha) { throw "Staged SHA-256 does not match the source file." }

    $manifest = [ordered]@{
        format = "chess-model-assets"
        version = "2.0"
        setId = $SetId
        displayName = $SetId
        author = "Pending metadata review"
        license = [ordered]@{
            spdxId = "LicenseRef-Provided"
            status = "provided-pending-review"
            noticePath = "LICENSE.txt"
        }
        source = [ordered]@{
            provenance = $SourceMetadata
            sourceSha256 = $inputSha
        }
        supportedApps = @($App)
        units = "unit"
        coordinateSystem = "right-handed-y-up"
        defaultScale = 1.0
        assets = @(
            [ordered]@{
                assetId = "$SetId.$Role"
                role = $Role
                format = $extension
                path = "assets/$runtimeName"
                sha256 = $stagedSha
                scale = 1.0
            }
        )
    }
    $manifestPath = Join-Path $stagedSet "asset-manifest-v2.json"
    $manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM

    $parsed = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($parsed.format -ne "chess-model-assets" -or $parsed.version -ne "2.0") {
        throw "Generated manifest did not pass internal format validation."
    }
    if ([IO.Path]::IsPathRooted([string]$parsed.assets[0].path) -or
        ([string]$parsed.assets[0].path).Contains("..")) {
        throw "Generated manifest contains an unsafe path."
    }

    $validator = Join-Path $repositoryRoot "tools\ModelAssetValidator\bin\Release\net8.0\ModelAssetValidator.exe"
    if (-not (Test-Path -LiteralPath $validator)) {
        dotnet build (Join-Path $repositoryRoot "tools\ModelAssetValidator\ModelAssetValidator.csproj") `
            -c Release --nologo | Out-Host
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $validator)) {
            throw "Model asset validator build failed."
        }
    }
    $validationOutput = & $validator --manifest $manifestPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Model asset validation failed: $($validationOutput -join [Environment]::NewLine)"
    }

    $result = [ordered]@{
        setId = $SetId
        role = $Role
        sourceFormat = $extension
        inputBytes = $input.Length
        inputSha256 = $inputSha
        staging = ".tmp/asset-import/$([IO.Path]::GetFileName($staging))"
        destination = ([IO.Path]::GetRelativePath($repositoryRoot, $setDestination) -replace '\\', '/')
        dryRun = [bool]$DryRun
        validation = "PASS"
        status = if ($DryRun) { "DRY_RUN_VALID" } else { "PROMOTED" }
    }
    $result | ConvertTo-Json -Depth 4

    if ($DryRun) { return }
    if (-not $PSCmdlet.ShouldProcess($setDestination, "Promote validated model asset set")) { return }

    [void](New-Item -ItemType Directory -Path $outputRootFull -Force)
    if (Test-Path -LiteralPath $setDestination) {
        $backup = "$setDestination.backup.$([Guid]::NewGuid().ToString('N'))"
        [IO.Directory]::Move($setDestination, $backup)
    }
    try {
        [IO.Directory]::Move($stagedSet, $setDestination)
        if ($backup) { Remove-Item -LiteralPath $backup -Recurse -Force }
        $backup = $null
    } catch {
        if ($backup -and -not (Test-Path -LiteralPath $setDestination)) {
            [IO.Directory]::Move($backup, $setDestination)
            $backup = $null
        }
        throw
    }
} finally {
    if (Test-Path -LiteralPath $staging) {
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
}
