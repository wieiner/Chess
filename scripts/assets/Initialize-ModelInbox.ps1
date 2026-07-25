[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [switch]$PassThru
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
}

$inboxRoot = Join-Path $RepositoryRoot "rude-resource\model-inbox"
$relativeDirectories = @(
    "chess2d\pieces",
    "chess2d\boards",
    "chess3d\common",
    "chess3d\asgard",
    "chess3d\rubik-convergence",
    "chess3d\hodge",
    "rubik\cubies",
    "rubik\stickers"
)

foreach ($relativeDirectory in $relativeDirectories) {
    $path = Join-Path $inboxRoot $relativeDirectory
    [void](New-Item -ItemType Directory -Path $path -Force)
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($git) {
    $ignored = & $git.Source -C $RepositoryRoot check-ignore "rude-resource/model-inbox" 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "The model inbox is not ignored by Git. Refusing to initialize an unsafe drop zone."
    }
}

Write-Host "Model inbox ready: $inboxRoot"
Write-Host "Raw FBX/Blend/ZIP/textures stay local until license and storage review."

if ($PassThru) {
    Get-Item -LiteralPath $inboxRoot
}
