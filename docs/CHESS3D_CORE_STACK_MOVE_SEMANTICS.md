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

P2E does not rotate stacks with Rubik layer turns. When core stacks are enabled, `Chess3D_RotateLayer` fails cleanly with an informational message instead of corrupting stack state.

P2H will define layer turns that move pieces and stacks together.

## Known Limits

Movement legality still uses the projected board. This means the stack model is safe and ABI-compatible, but not yet a complete Asgard physics simulator. Fusion descriptors and reserve counts now exist, but dislodge, reserve restore, and destructive implosion are future layers.
