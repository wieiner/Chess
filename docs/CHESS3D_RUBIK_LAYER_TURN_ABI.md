# Chess3D Rubik Layer Turn ABI

P2H adds append-only C ABI functions. Existing `Chess3D_RotateLayer` keeps its signature.

## Functions

```c
int Chess3D_IsLayerTurnEnabled(void* handle);
int Chess3D_CanRotateLayer(void* handle, int axis, int layer, int quarterTurns);
int Chess3D_GetLastLayerTurnInfo(void* handle, int* axis, int* layer, int* quarterTurns, int* resultCode);
int Chess3D_GetLayerTurnProfileSummary(void* handle, char* buffer, int capacity);
int Chess3D_GetLayerTurnResultName(int resultCode, char* buffer, int capacity);
```

## Result Codes

- `0 none`
- `1 success`
- `2 disabled`
- `3 invalidAxis`
- `4 invalidLayer`
- `5 invalidQuarterTurns`
- `6 stackMoveFailed`
- `7 internalError`

## Compatibility

Old piece, stack, fusion, reserve, and move ABI calls are unchanged. Invalid handles, invalid coordinates, null output pointers, and undersized buffers fail cleanly according to the existing ABI style.

