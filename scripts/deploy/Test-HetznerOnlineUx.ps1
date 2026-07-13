<#
.SYNOPSIS
Runs bounded P4K online UX smoke scenarios against a deployed ChessOnlineServer.

.EXAMPLE
pwsh -File .\scripts\deploy\Test-HetznerOnlineUx.ps1 -Scenario all -BuildTool -NoSecretLog

.EXAMPLE
pwsh -File .\scripts\deploy\Test-HetznerOnlineUx.ps1 -Scenario resume -ProfileId "classic-six-side-3d-8x8x8-v0.1" -DryRun
#>
param(
    [string]$BaseUrl = "http://178.105.220.117",
    [string]$ProfileId = "asgard-convergence-3d-8x8x8-v0.1",
    [ValidateSet("play", "resume", "spectator", "lobby", "all")]
    [string]$Scenario = "all",
    [int]$TimeoutSeconds = 180,
    [switch]$NoSecretLog,
    [switch]$BuildTool,
    [switch]$SkipActionSubmit,
    [switch]$NegativeResume,
    [Alias("UniqueRunId")]
    [string]$RunId = "",
    [string]$LogDirectory = "",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$toolProject = Join-Path $root "tools\HetznerSignalRSmoke\HetznerSignalRSmoke.csproj"
$toolExe = Join-Path $root "tools\HetznerSignalRSmoke\bin\Release\net8.0\HetznerSignalRSmoke.exe"
$watchdogProject = Join-Path $root "tools\TestProcessWatchdog\TestProcessWatchdog.csproj"
$watchdogExe = Join-Path $root "tools\TestProcessWatchdog\bin\Release\net8.0\TestProcessWatchdog.exe"

if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    throw "BaseUrl cannot be empty."
}

$parsedBaseUrl = $null
if (-not [Uri]::TryCreate($BaseUrl, [UriKind]::Absolute, [ref]$parsedBaseUrl) -or
    $parsedBaseUrl.Scheme -notin @("http", "https")) {
    throw "BaseUrl must be an absolute HTTP or HTTPS URL."
}

if ($TimeoutSeconds -lt 10 -or $TimeoutSeconds -gt 900) {
    throw "TimeoutSeconds must be between 10 and 900."
}

if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = "p4k-online-ux-$((Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss'))-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
}

if ($RunId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$') {
    throw "RunId may contain only letters, digits, dot, underscore, and hyphen (maximum 128 characters)."
}

if ([string]::IsNullOrWhiteSpace($LogDirectory)) {
    $LogDirectory = Join-Path $root ".tmp\remote-ux-smoke"
}

$LogDirectory = [System.IO.Path]::GetFullPath($LogDirectory)

$BaseUrl = $BaseUrl.TrimEnd("/")
$stdout = Join-Path $LogDirectory "$RunId.stdout.$Scenario.log"
$stderr = Join-Path $LogDirectory "$RunId.stderr.$Scenario.log"

Write-Host "P4K Online UX smoke"
Write-Host "BaseUrl: $BaseUrl"
Write-Host "ProfileId: $ProfileId"
Write-Host "Scenario: $Scenario"
Write-Host "TimeoutSeconds: $TimeoutSeconds"
Write-Host "RunId: $RunId"
Write-Host "StdoutLog: $stdout"
Write-Host "StderrLog: $stderr"
Write-Host "NoSecretLog: $($NoSecretLog.IsPresent)"
Write-Host "SkipActionSubmit: $($SkipActionSubmit.IsPresent)"
Write-Host "NegativeResume: $($NegativeResume.IsPresent)"
Write-Host "Health:"
Write-Host "  $BaseUrl/healthz/live"
Write-Host "  $BaseUrl/healthz/ready"
Write-Host "  $BaseUrl/chess3d/diagnostics"

if ($DryRun) {
    Write-Host "DryRun set; no build, no network calls, and no smoke process started."
    exit 0
}

New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null

function Get-RedactedLogTail {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [int]$Tail = 100
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    $sensitivePattern = '(?i)access[_-]?token|refresh[_-]?token|authorization|bearer|password|private[_-]?key|connection[_-]?token'
    Get-Content -LiteralPath $Path -Tail $Tail | ForEach-Object {
        if ($_ -match $sensitivePattern) {
            "<redacted sensitive log line>"
        }
        else {
            $_ -replace '(?i)(https?://[^\s?]+)\?[^\s]+', '$1?<redacted-query>'
        }
    }
}

function Invoke-DotnetBuild {
    param([string]$Project)

    dotnet build $Project -c Release -m:1 -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed: $Project"
    }
}

if ($BuildTool -or -not (Test-Path -LiteralPath $toolExe)) {
    Invoke-DotnetBuild $toolProject
}

if (-not (Test-Path -LiteralPath $watchdogExe)) {
    Invoke-DotnetBuild $watchdogProject
}

$argParts = @(
    "--base-url", $BaseUrl,
    "--profile-id", $ProfileId,
    "--scenario", $Scenario,
    "--timeout", "$TimeoutSeconds",
    "--run-id", $RunId
)

if ($NoSecretLog) {
    $argParts += "--no-secret-log"
}

if ($SkipActionSubmit) {
    $argParts += "--skip-action-submit"
}

if ($NegativeResume) {
    $argParts += "--negative-resume"
}

$childArgs = $argParts -join " "

& $watchdogExe `
    --file $toolExe `
    --args $childArgs `
    --workdir $root `
    --timeout ($TimeoutSeconds + 20) `
    --stdout $stdout `
    --stderr $stderr

$exit = $LASTEXITCODE
Write-Host "Smoke stdout: $stdout"
Write-Host "Smoke stderr: $stderr"

if (Test-Path -LiteralPath $stdout) {
    Write-Host "---- stdout tail ----"
    Get-RedactedLogTail -Path $stdout
}

if ($exit -ne 0 -and (Test-Path -LiteralPath $stderr)) {
    Write-Host "---- stderr tail ----"
    Get-RedactedLogTail -Path $stderr
}

exit $exit
