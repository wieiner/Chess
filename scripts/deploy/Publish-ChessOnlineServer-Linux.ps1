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
}
else {
    dotnet publish $project -c $Configuration -r $Runtime --self-contained false -p:Platform=x64 -o $OutputPath
}
