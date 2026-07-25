# P4M Model Asset Validation

`ModelAssetValidator` is the mandatory offline package gate. It applies the
closed v2 contract, package-root path containment, reparse-point rejection,
file and texture size bounds, SHA-256 matching, license notice checks,
required-role checks, declared bounds/stat checks, and bounded mesh inspection.

The OBJ inspector validates finite positions/normals/UVs, face/index ranges,
empty geometry, missing normals, and degenerate triangles. The Phase 31 GLB
check validates the container/header/chunk topology; accessor and primitive
validation belongs to the bounded runtime loader in Phase 34.

`KhronosGltfValidatorAdapter` is an additional offline check. It auto-detects
only an explicitly configured `KHRONOS_GLTF_VALIDATOR`, runs with a timeout,
and returns `SKIPPED` when unavailable. Internal validation never becomes
optional and the normalized result does not persist command paths or
timestamps.

Errors block promotion. Warnings such as pending provenance or optional
missing normals require operator review but remain distinguishable from
structural corruption.

`tools/ModelAssetValidator` exposes the same service to scripts:

```powershell
dotnet run --project .\tools\ModelAssetValidator -- `
  --manifest .\assets\models\<set-id>\asset-manifest-v2.json
```

The offline importer invokes this executable before dry-run success or
promotion.
