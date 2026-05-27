# Chess3D Hodge Mirror Arrow Visuals

P3B visualizes Hodge Projection Duel mirror moves without changing Hodge rules.

## Preview

The Hodge panel still uses the existing transform ABI. When the user previews a primary move, the viewport draws:

- primary dotted arrow;
- two mirror dotted arrows;
- blocked/invalid arrows in red when the all-or-nothing move fails.

## Apply

Successful projected moves flash the primary and mirror paths together, then rebuild the board from engine state. Failed projected moves keep the board unchanged and leave the blocked arrows visible with the engine error in the panel/status text.

## Scope

This is not a new Hodge editor, replay timeline, or AI/search layer. It is visual feedback over the existing all-or-nothing composite action.
