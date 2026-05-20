# Chess3D Rubik Layer Turn Semantics

P2H implements deterministic quarter-turn layer transforms for the 8x8x8 Chess3D board.

## Board

- Coordinates: `x,y,z in 0..7`.
- Layers: `0..7`.
- Quarter turns: `-1` or `+1`.
- ABI axis codes: `0 = Z`, `1 = Y`, `2 = X`.

## Coordinate Transforms

For `axis = 0` / `Z`, `z` is fixed:

- `+1`: `(x, y, z) -> (7 - y, x, z)`
- `-1`: `(x, y, z) -> (y, 7 - x, z)`

For `axis = 1` / `Y`, `y` is fixed. This follows the existing engine convention:

- `+1`: `(x, y, z) -> (7 - z, y, x)`
- `-1`: `(x, y, z) -> (z, y, 7 - x)`

For `axis = 2` / `X`, `x` is fixed:

- `+1`: `(x, y, z) -> (x, 7 - z, y)`
- `-1`: `(x, y, z) -> (x, z, 7 - y)`

## Profile Semantics

- `disabled`: runtime ritual turns clean-fail.
- `ritualTurn`: legal/special runtime layer action for Rubik convergence.
- `sandbox` and `globalEvent`: reserved for later profile behavior.

P2H enables runtime turns only for `ritualTurn`. The old draft profile can still perform non-stack debug rotation for compatibility.

## Targets

Target slots remain fixed in world coordinates. If a layer turn moves a piece or stack away from a target cell, anchors are recomputed against the fixed target-slot model.

