# Chess3D Knockback / Reserve ABI

P2G adds append-only C ABI functions. Existing piece, stack, fusion, and profile ABI signatures are unchanged.

## Enablement

```cpp
int Chess3D_IsReserveEnabled(void* handle);
int Chess3D_IsKnockbackEnabled(void* handle);
```

Classic and single-side profiles return `0`. Asgard and Rubik convergence return `1`.

## Reserve Counts

```cpp
int Chess3D_GetReserveCount(void* handle, int side, int pieceType);
int Chess3D_GetReserveTotal(void* handle, int side);
int Chess3D_ClearReserve(void* handle, int side);
```

Counts are keyed by side `1..6` and piece type `1..6`.

## Last Capture Telemetry

```cpp
int Chess3D_GetLastCaptureWasKnockback(void* handle);
int Chess3D_GetLastCapturedPieceCode(void* handle);
int Chess3D_GetLastCapturedPieceReserveDestination(void* handle);
int Chess3D_GetLastKnockbackHomeX(void* handle);
int Chess3D_GetLastKnockbackHomeY(void* handle);
int Chess3D_GetLastKnockbackHomeZ(void* handle);
int Chess3D_GetLastKnockbackInfo(void* handle, int* capturedPieceCode, int* destinationKind, int* x, int* y, int* z);
```

Destination kinds:

```text
0 none
1 home
2 reserve
3 classicRemoved
```

All functions fail cleanly on invalid handles or invalid pointer arguments. Invalid side/type queries return `0`.
