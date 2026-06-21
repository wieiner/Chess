param(
    [string]$Configuration = "Release",
    [string]$Runtime = "linux-x64",
    [string]$OutputPath = "",
    [string]$NativeLibraryPath = ""
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$project = Join-Path $root "src\ChessOnlineServer\ChessOnlineServer.csproj"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $root "DeploymentOutput\$Runtime\ChessOnlineServer"
}

if (-not [string]::IsNullOrWhiteSpace($NativeLibraryPath)) {
    if (-not (Test-Path -LiteralPath $NativeLibraryPath)) {
        throw "Linux native library not found: $NativeLibraryPath"
    }

    dotnet publish $project -c $Configuration -r $Runtime --self-contained false -p:Platform=x64 -p:Chess3DEngineLinuxPath="$NativeLibraryPath" -o $OutputPath
    Copy-Item -LiteralPath $NativeLibraryPath -Destination (Join-Path $OutputPath "libChess3DEngine.so") -Force
    Get-ChildItem -LiteralPath $OutputPath -Filter "libChess3DEngine*.so" -File |
        Where-Object { $_.Name -ne "libChess3DEngine.so" } |
        Remove-Item -Force
}
else {
    dotnet publish $project -c $Configuration -r $Runtime --self-contained false -p:Platform=x64 -o $OutputPath
}

$serverDll = Join-Path $OutputPath "ChessOnlineServer.dll"
if (-not (Test-Path -LiteralPath $serverDll)) {
    throw "Linux publish did not produce ChessOnlineServer.dll: $serverDll"
}

if (-not [string]::IsNullOrWhiteSpace($NativeLibraryPath)) {
    $linuxNative = Join-Path $OutputPath "libChess3DEngine.so"
    if (-not (Test-Path -LiteralPath $linuxNative)) {
        throw "Linux publish did not include libChess3DEngine.so: $linuxNative"
    }
}
