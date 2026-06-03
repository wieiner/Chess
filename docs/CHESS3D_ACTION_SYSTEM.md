# Chess3D Action System

P2I introduces a unified runtime action layer for Chess3D. It is intentionally small: it records successful turn-level actions without rewriting the move engine.

## ActionKind

- `None = 0`
- `Move = 1`
- `LayerTurn = 2`
- `ReserveRestore = 3`
- `ManualEdit = 4` reserved for future tooling; P2I does not write manual edits to history.
- `ProjectionCompositeMove = 5` for P2J Hodge Projection Duel composite turns.

## Recorded Actions

- Successful `Chess3D_TryMakeMove`.
- Successful `Chess3D_MakeBestMove`.
- Successful Rubik `Chess3D_RotateLayer` when the active profile permits `ritualTurn`.
- Successful `Chess3D_RestoreReservePiece` or `Chess3D_AutoRestoreReservePiece`.
- Successful `Chess3D_TryMakeProjectedMove` when the active profile permits `hodgeTriuneProjection`.
- Successful `Chess3D_ApplyAiAction` / `Chess3D_MakeBestProfileAction`, because those functions route through the same move, reserve restore, layer turn, or Hodge projected-move apply paths.
- P2K does not add new engine action kinds; it exposes the existing action history in the Chess3D control center.

## Not Recorded

- `Reset`, `Clear`, profile/rules loading, and board synchronization.
- Debug/setup helpers such as `SetPiece`, `PushCoreStackPiece`, `ClearCoreStack`, and `RemoveCoreStackEntry`.
- Failed moves, failed layer turns, failed reserve restores, and rejected Hodge composite turns.
- UI refresh, profile selection, scenario selection, action-log copy/save, and mirror preview.

## ActionRecord Fields

The runtime record stores action index, kind, side, piece code/type, from/to coordinates, layer-turn axis/layer/quarter turns, captured piece, capture destination, reserve side/type/delta, result code, flags, notation, and a short info string.

The structure is internal. Public access is through append-only C ABI getters.

## Recompute Contract

After successful actions, the engine keeps these layers consistent:

- projected board;
- CoreCell stacks;
- fusion descriptors;
- anchors and implosion progress;
- victory state;
- action history and last-action notation.

## P3E Online Authority

P3E does not add engine action kinds. The online authority submits the same existing action kinds to the server-side engine session:

- `normalMove` -> move/action history;
- `rubikLayerTurn` -> layer action history;
- `hodgeProjectedMove` -> `HPD` composite action history;
- `reserveRestore` -> restore action history.

Online action events wrap accepted engine actions with `serverSeq`, actor, state hash before/after, and notation. Rejected online commands are not recorded in engine action history.
