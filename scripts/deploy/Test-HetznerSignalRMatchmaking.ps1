param(
    [Parameter(Mandatory = $true)]
    [string]$SshTarget,

    [Parameter(Mandatory = $true)]
    [string]$SshKeyPath,

    [string]$RemoteHost = "127.0.0.1",
    [int]$RemotePort = 5077,
    [int]$LocalPort = 15077,
    [string]$RulesetId = "asgard-convergence-3d-8x8x8-v0.1",
    [int]$TimeoutSeconds = 120,
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

function Invoke-DotnetBuild {
    param([string]$Project)
    dotnet build $Project -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed: $Project"
    }
}

if ($BuildSmokeTool -or -not (Test-Path $toolExe)) {
    Invoke-DotnetBuild $toolProject
}
if (-not (Test-Path $watchdogExe)) {
    Invoke-DotnetBuild $watchdogProject
}

$tunnel = $null
try {
    if (-not $SkipTunnel) {
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

    $baseUrl = if ($SkipTunnel) {
        "http://$($RemoteHost):$RemotePort"
    } else {
        "http://127.0.0.1:$LocalPort"
    }

    $stdout = Join-Path $logDir "hetzner-signalr-smoke.stdout.log"
    $stderr = Join-Path $logDir "hetzner-signalr-smoke.stderr.log"
    $childArgs = @(
        "--base-url", $baseUrl,
        "--ruleset", $RulesetId,
        "--timeout", "$TimeoutSeconds"
    ) -join " "

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
    if ($exit -ne 0) {
        Write-Host "---- stdout tail ----"
        if (Test-Path $stdout) { Get-Content $stdout -Tail 80 }
        Write-Host "---- stderr tail ----"
        if (Test-Path $stderr) { Get-Content $stderr -Tail 80 }
        exit $exit
    }
}
finally {
    if ($tunnel -ne $null -and -not $tunnel.HasExited) {
        Write-Host "Stopping SSH tunnel PID $($tunnel.Id)"
        Stop-Process -Id $tunnel.Id -Force -ErrorAction SilentlyContinue
    }
}
