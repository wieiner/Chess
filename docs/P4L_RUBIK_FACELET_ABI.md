# P4L Rubik Facelet ABI

## Contract

Phase 04 adds a compact, append-only sticker boundary to `RubikEngine`. Existing
integer-cell functions and `RubikStateDto` keep their signatures and layout.
The new storage is ordered `U,R,F,D,L,B`, with each face row-major according to
`P4L_RUBIK_FACELET_COORDINATES.md`.

Color IDs are stable in schema version 1:

| ID | Face in solved state | Color |
| --- | --- | --- |
| 1 | U | white |
| 2 | R | red |
| 3 | F | green |
| 4 | D | yellow |
| 5 | L | orange |
| 6 | B | blue |

Zero is not a physical sticker color in the engine state. UI editor drafts may
use a separate empty value before applying a complete state.

## Added exports

```cpp
int Rubik_GetFaceletSchemaVersion(void* handle);
int Rubik_GetFaceletCount(void* handle);
int Rubik_GetFacelets(void* handle, int* facelets, int capacity);
int Rubik_SetFacelets(void* handle, const int* facelets, int count);
int Rubik_GetFacelet(void* handle, int face, int row, int column);
int Rubik_SetFacelet(void* handle, int face, int row, int column, int colorId);
int Rubik_GetColorScheme(void* handle, char* buffer, int capacity);
int Rubik_ValidateFacelets(void* handle, const int* facelets, int count);
```

`Rubik_GetFacelets` supports a count query with a null/zero-capacity buffer and
requires full capacity for a data read. `Rubik_SetFacelets` validates into a
temporary vector before commit. Validation currently covers exact `6*N*N`
length, IDs 1..6, and exactly `N*N` occurrences of every color. Physical
corner/edge/parity validation is intentionally Phase 13 scope.

`Rubik_GetColorScheme` returns UTF-8-compatible JSON metadata through the same
required-size convention as existing text getters. Managed declarations use
explicit `[In]` and `[Out]` array directions.

## Transition behavior

Reset creates synchronized solved facelets for every supported N. A facelet
import makes facelets authoritative, clears trusted history, and marks the
state manual.

The Phase 04 engine does not yet permute facelets during a layer turn. To avoid
inventing sticker orientation, a legacy cell edit/import or layer rotation
marks facelets unsynchronized; `Rubik_GetFacelets` then returns `-1` and
`Rubik_GetLastInfo` explains the boundary. Phase 05 replaces that temporary
guard with the real facelet permutation.

This behavior does not alter `Rubik_GetCells`, `Rubik_SetCells`, rendering, or
reverse-history solving. It makes the new API honest while preserving all old
callers.

## Verification

Contract tests cover solved sizes 2, 3, 8, 11, and 32, including:

- schema version 1;
- exact `6*N*N` count;
- six canonical `N*N` color blocks;
- exact import/export roundtrip;
- invalid-color rejection without mutation;
- single facelet access;
- versioned color-scheme metadata;
- explicit rejection of stale facelets after a legacy-only turn.
