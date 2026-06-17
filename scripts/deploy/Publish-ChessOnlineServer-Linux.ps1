param(
    [string]$Configuration = "Release",
    [string]$Runtime = "linux-x64",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$project = Join-Path $root "src\ChessOnlineServer\ChessOnlineServer.csproj"
$projectText = Get-Content -LiteralPath $project -Raw

if ($projectText -match "net8\.0-windows" -and -not $Force) {
    throw "ChessOnlineServer currently targets net8.0-windows and uses the Windows native Chess3DEngine DLL. Linux runtime publish is deferred; rerun with -Force only for portability experiments."
}

dotnet publish $project -c $Configuration -r $Runtime --self-contained false -p:Platform=x64
