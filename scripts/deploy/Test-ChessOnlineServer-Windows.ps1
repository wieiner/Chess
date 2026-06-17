param(
    [string]$ServerRoot = "",
    [int]$Port = 5077,
    [int]$TimeoutSeconds = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $ScriptDir "..\.."))
if ([string]::IsNullOrWhiteSpace($ServerRoot)) {
    if (Test-Path -LiteralPath (Join-Path $RepoRoot "ChessOnlineServer.exe") -PathType Leaf) {
        $ServerRoot = $RepoRoot
    }
    else {
        $ServerRoot = Join-Path $RepoRoot "ProductionOutput\ChessOnlineServer"
    }
}
$ServerRoot = [System.IO.Path]::GetFullPath($ServerRoot)
$dataRoot = Join-Path $ServerRoot "Data\smoke"
$hostUrl = "http://127.0.0.1:$Port"

try {
    & (Join-Path $ScriptDir "Start-ChessOnlineServer-Windows.ps1") -ServerRoot $ServerRoot -HostUrls $hostUrl -DataRoot $dataRoot

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $liveUrl = "$hostUrl/healthz/live"
    $readyUrl = "$hostUrl/healthz/ready"
    do {
        try {
            $live = Invoke-WebRequest -UseBasicParsing -Uri $liveUrl -TimeoutSec 2
            if ($live.StatusCode -eq 200) {
                $ready = Invoke-WebRequest -UseBasicParsing -Uri $readyUrl -TimeoutSec 2
                if ($ready.StatusCode -eq 200) {
                    Write-Host "ChessOnlineServer Windows smoke PASS: $hostUrl"
                    exit 0
                }
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    } while ((Get-Date) -lt $deadline)

    throw "ChessOnlineServer did not become healthy at $hostUrl within $TimeoutSeconds seconds."
}
finally {
    & (Join-Path $ScriptDir "Stop-ChessOnlineServer-Windows.ps1") -ServerRoot $ServerRoot -DataRoot $dataRoot
}
