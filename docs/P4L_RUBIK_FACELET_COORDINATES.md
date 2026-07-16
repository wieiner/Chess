# P4L Rubik Facelet Coordinates

Date: 2026-07-16  
Status: canonical contract for future standalone Rubik facelet/orientation code.

## Scope

This document defines the physical face, coordinate, color, and turn
conventions for `RubikApp` sizes 2 through 32. It does not change the current
integer-cell ABI or the separate Chess3D Rubik Convergence profile.

Portable files and physical-cube UI use WCA-style U/R/F/D/L/B names. Legacy
engine axis/layer moves remain a separate compatibility representation.

## World Coordinates And Handedness

The cube uses a right-handed WPF world basis:

- `+X`: right;
- `+Y`: up;
- `+Z`: front, toward the initial visible front side;
- `X cross Y = Z`.

Integer cubie coordinates are `x,y,z in [0,N-1]` and retain the existing
linear index:

```text
index = z * N * N + y * N + x
```

Faces and outward normals:

| Face | Coordinate plane | Outward normal | Opposite |
| --- | --- | --- | --- |
| U | `y = N-1` | `+Y` | D |
| R | `x = N-1` | `+X` | L |
| F | `z = N-1` | `+Z` | B |
| D | `y = 0` | `-Y` | U |
| L | `x = 0` | `-X` | R |
| B | `z = 0` | `-Z` | F |

## Canonical Solved Color Scheme

The default portable scheme is:

| Face | Color name | Compact ID |
| --- | --- | ---: |
| U | white | 1 |
| R | red | 2 |
| F | green | 3 |
| D | yellow | 4 |
| L | orange | 5 |
| B | blue | 6 |

ID `0` means no sticker/plastic in internal descriptors. Portable face arrays
must contain only IDs 1..6 unless a later schema explicitly adds a temporary
editor value. Opposite pairs are white/yellow, red/orange, and green/blue.

Custom color names may be metadata aliases, but physical identity remains the
face/color ID mapping above.

## Face Grid Viewing Rule

Every `face[row][column]` grid is viewed directly from outside that face:

- row 0 is the visual top;
- column 0 is the visual left;
- rows and columns are zero-based and row-major;
- the adjacent orientation used as visual up is fixed by the table below.

```text
             U (B edge at top)
L (B left)  F            R (F left)  B
             D (F edge at top)
```

The text net is only a placement cue. The formulas, not folding intuition, are
the authoritative mapping.

Let `M = N-1`, `r = row`, and `c = column`:

| Face | Cubie coordinate `(x,y,z)` | Grid-right world direction | Grid-down world direction |
| --- | --- | --- | --- |
| U | `(c, M, r)` | `+X` | `+Z` |
| R | `(M, M-r, M-c)` | `-Z` | `-Y` |
| F | `(c, M-r, M)` | `+X` | `-Y` |
| D | `(c, 0, M-r)` | `+X` | `-Z` |
| L | `(0, M-r, c)` | `+Z` | `-Y` |
| B | `(M-c, M-r, 0)` | `-X` | `-Y` |

Inverse mapping for a coordinate known to lie on a face:

| Face | `row` | `column` |
| --- | --- | --- |
| U | `z` | `x` |
| R | `M-y` | `M-z` |
| F | `M-y` | `x` |
| D | `M-z` | `x` |
| L | `M-y` | `z` |
| B | `M-y` | `M-x` |

## Corner Examples

These examples make mirrored back/right views explicit:

| Face cell | Coordinate | Adjacent physical corner |
| --- | --- | --- |
| `F[0][0]` | `(0,M,M)` | U-L-F |
| `F[0][M]` | `(M,M,M)` | U-R-F |
| `R[0][0]` | `(M,M,M)` | U-R-F |
| `R[0][M]` | `(M,M,0)` | U-R-B |
| `B[0][0]` | `(M,M,0)` | U-R-B |
| `B[0][M]` | `(0,M,0)` | U-L-B |
| `U[M][0]` | `(0,M,M)` | U-L-F |
| `D[0][M]` | `(M,0,M)` | D-R-F |

A corner therefore owns three local face stickers, not one blended or
priority-selected color.

## Facelet Linear Layout

The append-only ABI and `.rubik.json` conversion use this order:

```text
U, R, F, D, L, B
```

Each face contributes `N*N` row-major entries. The total count is always
`6*N*N`. The linear index is:

```text
faceletIndex = faceOrdinal * N * N + row * N + column
```

Face ordinals are U=0, R=1, F=2, D=3, L=4, B=5. Ordinals and compact color IDs
are different concepts and must not be interchanged.

## Cubie Classes And Counts

For a cubie coordinate, count how many components are either 0 or M:

