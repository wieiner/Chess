# P4L Rubik Sticker Rendering

## Result

`RubikApp` no longer assigns one boundary-derived color to an entire cubie.
Each rendered cubie is a `Model3DGroup` containing:

- one dark plastic body;
- zero to three separate sticker quads;
- one shared group transform used by layer animation.

The visible physical classes are therefore represented directly:

- corner: three stickers;
- edge or wing: two stickers;
- center: one sticker;
- internal cubie: plastic only when internal rendering is enabled.

The physical sticker mask comes from the cubie identity and is independent of
the cubie's current position. Its exact integer orientation basis maps each
local sticker normal to a world face. Sticker colors then come from the current
facelet on that world face. This is the visible fix for monochrome corners and
edges without losing turn orientation.

## Geometry and materials

The body uses a neutral charcoal material rather than a face color. A sticker
is a two-triangle square at 74 percent of the body width, offset slightly beyond
the plastic surface. The offset and plastic border prevent z-fighting and keep
clear gaps between neighboring stickers.

Schema version 1 colors use readable materials:

- U: warm ivory;
- R: red;
- F: green;
- D: yellow;
- L: orange;
- B: blue.

Body and sticker materials combine diffuse and restrained specular terms.
Brushes and material groups are frozen after construction. Selected-layer
highlighting brightens the plastic and lightly blends sticker colors without
replacing their identity.

## Facelet-to-world lookup

For a cubie at current `(x,y,z)`, the oriented physical sticker must point to a
boundary world face. The renderer uses the inverse formulas from
`P4L_RUBIK_FACELET_COORDINATES.md` to read that facelet color:

| World face | Boundary | Face row/column |
| --- | --- | --- |
| U | `y=M` | `(z,x)` |
| R | `x=M` | `(M-y,M-z)` |
| F | `z=M` | `(M-y,x)` |
| D | `y=0` | `(M-z,x)` |
| L | `x=0` | `(M-y,z)` |
| B | `z=0` | `(M-y,M-x)` |

A validated facelet-only state has no proven physical identity/orientation. In
that case the renderer explicitly switches to a canonical shell fallback that
uses these same world-face formulas and reports the fallback in diagnostics.

## Hit testing and animation

Every body and sticker `GeometryModel3D` maps to the same logical `CubeVisual`
in `_cubeHitMap`. Clicking a sticker therefore selects the same cubie/layer as
clicking its plastic body. Sticker overlays do not become independent actions.

Layer animation applies one transform to the parent `Model3DGroup`; body and
all stickers rotate together. After native commit, the existing scene refresh
rebuilds from authoritative facelets, so no animated offset accumulates.

Surface-only mode and selected-layer behavior are preserved. Interior cubies
carry no stickers.

## Diagnostics

The status line reports:

- rendered cubie count;
- rendered sticker count;
- rendered corner/edge/center counts;
- invalid or unavailable sticker count.

A legacy integer-only edit has no sticker orientation, so the renderer shows
plastic and counts unavailable stickers instead of inventing colors. Reset or
a validated facelet load restores physical sticker rendering.

## Performance boundary

The default remains surface-only rendering. For N=11 this bounds physical
stickers to `6*N*N = 726` and avoids rendering `(N-2)^3` internal bodies. Phase
08 adds explicit visual fixtures; later optimization may cache reusable local
meshes or batch surfaces if large-N frame measurements require it.
