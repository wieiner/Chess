[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$SetId,
    [ValidateSet("ordinary-chess", "chess3d-common", "asgard", "rubik-convergence", "hodge", "rubik")]
    [string]$Template = "ordinary-chess",
    [string]$InboxRoot = "",
    [switch]$Force,
    [switch]$PassThru
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
if ($SetId -notmatch '^[A-Za-z0-9._-]{1,128}$') { throw "SetId is invalid." }
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$category = switch ($Template) {
    "ordinary-chess" { "chess2d\pieces" }
    "chess3d-common" { "chess3d\common" }
    "asgard" { "chess3d\asgard" }
    "rubik-convergence" { "chess3d\rubik-convergence" }
    "hodge" { "chess3d\hodge" }
    "rubik" { "rubik\cubies" }
}
if ([string]::IsNullOrWhiteSpace($InboxRoot)) {
    $InboxRoot = Join-Path $repositoryRoot "rude-resource\model-inbox"
}
$inboxFull = [IO.Path]::GetFullPath($InboxRoot)
$setRoot = [IO.Path]::GetFullPath((Join-Path (Join-Path $inboxFull $category) $SetId))
$boundedRoot = $inboxFull.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $setRoot.StartsWith($boundedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Set path escapes the inbox root."
}

if (Test-Path -LiteralPath $setRoot) {
    if (-not $Force) { throw "Inbox set '$SetId' already exists." }
} else {
    [void](New-Item -ItemType Directory -Path $setRoot)
}
foreach ($directory in @("source", "textures", "license", "metadata")) {
    [void](New-Item -ItemType Directory -Path (Join-Path $setRoot $directory) -Force)
}

$roles = switch ($Template) {
    "ordinary-chess" {
        @(
            "chess.white.pawn", "chess.white.knight", "chess.white.bishop",
            "chess.white.rook", "chess.white.queen", "chess.white.king",
            "chess.black.pawn", "chess.black.knight", "chess.black.bishop",
            "chess.black.rook", "chess.black.queen", "chess.black.king",
            "chess.board.lightTile", "chess.board.darkTile", "chess.board.frame"
        )
    }
    "chess3d-common" {
        @("chess3d.common.pawn", "chess3d.common.knight", "chess3d.common.bishop",
          "chess3d.common.rook", "chess3d.common.queen", "chess3d.common.king")
    }
    "asgard" { @("asgard.core", "asgard.anchor", "asgard.reserveSlot", "asgard.fusionMarker") }
    "rubik-convergence" { @("rubikConvergence.core", "rubikConvergence.layerMarker", "rubikConvergence.turnMarker") }
    "hodge" { @("hodge.primaryMarker", "hodge.mirrorMarker", "hodge.projectionArrow") }
    "rubik" { @("rubik.cubieBody", "rubik.sticker", "rubik.core") }
}

$draft = [ordered]@{
    format = "chess-model-inbox-draft"
    version = "1.0"
    setId = $SetId
    template = $Template
    status = "local-unreviewed"
    author = ""
    sourceUrl = ""
    licenseSpdxId = ""
    roles = $roles
    instructions = @(
        "Place original files under source/ and source textures under textures/.",
        "Place complete license evidence under license/.",
        "Fill author, sourceUrl, and licenseSpdxId before import.",
        "Do not copy this draft directly into assets/models."
    )
}
$draftPath = Join-Path $setRoot "metadata\asset-manifest-v2.draft.json"
if (-not (Test-Path -LiteralPath $draftPath) -or $Force) {
    $draft | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $draftPath -Encoding utf8NoBOM
}

$readme = @"
Local model inbox: $SetId
Template: $Template

1. source/   - raw FBX, Blend, GLB, OBJ or archive after manual extraction.
2. textures/ - source textures.
3. license/  - license text and provenance evidence.
4. metadata/ - local draft; fill it before import.

Nothing in this directory is a runtime asset.
"@
$readmePath = Join-Path $setRoot "README.local.txt"
if (-not (Test-Path -LiteralPath $readmePath) -or $Force) {
    $readme | Set-Content -LiteralPath $readmePath -Encoding utf8NoBOM
}

Write-Host "Model set inbox ready: $setRoot"
Write-Host "Roles: $($roles -join ', ')"
if ($PassThru) { Get-Item -LiteralPath $setRoot }
