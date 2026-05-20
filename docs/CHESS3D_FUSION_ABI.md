# Chess3D Fusion ABI

P2F adds append-only C ABI functions to `Chess3DEngine.dll`.

## Functions

- `Chess3D_IsFusionEnabled(handle)`
- `Chess3D_RecomputeFusion(handle)`
- `Chess3D_GetCoreFusionKind(handle, x, y, z)`
- `Chess3D_GetCoreFusionState(handle, x, y, z, ...)`
- `Chess3D_IsCoreCellContested(handle, x, y, z)`
- `Chess3D_HasRoyalPairFusion(handle, x, y, z, side)`
- `Chess3D_GetSideFusionCount(handle, side)`
- `Chess3D_GetSideContestedCount(handle, side)`
- `Chess3D_GetSideImplosionProgress(handle, side)`
- `Chess3D_GetFusionKindName(fusionKind, buffer, bufferSize)`

## Safety

Invalid handles, invalid coordinates, disabled fusion profiles, and outside-core cells fail cleanly or return `none`/`0`.

## Compatibility

The old piece and stack ABI is unchanged. `GetPiece` still returns projected/top piece. Stack ABI remains the way to inspect actual entries. Fusion ABI is read-only descriptor access.
