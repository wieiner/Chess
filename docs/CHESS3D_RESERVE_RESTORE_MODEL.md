# Chess3D Reserve Restore Model

P2I turns reserve restore from a future note into a real runtime action for profiles with reserve enabled.

## Preconditions

`Chess3D_RestoreReservePiece(side, pieceType, x, y, z)` succeeds only when:

- reserve is enabled by the active RuleProfile;
- `side` is in `1..6`;
- `pieceType` is a known piece type;
- reserve count for `side/pieceType` is greater than zero;
- target coordinate is valid;
- target is a matching home slot for that side and type;
- target projected cell is empty;
- target is outside the Forbidden Core for P2I.

## Success

On success:

- reserve count is decremented;
- `side * 10 + pieceType` is placed on the target cell;
- anchors/victory are recomputed;
- an `ActionRecord` with `ActionKind=ReserveRestore` is appended;
- notation contains `RESTORE`.

## Failure

Failures are clean:

- no board mutation;
- no reserve mutation;
- no action-history append.

## Auto Restore

`Chess3D_AutoRestoreReservePiece(side, pieceType)` finds the first free matching home slot and delegates to the same restore path. If no matching slot is free, it fails without mutation.

## Deferred

- Restore into Forbidden Core.
- Restore captures.
- UI inventory drag/drop.
- Unique piece ids.
- Online serialization.
