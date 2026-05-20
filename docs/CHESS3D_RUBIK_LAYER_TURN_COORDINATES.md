# Chess3D Rubik Layer Turn Coordinates

The engine preserves the historical ABI axis mapping:

```text
0 = Z layer, fixed z
1 = Y layer, fixed y
2 = X layer, fixed x
```

This differs from a naive `0=X,1=Y,2=Z` enum, so tests and wrappers should use the ABI mapping above.

## Invariants

The Forbidden Core is `x/y/z = 2..5`. A quarter turn of an 8x8 layer maps the middle `4x4` square to itself. Therefore:

- core cells rotate to core cells;
- non-core cells rotate to non-core cells;
- a CoreCell stack can be relocated as a whole without splitting entries;
- reserve counts do not participate in geometry and never rotate.

## World-Fixed Targets

P2H keeps target slots fixed in world coordinates. Rotating a stack may remove or create anchors depending on where the stack lands after the turn.

