# P4G Online Board Snapshot Adapter

Date: 2026-06-27

`src\ChessOnlineClient\OnlineChess3DBoardSnapshot.cs` adds the first P4G board adapter for the playable online client. It is intentionally client-side and read-only.

## Input

The adapter consumes `OnlineSnapshot.SaveGameJson` from the authoritative server snapshot.

Required savegame fields:

- `format = chess3d-savegame`;
- `board.width`, `board.height`, `board.depth`;
- `currentSide`;
- `currentMacroPlayer`;
- `currentTurnKind`;
- `projectedBoard`, currently 512 piece codes for the 8x8x8 board.

The adapter also carries snapshot metadata from `OnlineSnapshot`:

- `rulesetId`;
- `roomId`;
- `tableId`;
- `serverSeq`;
- `stateHash`;
- `actionCount`;
- `lastActionNotation`.

## Output

`OnlineChess3DBoardSnapshot` exposes:

- dimensions;
- current side / macro-player / turn kind;
- `Cells`, one `OnlineChess3DBoardCell` per board index;
- `OccupiedCells`;
- `GetCell(x, y, z)`.

The coordinate mapping follows the native engine convention:

```text
index = z * width * height + y * width + x
```

For the current board this means:

- `(0,0,0) -> 0`;
- `(7,7,7) -> 511`.

Each `OnlineChess3DBoardCell` exposes:

- `Index`;
- `X`, `Y`, `Z`;
- `PieceCode`;
- `IsOccupied`;
- `Side = PieceCode / 10`;
- `PieceType = PieceCode % 10`;
- `Coordinate`.

## Safety Boundary

The adapter does not:

- load or mutate the native engine;
- generate legal moves;
- apply actions;
- infer mode-specific rules;
- replace the authoritative server state;
- log credentials or tokens.

The online UI must submit actions to the server with `ExpectedStateHashBefore`, then refresh from server events/snapshots. A local board derived from `SaveGameJson` is a view model, not the source of truth.

## Failure Behavior

`OnlineChess3DBoardSnapshotParser.TryParse(...)` returns `false` with a readable error for:

- missing snapshot;
- missing `SaveGameJson`;
- invalid JSON;
- wrong savegame format;
- missing/non-array `projectedBoard`;
- non-integer projected board entries;
- wrong projected board length;
- invalid dimensions.

This keeps the UI from presenting stale or guessed board state.

## Current Limitations

- Stack/fusion/anchor visual metadata is not projected into the first board cell DTO.
- Legal target preview is not yet exposed online.
- Hodge mirror previews and Rubik layer actions still need dedicated online UI affordances.
- Rich local `Chess3DWindow` visuals remain separate from this first online board adapter.

Those are planned follow-up pieces inside P4G/P4H without adding a sixth profile or changing rules.
