# P4M Model Import Guide

The complete offline workflow is:

```text
local inbox
-> license/provenance
-> optional Blender conversion
-> import dry run
-> manifest/SHA validation
-> preview/evidence
-> explicit approval
-> runtime promotion
-> application/package tests
```

## 1. Convert when needed

```powershell
pwsh -NoProfile -File .\scripts\assets\Convert-ModelAssetWithBlender.ps1 `
  -InputPath .\rude-resource\model-inbox\chess2d\pieces\my-set\source\king.blend `
  -OutputPath .\.tmp\asset-conversion\king.glb `
  -TargetFormat glb `
  -DryRun
```

Remove `-DryRun` only when Blender is installed and the command/report were
reviewed. A missing Blender returns `SKIPPED`; it is not installed
automatically.

## 2. Import dry run

```powershell
pwsh -NoProfile -File .\scripts\assets\Import-ModelAsset.ps1 `
  -InputPath .\.tmp\asset-conversion\king.glb `
  -SetId my-set `
  -Role chess.white.king `
  -App Chess2D `
  -LicenseFile .\rude-resource\model-inbox\chess2d\pieces\my-set\license\LICENSE.txt `
  -SourceMetadata "Author; source URL; retrieval date" `
  -TargetFormat glb `
  -DryRun
```

Dry run writes only ignored staging. The importer rejects URLs, archives,
symlinks, traversal, missing evidence, unsupported extensions, destination
collisions, SHA mismatch and validator failure.

## 3. Preview and approve

Build and run:

```powershell
dotnet build .\src\ModelAssetPreview\ModelAssetPreview.csproj -c Release -p:Platform=x64
.\src\ModelAssetPreview\bin\x64\Release\net8.0-windows\ModelAssetPreview.exe
```

Open the draft manifest/model, validate, inspect scale/pivot/material/bounds,
then save ignored evidence. Only an explicitly reviewed set belongs under
`assets/models`; approved editable sources may be copied separately to
`assets-source/models` after the Phase 28 size/storage decision.
