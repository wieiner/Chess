# Chess3D Hodge Projection Transforms

P2J adds transform helpers for Hodge Projection Duel:

```text
Chess3D_TransformMoveBetweenSides(sourceSide, targetSide, from, to) -> mirrored from/to
```

The transform preserves local coordinates. For example, side 1 and side 3 share the same local move:

```text
S1 global (3,3,0)->(3,3,1)
S1 local  (3,3,0)->(3,3,1)
S3 global (3,0,3)->(3,1,3)
```

## Properties

- Bounds: valid inputs produce valid board coordinates.
- Determinism: the same source and target side always produce the same mirror cells.
- Round-trip: transforming side A to side B and back to side A returns the original cells for tested samples.
- Profile isolation: transforms are exposed through ABI, but projected composite turns are enabled only when `projectionProfile.type = hodgeTriuneProjection`.

## Caveats

The transform is a gameplay frame transform, not a full mathematical Hodge-star implementation. It is stable enough for v0.1 and can be generalized later if the game design needs orientation variants.
