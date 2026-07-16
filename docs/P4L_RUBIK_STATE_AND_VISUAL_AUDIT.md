# P4L Standalone Rubik State And Visual Audit

Date: 2026-07-16  
Scope: standalone `RubikApp` and `RubikEngine`, not the Chess3D Rubik
Convergence RuleProfile.

## Executive Finding

The current engine is a reliable **cubie-position permutation and trusted move
history model**, but it is not a physical sticker cube model. The visible
one-color corners and edges are not a missing texture accident: neither the
native state nor the renderer currently represents multiple sticker faces or
cubie orientation.

The existing integer-cell ABI and reverse-history solver remain useful and
must be preserved. The correct path is an append-only facelet/orientation layer
plus separate WPF body/sticker geometry.

## Native State Model

`Engine` stores:

- `size`, clamped to 2..32;
- `cells`, exactly `size^3` integers;
- normalized layer move history;
- last move/info/command text;
- a `manualState` flag.

Solved reset assigns `cells[i] = i`. Indexing is
`z * N * N + y * N + x`. A layer turn copies the integer IDs from a snapshot
into rotated coordinates. `isSolved` tests only whether every integer ID has
returned to its original index.

What the state does **not** contain:

- a face enum or six `N*N` face arrays;
- sticker color IDs;
- a local orientation/basis for each cubie;
- corner twist or edge flip state;
- center/wing group identity beyond an integer's solved coordinate;
- physical-state parity/solvability metadata.

Consequently two physically different sticker orientations can have the same
current integer-cell representation. Arbitrary facelet input cannot be mapped
losslessly into the current state.

## Rotation Path

`Rubik_RotateLayer` validates axis/layer, normalizes quarter turns, and applies
the same 2D square transform to every cubie ID in the selected X/Y/Z slice. It
records the move unless the operation is internal.

This is sufficient for:

- cubie position animation;
- reproducible scramble by seed;
- four-turn and inverse position identities;
- reverse-history solving.

It is insufficient for a physical cube because the same operation never
rotates a cubie's local sticker-face mapping. An ID moves, but no orientation
state changes with it.

## Current Visual Path

`RefreshScene` reads integer cells and creates one `GeometryModel3D` box per
visible coordinate. `CreateCube` delegates to `CreateBox`, which assigns one
`MaterialGroup` to the whole mesh. All six sides therefore share one color.

`ColorForCell` derives the original solved coordinate from the integer ID and
uses first-match boundary logic:

1. `x == 0`;
2. `x == N-1`;
3. `y == 0`;
4. `y == N-1`;
5. `z == 0`;
6. `z == N-1`.

For a corner, the first X condition wins and the other two physical stickers
are discarded. For an edge, whichever earlier axis matches wins. This exactly
explains monochrome corners/edges. It also means the color follows the cubie ID
as a single swatch rather than following independently rotated stickers.

Microsoft WPF `GeometryModel3D` has one front `Material` for its mesh. A later
correct renderer should use a reusable dark body model plus independently
colored, slightly offset sticker quads grouped under one hit-test owner. It
should render only exposed stickers, reuse/freeze geometry and materials, and
avoid adding stickers to internal faces.

## Hit Testing And Animation Risk

The current `_cubeHitMap` maps each body `GeometryModel3D` to one `CubeVisual`.
Layer selection and animation assume one model per cubie. Adding sticker models
without a grouping/ownership plan would make clicks ambiguous or leave stickers
behind during transform animation.

The safe later contract is:

- one logical cubie visual owns body and sticker child models;
- every child resolves to that owner for hit testing, or sticker children are
  hit-test transparent through the scene policy;
- the entire group receives one layer animation transform;
- after commit, visuals rebuild from native facelets/orientation;
- only surface cubies/stickers are created in surface-only mode.

## Manual State And File Boundary

The current UI `Export` writes whitespace-formatted integer IDs to the shared
text box. `Load State` parses integers and requires only `size^3` values before
calling `Rubik_SetCells`.

Native `Rubik_SetCells` copies values without validating that they form a
unique 0..`size^3-1` permutation. `Rubik_SetCell` likewise permits duplicate
IDs. Both clear history and set `manualState=true`. Therefore current text
state is:

- not versioned;
- not self-describing (size and color scheme are external UI context);
- not physical-cube compatible;
- not atomically written to disk;
- not sufficiently validated as even an integer permutation;
- unable to preserve unknown metadata or source information.

It must remain a legacy/debug import surface until the versioned facelet JSON
path is implemented.

## Solver Boundary

`SolveByReverseHistory` reverses the recorded move vector and inverts each
quarter-turn count. This is correct for states reached through trusted recorded
rotations. It deliberately returns `-1` for `manualState` because no trusted
history is attached.

It is not an arbitrary-state solver and cannot become one merely by accepting
more integer IDs. An arbitrary solver requires:

- validated facelets and color scheme;
- complete cubie/sticker orientation;
- physical solvability checks;
- a size-aware solver plugin with cancellation/resource limits;
- replay of the returned solution against a copy and solved-hash verification.

The reverse-history solver should remain as a fast, deterministic plugin and
regression oracle.

## Existing ABI And Tests

The public C ABI exposes creation, size/reset, state DTO, integer cells,
individual edits, slice rotation, scramble, history, reverse-history solution,
move application, and text diagnostics. The C# wrapper mirrors it with Cdecl
P/Invoke. No facelet or orientation functions exist.

Current contract coverage proves:

- 8x8 creation/reset;
- one turn changes position;
- four identical turns restore integer solved state;
- seeded scramble reproducibility;
- reverse-history solution replay;
- manual edit marks manual mode and rejects reverse-history solve.

It does not test sticker counts, face orientation, color conservation,
multi-color corners/edges, parity, arbitrary import, or portable JSON.

## Four Separate Gaps

| Gap | Current cause | Required direction |
| --- | --- | --- |
| Logical state | Position IDs only; no facelets/orientation | Append-only canonical facelet and discrete cubie orientation state |
| Visual | One material on one cubie mesh; first-match color | Dark body plus separate exposed sticker geometries |
| File format | Unversioned integer text in UI textbox | Transactional versioned `.rubik.json`, separate moves/session formats |
| Solver | Inverse trusted history only | Validated plugin architecture, small-cube solver, then NxN reduction |

Fixing only `ColorForCell` would at best paint a different single color and
would not solve any of the other three gaps.

## Safe Implementation Order

1. Freeze face/axis/turn conventions and facelet mapping.
2. Define file and compatibility boundaries.
3. Add facelet ABI/state append-only while preserving integer cells.
4. Rotate facelets and track discrete cubie orientation in lockstep.
5. Add invariant tests across representative sizes.
6. Replace solid cubie rendering with grouped body/sticker visuals.
7. Add transactional JSON and a physical face editor.
8. Validate solvability before introducing arbitrary-state solver plugins.

No engine, ABI, renderer, online protocol, RuleProfile, or server deployment is
changed by this audit phase.

