# P4L Rubik Facelet Rotation

## Purpose

Every native layer turn now updates cubie IDs and physical facelets as one
logical operation. The implementation covers outer faces, inner slices, wide
turns, and whole-cube rotations for every supported size without a table of
size-specific strip cases.

## Representation used during a turn

The canonical face array remains `U,R,F,D,L,B`, row-major. For rotation, each
entry is temporarily interpreted as:

```text
Sticker = (cubie x/y/z coordinate, outward unit normal, color ID)
```

The face-to-coordinate formulas are defined in
`P4L_RUBIK_FACELET_COORDINATES.md`. A sticker belongs to a layer when its cubie
coordinate on the selected axis equals the layer index.

For one positive internal quarter turn, coordinates and normals use the same
discrete transforms:

| Axis | Coordinate transform | Normal transform |
| --- | --- | --- |
| Z | `(x,y,z) -> (M-y,x,z)` | `(nx,ny,nz) -> (-ny,nx,nz)` |
| Y | `(x,y,z) -> (M-z,y,x)` | `(nx,ny,nz) -> (-nz,ny,nx)` |
| X | `(x,y,z) -> (x,M-z,y)` | `(nx,ny,nz) -> (nx,-nz,ny)` |

Here `M=N-1`. Turns 2 and 3 repeat the same exact integer transform. The new
normal selects the destination face; the destination coordinate is then
converted back to that face's row and column.

This keeps the existing engine axis convention stable. Portable WCA notation
continues to use the explicit sign conversion documented in Phase 02 rather
than changing native history semantics.

## Atomicity and legacy state

When synchronized facelets are available, the engine computes the entire next
facelet vector before changing cubie cells. A mapping failure rejects the turn
without changing either representation. After successful computation, cubie
and facelet vectors commit within the same native call and existing history is
recorded once.

`Rubik_SetCells` and `Rubik_SetCell` still mark facelets unsynchronized because
integer cubie IDs contain no sticker orientation. Turns remain available for
those legacy states, but facelet reads continue to fail explicitly until reset
or a validated facelet state is loaded. This is a compatibility boundary, not
a synthesized orientation.

## Wide and whole-cube turns

The ABI remains one axis/layer action. Existing notation expands:

- a wide turn into the required adjacent layer actions;
- a whole-cube rotation into all N layer actions.

Because every layer uses the same rigid sticker transform, no additional
facelet code path is needed. Parallel layers on the same axis commute; history
and playback retain their existing ordered action sequence.

## Verification

Native contract coverage includes N=2,3,4,5,8,11 and asserts:

- a turn followed by its inverse restores facelets and cubie IDs;
- four quarter turns restore identity;
- two half turns restore identity;
- inner slice, wide, and whole-cube roundtrips restore identity;
- every turn preserves six exact `N*N` color counts;
- cubie IDs remain a permutation;
- explicit X, Y, and Z strip-direction fixtures match the canonical mapping;
- reverse-history replay still returns a scramble to the solved state.

Depth, search, solver strategy, and physical-state parity validation are not
part of this phase.
