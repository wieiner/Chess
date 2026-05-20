# Chess3D Fusion Model

P2F introduces runtime fusion descriptors over CoreCell stacks.

## Descriptor, Not Merge

Fusion does not delete entries, combine pieces into one `pieceCode`, or rewrite stack storage. Stack entries remain the source of truth. Fusion is a computed state for one Forbidden Core cell.

## CoreFusionState

The runtime descriptor contains:

- `fusionKind`;
- `ownerSide`;
- `sideMask`;
- `entryCount`;
- `friendlyCount`;
- `enemyCount`;
- `dominantPieceType`;
- `flags`;
- internal `implosionStage`.

## Fusion Kinds

- `none`: empty, outside core, or fusion disabled.
- `single`: one stack entry.
- `friendlyPair`: two entries from one side.
- `friendlyStack`: three or more entries from one side.
- `royalPair`: king and queen of one side share a core cell.
- `contested`: entries from more than one side.
- `mixedStack`: reserved.
- `implosionSeed`: reserved as a future kind; P2F currently exposes seed through flags.
- `implosionReady`: reserved.

## Friendly Fusion

If a core cell has two same-side entries and no enemy entries, it becomes `friendlyPair`. If it has three or more, it becomes `friendlyStack`. If it contains king and queen of the same side, it becomes `royalPair`.

## Enemy Co-Occupancy

If entries from different sides share a core cell, the cell is `contested`. Ordinary capture inside the core remains disabled for stack-enabled profiles, so both sides' entries remain in the stack.

## Implosion

P2F implosion is progress state only. It marks that assembly is developing, but it does not remove, transform, or animate pieces.

## 216 Principle

The Volume-Surface 216 Principle remains future/disabled metadata. P2F does not use it for victory.

## P2G Capture Interaction

P2G does not make fusion destructive.

When a piece enters the Forbidden Core, existing occupants remain in the stack. If entries from different sides share the cell, fusion reports contested state. Knockback applies only to ordinary outside destinations, including core-to-outside captures against an enemy outside piece.

## P2H Layer-Turn Interaction

P2H moves whole CoreCell stacks during Rubik convergence ritual layer turns. Fusion descriptors are recomputed after the stack relocation, so a `friendlyPair`, `royalPair`, or `contested` state follows the moved stack to its new core cell. Layer turns do not merge, split, or destroy stack entries.
