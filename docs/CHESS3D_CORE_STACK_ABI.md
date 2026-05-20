# Chess3D Core Stack ABI

P2E adds append-only C ABI functions to `Chess3DEngine.dll`.

## Functions

```c
int Chess3D_IsCoreStackEnabled(void* handle);

int Chess3D_GetCoreStackCount(void* handle, int x, int y, int z);

int Chess3D_GetCoreStackEntry(
    void* handle,
    int x, int y, int z,
    int stackIndex,
    int* side,
    int* pieceType,
    int* pieceCode,
    int* flags);

int Chess3D_PushCoreStackPiece(void* handle, int x, int y, int z, int pieceCode);

int Chess3D_ClearCoreStack(void* handle, int x, int y, int z);

int Chess3D_RemoveCoreStackEntry(void* handle, int x, int y, int z, int stackIndex);

int Chess3D_GetProjectedPiece(void* handle, int x, int y, int z);
```

## Compatibility

Old APIs are unchanged:

- `Chess3D_GetPiece` returns the projected/top piece;
- `Chess3D_SetPiece` replaces the stack with one entry inside the core;
- `Chess3D_SetPiece(..., 0, 0)` clears the core stack;
- `Chess3D_GetBoard` returns the projected 512-int board;
- `Chess3D_SetBoard` imports projected board state and creates one-entry stacks for occupied core cells when stack mode is active.

## Failure Rules

The stack ABI returns clean failure for:

- invalid handle;
- invalid coordinates;
- disabled stack mode;
- outside-core push/clear/remove;
- invalid stack index;
- null output pointers in `Chess3D_GetCoreStackEntry`;
- invalid or empty piece code for push.

## Projection

Projection rule:

```text
projected piece = last pushed stack entry
```

This is simple, deterministic, and sufficient for old UI and tests until a richer stack visualizer exists.
