$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$caseRoot = Join-Path $root ".tmp\asset-import-contracts"
$outputRoot = Join-Path $caseRoot "output"
$importer = Join-Path $root "scripts\assets\Import-ModelAsset.ps1"

function Invoke-ExpectedFailure([scriptblock]$Action, [string]$Label) {
    try { & $Action; throw "Expected failure: $Label" }
    catch {
        if ($_.Exception.Message -eq "Expected failure: $Label") { throw }
        Write-Host "PASS rejected $Label"
    }
}

try {
    if (Test-Path -LiteralPath $caseRoot) { Remove-Item -LiteralPath $caseRoot -Recurse -Force }
    [void](New-Item -ItemType Directory -Path $caseRoot)
    $obj = Join-Path $caseRoot "triangle.obj"
    $glb = Join-Path $caseRoot "placeholder.glb"
    $license = Join-Path $caseRoot "LICENSE.txt"
    "v 0 0 0`nv 1 0 0`nv 0 1 0`nf 1 2 3" | Set-Content -LiteralPath $obj -Encoding ascii
    [IO.File]::WriteAllBytes($glb, [byte[]](0x67,0x6c,0x54,0x46,0x02,0x00,0x00,0x00,0x0c,0x00,0x00,0x00))
    "CC0-1.0 synthetic contract fixture" | Set-Content -LiteralPath $license -Encoding ascii

    $dry = & $importer -InputPath $obj -SetId "dry-run" -Role "chess.white.king" `
        -App Chess2D -LicenseFile $license -SourceMetadata "Synthetic test" `
        -OutputRoot $outputRoot -TargetFormat obj -DryRun
    if (Test-Path (Join-Path $outputRoot "dry-run")) { throw "Dry run wrote to runtime output." }
    if (($dry -join "`n") -notmatch '"status":\s*"DRY_RUN_VALID"') { throw "Dry-run result missing." }
    Write-Host "PASS valid OBJ dry run"

    & $importer -InputPath $glb -SetId "glb-set" -Role "chess.white.king" `
        -App Chess2D -LicenseFile $license -SourceMetadata "Synthetic test" `
        -OutputRoot $outputRoot -TargetFormat glb | Out-Null
    $manifestPath = Join-Path $outputRoot "glb-set\asset-manifest-v2.json"
    if (-not (Test-Path $manifestPath)) { throw "GLB manifest was not promoted." }
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $assetPath = Join-Path (Split-Path $manifestPath) ([string]$manifest.assets[0].path)
    if ((Get-FileHash $assetPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.assets[0].sha256) {
        throw "Promoted SHA mismatch."
    }
    Write-Host "PASS GLB placeholder promotion and SHA"

    Invoke-ExpectedFailure { & $importer -InputPath $obj -SetId "missing-license" -Role "chess.white.king" -App Chess2D -LicenseFile (Join-Path $caseRoot "missing.txt") -SourceMetadata "test" -OutputRoot $outputRoot -TargetFormat obj } "missing license"
    $unknown = Join-Path $caseRoot "model.fbx"; "x" | Set-Content $unknown
    Invoke-ExpectedFailure { & $importer -InputPath $unknown -SetId "unknown" -Role "chess.white.king" -App Chess2D -LicenseFile $license -SourceMetadata "test" -OutputRoot $outputRoot -TargetFormat glb } "unknown extension"
    Invoke-ExpectedFailure { & $importer -InputPath $obj -SetId "..\escape" -Role "chess.white.king" -App Chess2D -LicenseFile $license -SourceMetadata "test" -OutputRoot $outputRoot -TargetFormat obj } "traversal set"
    Invoke-ExpectedFailure { & $importer -InputPath $obj -SetId "glb-set" -Role "chess.white.king" -App Chess2D -LicenseFile $license -SourceMetadata "test" -OutputRoot $outputRoot -TargetFormat obj } "duplicate destination"
    Invoke-ExpectedFailure { & $importer -InputPath "https://example.invalid/model.glb" -SetId "url" -Role "chess.white.king" -App Chess2D -LicenseFile $license -SourceMetadata "test" -OutputRoot $outputRoot -TargetFormat glb } "URL input"

    $leftovers = @(Get-ChildItem (Join-Path $root ".tmp\asset-import") -Force -ErrorAction SilentlyContinue)
    if ($leftovers.Count -ne 0) { throw "Importer left interrupted staging directories." }
    Write-Host "PASS staging cleanup"
} finally {
    if (Test-Path -LiteralPath $caseRoot) { Remove-Item -LiteralPath $caseRoot -Recurse -Force }
}
