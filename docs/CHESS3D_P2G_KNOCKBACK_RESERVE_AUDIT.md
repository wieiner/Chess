# Chess3D P2G Knockback / Reserve Audit

P2G starts from the P2F runtime where `Position::board` remains a 512-int projected board, Forbidden Core cells can hold `CoreStackEntry` vectors, and fusion is a descriptor overlay.

## 1. Ordinary capture before P2G

Ordinary capture was implicit in `applyMove`: if a move landed on an occupied non-core destination, the destination was overwritten by the moving piece and the captured piece disappeared.

The generated `Chess3DMoveDto` already carried `captured` and `MoveCapture` for non-core enemy destinations.

## 2. Outside to outside

Before P2G this path used the legacy single-occupancy board only:

- own occupied destination rejected by move generation;
- enemy occupied destination accepted as capture;
- captured piece removed by overwrite.

This is the safest insertion point for `knockbackCapture`.

## 3. Outside to core

For stack-enabled profiles, a target inside the Forbidden Core is treated as a stack destination. Existing occupants remain and the moving piece is appended. Move generation intentionally does not mark this as ordinary capture.

P2G should not knock back core occupants on entry.

## 4. Core to core

P2E moves the projected/top stack entry from one core stack to another. Ordinary capture is disabled in the core. P2F then recomputes fusion/contested descriptors.

P2G should keep this non-destructive behavior.

## 5. Core to outside

P2E moves the projected/top stack entry out to an ordinary cell. Before P2G, an enemy outside destination was overwritten like a classic capture.

P2G can route that outside captured piece through home-or-reserve without changing the core stack model.

## 6. Recompute points

State is recomputed after:

- profile load and reset;
- board clear/set;
- setup changes;
- stack push/remove/clear;
- successful moves.

`recomputeAnchors` calls fusion recompute first, then anchors, implosion progress, and compatible victory.

## 7. Profiles

`classic_six_side_3d_v0_1` and `single_side_3d_v0_1` use `classicCapture`, `knockbackProfile: none`, and `reserveProfile: none`.

`asgard_convergence_3d_v0_1` and `rubik_convergence_3d_v0_1` use `knockbackCapture`, `knockbackProfile: homeOrReserve`, and `reserveProfile: sidePieceTypeCounts`.

## 8. Why reserve counts, not ids

The engine still stores pieces as:

```text
pieceCode = side * 10 + pieceType
```

There are no unique piece ids yet. A reserve inventory by `side + pieceType` matches the current ABI, is deterministic, and avoids a deep identity refactor.

## 9. Safe P2G scope

Safe:

- add reserve counters by side/type;
- add last-capture telemetry;
- route outer-field captures to home slot or reserve for Asgard/Rubik profiles;
- keep classic capture unchanged;
- recompute stack/fusion/anchor state after moves.

Unsafe for P2G:

- reserve restore action;
- unique piece ids;
- destructive implosion;
- Rubik turns moving stacks/reserve;
- online serialization of reserve/stack/fusion state.
