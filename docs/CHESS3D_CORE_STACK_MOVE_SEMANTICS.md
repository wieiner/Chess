# Chess3D Core Stack Move Semantics

P2E integrates CoreCell stacks into `TryMakeMove` conservatively.

## Outer To Outer

If source and target are outside the Forbidden Core:

- classic profiles keep old movement and capture behavior;
- Asgard/Rubik convergence profiles route enemy captures through P2G knockback/home-or-reserve.

## Entering Core

If source is outside the core and target is inside a stack-enabled core:

- normal movement legality is still checked against the projected board;
- ordinary capture in the core is disabled;
- the moving piece is appended to the target stack;
- existing target occupants remain;
- source is cleared;
- target projection becomes the moved piece.

## Core To Core

If source and target are both inside the core:

- the projected/top source entry moves;
- that entry is removed from the source stack;
- the entry is appended to the target stack;
- ordinary capture is not performed in the core.

## Leaving Core

If source is inside the core and target is outside:

- the projected/top source entry moves as a normal piece;
- the entry is removed from the source stack;
- the outside target uses classic capture or P2G knockback/home-or-reserve according to the active profile.

## Rubik Layer Turns

P2H rotates stacks for `rubik_convergence_3d_v0_1`:

- whole CoreCell stacks move with the rotated layer;
- projected core cells are resynchronized from the moved stacks;
- ordinary capture is not involved in a layer turn;
- fusion, anchors, implosion progress, and compatible victory are recomputed;
- reserve counts are unaffected.

Profiles with `layerTurnProfile.type = disabled` clean-fail for ritual turns.

## Known Limits

Movement legality still uses the projected board. This means the stack model is safe and ABI-compatible, but not yet a complete Asgard physics simulator. Fusion descriptors and reserve counts now exist, but dislodge, reserve restore, and destructive implosion are future layers.
