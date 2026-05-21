# Chess3D Face Coordinate Frames

Chess3D now documents the cube-face local frames used by six-side setup, home slots, target projections, and Hodge Projection Duel.

Each side has a local coordinate `(u,v,w)`:

- `u,v` are coordinates on the home face;
- `w` is distance inward from the home face;
- all coordinates are in `0..7`.

| Side | Face | Local-to-global transform |
| --- | --- | --- |
| 1 | `Z0`, inward `+Z` | `(u,v,w) -> (u,v,w)` |
| 2 | `Z7`, inward `-Z` | `(u,v,w) -> (7-u,7-v,7-w)` |
| 3 | `Y0`, inward `+Y` | `(u,v,w) -> (u,w,v)` |
| 4 | `Y7`, inward `-Y` | `(u,v,w) -> (7-u,7-w,7-v)` |
| 5 | `X0`, inward `+X` | `(u,v,w) -> (w,u,v)` |
| 6 | `X7`, inward `-X` | `(u,v,w) -> (7-w,7-u,7-v)` |

The inverse transform maps global board cells back to the side's local frame. A Hodge mirror move is:

1. convert source-side global `from/to` cells to source local `from/to`;
2. convert those local coordinates to the target side's global cells;
3. validate the resulting target-side move with the normal movement generator.

This gives deterministic, bounded, bijective transforms for v0.1.
