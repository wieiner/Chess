# Chess3D Rubik Stack Rotation

P2H moves CoreCell stacks as whole values during a ritual layer turn.

## Algorithm

1. Validate axis, layer, quarter turn, and profile.
2. Validate every non-empty stack in the layer maps from a core cell to a core cell.
3. Rotate the projected board using a temporary board snapshot.
4. Rotate the stack overlay using a temporary stack snapshot.
5. Resynchronize projected core cells from the top entry of each moved stack.
6. Recompute fusion descriptors.
7. Recompute anchors, implosion progress, and compatible victory.
8. Preserve reserve counts.

## Projection

The old projected board still shows the top stack entry:

```text
projected piece = last pushed stack entry
```

If a moved stack is empty, the projected core cell is `0`.

## Not Implemented

P2H does not split stacks, rotate individual entries, serialize turns for online play, animate the layer, or apply destructive fusion/implosion.

