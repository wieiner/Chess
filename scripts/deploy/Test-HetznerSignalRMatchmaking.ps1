param(
    [string]$BaseUrl = "",
    [string]$ServerUrl = "",
    [string]$ProfileId = "asgard-convergence-3d-8x8x8-v0.1",
    [int]$TimeoutSeconds = 180,
    [switch]$DryRun,
    [switch]$NoSecretLog,
    [switch]$SkipActionSubmit,

    # Legacy tunnel mode remains supported for private loopback smoke.
    [string]$SshTarget = "",
    [string]$SshKeyPath = "",
    [string]$RemoteHost = "127.0.0.1",
    [int]$RemotePort = 5077,
    [int]$LocalPort = 15077,
    [string]$RulesetId = "",
    [switch]$SkipTunnel,
    [switch]$BuildSmokeTool
)

$ErrorActionPreference = "Stop"

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$toolProject = Join-Path $root "tools\HetznerSignalRSmoke\HetznerSignalRSmoke.csproj"
$toolExe = Join-Path $root "tools\HetznerSignalRSmoke\bin\Release\net8.0\HetznerSignalRSmoke.exe"
$watchdogProject = Join-Path $root "tools\TestProcessWatchdog\TestProcessWatchdog.csproj"
$watchdogExe = Join-Path $root "tools\TestProcessWatchdog\bin\Release\net8.0\TestProcessWatchdog.exe"
$logDir = Join-Path $root ".tmp\test-logs"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

if (-not [string]::IsNullOrWhiteSpace($RulesetId)) {
    Write-Warning "-RulesetId is deprecated for this wrapper. Use -ProfileId instead."
    if ($ProfileId -eq "asgard-convergence-3d-8x8x8-v0.1") {
        $ProfileId = $RulesetId
    }
}

function Invoke-DotnetBuild {
    param([string]$Project)
    dotnet build $Project -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed: $Project"
    }
}

function Resolve-BaseUrl {
    if (-not [string]::IsNullOrWhiteSpace($BaseUrl)) {
        return $BaseUrl.TrimEnd("/")
    }
    if (-not [string]::IsNullOrWhiteSpace($ServerUrl)) {
        return $ServerUrl.TrimEnd("/")
    }
    if (-not [string]::IsNullOrWhiteSpace($SshTarget) -and -not $SkipTunnel) {
        return "http://127.0.0.1:$LocalPort"
    }
    if (-not [string]::IsNullOrWhiteSpace($SshTarget) -and $SkipTunnel) {
        return "http://$($RemoteHost):$RemotePort"
    }

    Write-Warning "No -BaseUrl or -ServerUrl supplied. Defaulting to the current Hetzner HTTP endpoint; prefer passing -BaseUrl explicitly."
    return "http://178.105.220.117"
}

$resolvedBaseUrl = Resolve-BaseUrl
$hubUrl = "$resolvedBaseUrl/chess3d/relay"
$liveUrl = "$resolvedBaseUrl/healthz/live"
$readyUrl = "$resolvedBaseUrl/healthz/ready"
$diagnosticsUrl = "$resolvedBaseUrl/chess3d/diagnostics"

Write-Host "Hetzner SignalR matchmaking smoke"
Write-Host "BaseUrl: $resolvedBaseUrl"
Write-Host "HubUrl: $hubUrl"
Write-Host "ProfileId: $ProfileId"
Write-Host "TimeoutSeconds: $TimeoutSeconds"
Write-Host "SkipActionSubmit: $($SkipActionSubmit.IsPresent)"
Write-Host "NoSecretLog: $($NoSecretLog.IsPresent)"
Write-Host "Health:"
Write-Host "  $liveUrl"
Write-Host "  $readyUrl"
Write-Host "  $diagnosticsUrl"

if ($DryRun) {
    Write-Host "DryRun set; no network calls, no tunnel, and no smoke process started."
    exit 0
}

if ($BuildSmokeTool -or -not (Test-Path $toolExe)) {
    Invoke-DotnetBuild $toolProject
}
if (-not (Test-Path $watchdogExe)) {
    Invoke-DotnetBuild $watchdogProject
}

$tunnel = $null
try {
    if (-not [string]::IsNullOrWhiteSpace($SshTarget) -and -not $SkipTunnel -and
        [string]::IsNullOrWhiteSpace($BaseUrl) -and [string]::IsNullOrWhiteSpace($ServerUrl)) {
        if ([string]::IsNullOrWhiteSpace($SshKeyPath)) {
            throw "-SshKeyPath is required when using legacy SSH tunnel mode."
        }

        $sshArgs = @(
            "-i", $SshKeyPath,
            "-N",
            "-L", "127.0.0.1:$($LocalPort):$($RemoteHost):$($RemotePort)",
            $SshTarget
        )
        Write-Host "Starting SSH tunnel 127.0.0.1:$LocalPort -> ${RemoteHost}:$RemotePort"
        $tunnel = Start-Process -FilePath "ssh" -ArgumentList $sshArgs -PassThru -WindowStyle Hidden
        Start-Sleep -Seconds 2
        if ($tunnel.HasExited) {
            throw "SSH tunnel exited early with code $($tunnel.ExitCode)."
        }
    }

    $stdout = Join-Path $logDir "hetzner-signalr-smoke.stdout.log"
    $stderr = Join-Path $logDir "hetzner-signalr-smoke.stderr.log"
    $argParts = @(
        "--base-url", $resolvedBaseUrl,
        "--ruleset", $ProfileId,
        "--timeout", "$TimeoutSeconds"
    )
    if ($SkipActionSubmit) {
        $argParts += "--skip-action-submit"
    }
    if ($NoSecretLog) {
        $argParts += "--no-secret-log"
    }
    $childArgs = $argParts -join " "

    & $watchdogExe `
        --file $toolExe `
        --args $childArgs `
        --workdir $root `
        --timeout ($TimeoutSeconds + 15) `
        --stdout $stdout `
        --stderr $stderr

    $exit = $LASTEXITCODE
    Write-Host "Smoke stdout: $stdout"
    Write-Host "Smoke stderr: $stderr"

    if (Test-Path $stdout) {
        Write-Host "---- stdout tail ----"
        Get-Content $stdout -Tail 80
    }
    if ($exit -ne 0 -and (Test-Path $stderr)) {
        Write-Host "---- stderr tail ----"
        Get-Content $stderr -Tail 80
    }
    exit $exit
}
finally {
    if ($tunnel -ne $null -and -not $tunnel.HasExited) {
        Write-Host "Stopping SSH tunnel PID $($tunnel.Id)"
        Stop-Process -Id $tunnel.Id -Force -ErrorAction SilentlyContinue
    }
}
