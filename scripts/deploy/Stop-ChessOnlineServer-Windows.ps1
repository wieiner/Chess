param(
    [string]$ServerRoot = "",
    [string]$DataRoot = "",
    [string]$PidFile = ""
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
if ([string]::IsNullOrWhiteSpace($DataRoot)) {
    $DataRoot = Join-Path $ServerRoot "Data"
}
$DataRoot = [System.IO.Path]::GetFullPath($DataRoot)
if ([string]::IsNullOrWhiteSpace($PidFile)) {
    $PidFile = Join-Path $DataRoot "chessonline-server.pid"
}

if (-not (Test-Path -LiteralPath $PidFile -PathType Leaf)) {
    Write-Host "PID file not found; no packaged ChessOnlineServer process to stop: $PidFile"
    exit 0
}

$pidText = (Get-Content -LiteralPath $PidFile -Raw).Trim()
$parsedPid = 0
if (-not [int]::TryParse($pidText, [ref]$parsedPid)) {
    Remove-Item -LiteralPath $PidFile -Force
    throw "Invalid PID file content: $PidFile"
}

$serverPid = $parsedPid
$process = Get-Process -Id $serverPid -ErrorAction SilentlyContinue
if ($process -and $process.ProcessName -like "ChessOnlineServer*") {
    Stop-Process -Id $serverPid -Force
    Write-Host "Stopped ChessOnlineServer PID $serverPid."
}
else {
    Write-Host "No running ChessOnlineServer found for PID $serverPid."
}

Remove-Item -LiteralPath $PidFile -Force -ErrorAction SilentlyContinue
