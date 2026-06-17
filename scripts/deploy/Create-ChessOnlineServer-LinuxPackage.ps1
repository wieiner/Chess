param(
    [string]$PackageRoot = "LinuxPackage\ChessOnlineServer"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$source = Join-Path $root "ProductionOutput\ChessOnlineServer"
$target = Join-Path $root $PackageRoot

if (!(Test-Path -LiteralPath $source)) {
    throw "ProductionOutput\ChessOnlineServer does not exist. Run scripts\verify.ps1 or tools\release\Build-Production.ps1 first."
}

New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -LiteralPath (Join-Path $source "*") -Destination $target -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root "deploy\linux") -Destination (Join-Path $target "Deploy") -Recurse -Force
Write-Host "Created Linux deployment scaffold at $target. Runtime Linux execution is deferred until the native engine boundary is portable."
