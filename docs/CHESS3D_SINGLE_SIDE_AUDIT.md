# Chess3D Single-Side Audit

P2A audit target: single-side 3D chess rules for one army on an 8x8x8 board.

## Current Engine Shape

`src/Chess3DEngine` is a native C++ DLL with a C ABI consumed by WPF and contract tests. The engine owns:

- fixed 8x8x8 board storage as 512 integer cells;
- piece codes encoded as `side * 10 + type`;
- side ids reserved as `1..6`;
- draft movement generation;
- draft six-face setup;
- Rubik-style layer rotation as a board transform;
- simple material/center evaluation and shallow minimax.

The public ABI is declared in `src/Chess3DEngine/Chess3DEngine.h`. Existing exported functions include create/destroy/reset/clear, JSON rules load/read, rules/state/board read, `SetPiece`/`GetPiece`, legal move generation, piece move generation, move execution, best move, layer rotation, and position text.

## Coordinates

The engine already uses 8x8x8 integer coordinates:

- `x = 0..7`
- `y = 0..7`
- `z = 0..7`
- index layout: `z * 64 + y * 8 + x`

The ABI comments map `x` to files `a..h`, `y` to ranks `1..8`, and `z` to levels `1..8`.

## Sides And Players

The current model reserves six side ids. The default draft JSON defines sides on the six cube faces:

- side 1: zMin, forward +Z
- side 2: zMax, forward -Z
- side 3: yMin, forward +Y
- side 4: yMax, forward -Y
- side 5: xMin, forward +X
- side 6: xMax, forward -X

`activeSideCount` controls how many sides are placed and included in turn order. Before P2A the default was six-sided draft behavior. P2A uses the same model with `activeSideCount = 1`.

## Pieces

Piece type ids match classic chess:

- `1`: pawn
- `2`: knight
- `3`: bishop/officer
- `4`: rook
- `5`: queen
- `6`: king

The engine does not currently store rich piece objects. It stores only integer codes in the board array.

## Reset

`Chess3D_Reset` calls the engine reset helper. Reset clears the board and places each active side on the central 4x4 square of that side's home face.

The setup helper is generated procedurally in C++ rather than parsed from the JSON setup list. P2A keeps this behavior and makes the single-side JSON describe the same canonical layout.

## GetLegalMoves

`Chess3D_GetLegalMoves` generates moves for `sideToMove` only. If `movementProfile` is setup-only, it returns zero moves.

Current movement is pseudo-legal draft movement. It checks bounds, own-piece blocking, captures, line-piece blockers, knight jumps, pawn forward/capture vectors, and promotion. It does not fully enforce 3D king safety, check, mate, or stalemate.

## TryMakeMove

`Chess3D_TryMakeMove` regenerates legal moves for the current position and applies a move only if the requested source and target match one generated move. It updates board cells, stores `lastMove`, applies promotion when flagged, and advances `sideToMove` through active sides.

For P2A with one active side, turn order wraps back to side 1 after every move.

## SetPiece / GetPiece

`Chess3D_SetPiece` validates coordinates, side range `0..6`, and type range `0..6`. Side or type zero clears the target cell. `Chess3D_GetPiece` returns zero for invalid coordinates or empty cells.

`Chess3D_SetBoard` validates all 512 piece codes before accepting a board snapshot.

## Rules JSON

The current runtime rules asset lives at:

- `src/ChessApp/Assets/Rules3D/cube8x8x8_draft.json`

There is no root-level `assets/rules` directory at this baseline. `Chess3DApp` links and copies `src/ChessApp/Assets/Rules3D/**` to its output.

The loader is intentionally light and tolerant: it extracts known fields by key and ignores unknown metadata. P2A can safely add metadata-rich rules JSON as long as the existing keys remain present.

## Draft Areas

These behaviors remain draft:

- final six-sided laws;
- king safety in 3D;
- check, mate, and stalemate in 3D;
- exact multiplayer turn semantics;
- Rubik layer turns as legal chess actions;
- JSON-driven setup parsing.

## Safe P2A Changes

Safe changes for P2A:

- add a separate single-side rules JSON asset;
- document formal single-side local rules;
- adjust the procedural central 4x4 setup to the P2A canonical layout;
- add the pawn initial double move for the local +Z side and generalized forward faces;
- add narrow JSON envelope rejection for clearly invalid rules text;
- strengthen contract tests around setup, movement, captures, promotion, and rules metadata;
- update docs without changing 2D chess, Rubik, Online, CUDA optional behavior, or existing public ABI signatures.
