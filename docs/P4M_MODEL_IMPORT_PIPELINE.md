# P4M Offline Model Import Pipeline

`scripts/assets/Import-ModelAsset.ps1` is a local-file-only promotion boundary.
It accepts OBJ or GLB after conversion, requires a non-empty license file and
provenance text, hashes the source and staged copy, emits a v2 draft manifest,
and promotes through an isolated `.tmp/asset-import` directory.

The importer does not download URLs, follow reparse points, unpack archives, or
convert FBX/Blend. `-DryRun` validates and reports without writing the selected
runtime output. A destination collision fails unless the operator explicitly
uses `-Force`; replacement is rollback-aware and never modifies the source.

Example:

```powershell
pwsh -NoProfile -File .\scripts\assets\Import-ModelAsset.ps1 `
  -InputPath .\rude-resource\model-inbox\chess2d\pieces\my-set\king.glb `
  -SetId my-set `
  -Role chess.white.king `
  -App Chess2D `
  -LicenseFile .\rude-resource\model-inbox\chess2d\pieces\my-set\LICENSE.txt `
  -SourceMetadata "Author and source URL recorded locally" `
  -TargetFormat glb `
  -DryRun
```

The generated `LicenseRef-Provided` status means evidence was supplied, not
that redistribution was approved. Phase 31 validation and Phase 41 QA remain
mandatory before enabling or packaging a set.