| Boundary components | Class | Sticker count on that cubie | Cubie count |
| ---: | --- | ---: | ---: |
| 3 | corner | 3 | 8 |
| 2 | edge/wing | 2 | `12*(N-2)` |
| 1 | center | 1 | `6*(N-2)^2` |
| 0 | internal/core | 0 | `(N-2)^3` |

The cubie counts sum to `N^3`; the sticker counts sum to `6*N^2`.

Examples:

| N | corners | edges/wings | centers | internal | facelets |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 2 | 8 | 0 | 0 | 0 | 24 |
| 3 | 8 | 12 | 6 | 1 | 54 |
| 4 | 8 | 24 | 24 | 8 | 96 |
| 11 | 8 | 108 | 486 | 729 | 726 |
| 32 | 8 | 360 | 5400 | 27000 | 6144 |

For N=3, the one position between the corners on each edge is the ordinary
edge cubie. For larger cubes, non-corner positions along an edge are wing
pieces; odd N has one central edge position per edge and paired wings around
it.

## Odd And Even Cubes

Odd N:

- each face has one exact center at `(row,column)=(N/2,N/2)`;
- that center identifies the face/color orientation for a solid-color physical
  cube;
- other center pieces remain movable;
- the internal coordinate `(N/2,N/2,N/2)` exists but has no sticker.

Even N:

- no single facelet is a fixed visual center;
- face colors alone do not determine the whole-cube orientation without an
  external color-scheme/orientation choice;
- center pieces form movable groups;
- physical import must require or propose an explicit U/F orientation rather
  than silently guessing.

The term `fixed center` is physical puzzle structure, not permission to skip
that facelet in the state array.

## Canonical WCA Face Turns

Following WCA Article 12, an unprimed face move is 90 degrees clockwise while
looking directly at that named face from outside. On that face grid:

```text
clockwise:        (r,c) -> (c, M-r)
counter-clockwise:(r,c) -> (M-c, r)
half turn:        (r,c) -> (M-r, M-c)
```

Canonical WCA face moves convert to the current internal axis/layer storage as:

| WCA move | Axis/layer | Internal quarterTurns |
| --- | --- | ---: |
| U | Y / `M` | 1 |
| D | Y / `0` | 3 |
| R | X / `M` | 3 |
| L | X / `0` | 1 |
| F | Z / `M` | 3 |
| B | Z / `0` | 1 |

Prime inverts 1 and 3; `2` remains 2. An n-wide block repeats the same
face-relative direction for the first n layers measured inward from that face.
Whole-cube x/y/z rotations use R/U/F direction respectively across all layers.

## Legacy Internal Axis Turns

The existing native ABI is coordinate-based, not WCA-based. For
`quarterTurns=1`, it applies these exact formulas:

```text
axis Z: (x,y,z) -> (M-y, x,   z)
axis Y: (x,y,z) -> (M-z, y,   x)
axis X: (x,y,z) -> (x,   M-z, y)
```

Repeated application defines turns 2 and 3. These formulas and old integer
history must remain readable.

The current managed `RubikNotation` maps R and F directly to internal turn 1,
which is opposite canonical outside-view WCA clockwise under the coordinate
contract above. U already matches. This discrepancy is now explicit:

- do not silently reinterpret stored legacy coordinate history;
- add conversion tests before changing face-token parsing;
- version portable move files with their notation convention;
- keep `Xn/Yn/Zn` coordinate notation as an unambiguous legacy/internal form;
- new physical facelet files always use the canonical face-grid convention in
  this document.

## Sticker Ownership And Orientation

A cubie carries a sticker for every boundary face at its solved coordinate.
After turns, sticker ownership stays with that cubie while its local face
directions rotate discretely. Facelet arrays are the projected colors currently
visible on world U/R/F/D/L/B planes.

The later engine may store both:

- canonical facelets as import/export truth;
- a discrete cubie orientation basis as renderer/solver truth.

They must be mutually derivable and checked after every move. No floating-point
quaternion is permitted as authoritative logical orientation.

## Required Contract Tests Before Implementation

1. Every face has exactly `N*N` unique `(coordinate,localFace)` pairs.
2. All six faces total `6*N*N` facelets.
3. Corner/edge/center/internal counts match the formulas for 2,3,4,5,8,11,32.
4. Mapping face to coordinate and back is identity for every cell.
5. A canonical face quarter turn maps grid cells by the clockwise formula.
6. A turn plus inverse, four quarter turns, and two half turns are identity.
7. Opposite color counts and solved face uniformity hold.
8. Legacy axis histories preserve their exact old coordinate transforms.
9. Portable WCA tokens use explicit conversion rather than legacy sign
   assumptions.

