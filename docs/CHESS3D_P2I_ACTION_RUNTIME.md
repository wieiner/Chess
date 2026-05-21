# Chess3D P2I Action Runtime

P2I connects the existing Chess3D runtime features through one action-history layer.

## Move Integration

Successful normal moves append `ActionKind=Move`. The record captures from/to coordinates, side, piece, captured piece, capture destination, and core transition flags.

Outer-field classic captures record `removed`. Asgard/Rubik knockback captures record `home` or `reserve`. Core co-occupancy records `coreCoOccupancy` without deleting existing stack entries.

## Layer-Turn Integration

Successful Rubik convergence layer turns append `ActionKind=LayerTurn`. The record stores ABI axis code, layer, quarter-turn sign, result code, and notation such as `#4 LAYER Z[2]+`.

Disabled or invalid layer turns do not append history.

## Reserve Restore Integration

Successful restore appends `ActionKind=ReserveRestore`, decrements reserve count, updates the board, and writes notation such as `#3 S2 RESTORE P reserve->(5,5,7)`.

## Reset And Profile Load

Reset, clear, board synchronization, and profile/rules loading clear action history because they define a new test/setup/session boundary.

## Compatibility

Old board, stack, fusion, reserve, and layer-turn ABI remains intact. P2I adds new getters and actions only.
