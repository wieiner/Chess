# P4M Optional Rubik Model Overrides

Phase 40 adds an optional model-set selector to RubikApp while keeping the P4L
procedural renderer and physical facelets authoritative.

## Supported boundary

The catalog recognizes:

- `rubik.cubieBody`;
- `rubik.sticker`;
- `rubik.core`.

This phase applies only `rubik.cubieBody`. It may replace the reusable cubie
body shape/material through validated GLB or compatible OBJ. Every sticker is
still generated from the native facelet state, world-face orientation, and
existing shared sticker meshes. A model package therefore cannot erase the
three colors of a corner, two colors of an edge, or rotate a sticker away from
its authoritative face.

`rubik.sticker` and `rubik.core` are reported by diagnostics but remain deferred
until a format can prove per-face geometry placement without weakening the
facelet shell. Missing or invalid overrides use the current procedural body.

## Interaction and performance

The selected body prototype is loaded once. GLB parsing is asynchronous and a
new selection cancels the stale request. The immutable frozen prototype is
instanced under each cubie's existing transform. OBJ continues through the
cached loader.

Hit testing walks nested geometry descendants, so a model-group body and the
authoritative stickers resolve to the same cubie. Selection, layer animation,
and the existing N-dimensional state remain unchanged.

The override selector is visual-only and is not written into `.rubik.json`;
state files remain backward compatible and portable without optional assets.

## Verification

- the pure planner proves procedural and body-only fallback behavior;
- sticker authority remains true for every plan;
- the existing Rubik contracts continue to cover N=2, N=3, N=8, N=11,
  solved/scrambled/imported states, selection, and animation;
- RubikApp x64 Release builds with the shared catalog and WPF model libraries.

No approved Rubik override asset is included yet, so the default UI remains the
P4L procedural renderer.
