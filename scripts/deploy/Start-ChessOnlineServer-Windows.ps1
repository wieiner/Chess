param(
    [string]$ServerRoot = "",
    [string]$HostUrls = "http://127.0.0.1:5077",
    [string]$DataRoot = "",
    [string]$PidFile = "",
    [string]$StdoutPath = "",
    [string]$StderrPath = ""
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
if ([string]::IsNullOrWhiteSpace($StdoutPath)) {
    $StdoutPath = Join-Path $DataRoot "chessonline-server.out.log"
}
if ([string]::IsNullOrWhiteSpace($StderrPath)) {
    $StderrPath = Join-Path $DataRoot "chessonline-server.err.log"
}

$exe = Join-Path $ServerRoot "ChessOnlineServer.exe"
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
    throw "ChessOnlineServer.exe not found. Build package first: $exe"
}

New-Item -ItemType Directory -Force -Path $DataRoot | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $DataRoot "keys") | Out-Null

$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:CHESS3D_ONLINE_HostedOnline__HostUrls = $HostUrls
$env:CHESS3D_ONLINE_HostedOnline__Persistence__StorePath = (Join-Path $DataRoot "chess3d-online-store.json")
$env:CHESS3D_ONLINE_HostedOnline__DataProtection__KeyRingPath = (Join-Path $DataRoot "keys")

$process = Start-Process -FilePath $exe -WorkingDirectory $ServerRoot -RedirectStandardOutput $StdoutPath -RedirectStandardError $StderrPath -PassThru -WindowStyle Hidden
Set-Content -LiteralPath $PidFile -Value $process.Id -Encoding ASCII

Write-Host "ChessOnlineServer started."
Write-Host "PID: $($process.Id)"
Write-Host "URL: $HostUrls"
Write-Host "DataRoot: $DataRoot"
Write-Host "Stdout: $StdoutPath"
Write-Host "Stderr: $StderrPath"
