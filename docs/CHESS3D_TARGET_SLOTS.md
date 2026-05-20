# Chess3D Target Slots

P2D defines typed logical target slots for centerAssembly profiles.

## CoreCube

The default and current Asgard/Meru core is:

```text
x = 2..5
y = 2..5
z = 2..5
```

Profiles may define this through `coreProfile.coreCube`. If omitted, the runtime uses the same `2..5` fallback.

## Six-side projection

Each side has 16 logical target slots projected onto one plane of the core:

- side 1 / Z0 gate: `z = 2`, local board maps to `x=2..5, y=2..5`;
- side 2 / Z7 gate: `z = 5`, local board maps to `x=2..5, y=2..5`;
- side 3 / Y0 gate: `y = 2`, local board maps to `x=2..5, z=2..5`;
- side 4 / Y7 gate: `y = 5`, local board maps to `x=2..5, z=2..5`;
- side 5 / X0 gate: `x = 2`, local board maps to `y=2..5, z=2..5`;
- side 6 / X7 gate: `x = 5`, local board maps to `y=2..5, z=2..5`.

## Typed pattern

All sides use the P2A central 4x4 pattern:

```text
y=5:  N  P  P  R
y=4:  P  Q  K  P
y=3:  P  B  B  P
y=2:  R  P  P  N
       x=2 3 4 5
```

For Y/X faces, the same local pattern is projected onto the target plane.

## Type matching, not identity matching

The current board stores only `side * 10 + type`; it does not track unique piece identity. Therefore target matching is type-based:

- any pawn of the side can satisfy a pawn slot;
- any rook of the side can satisfy a rook slot;
- any same-type piece of the side can satisfy a same-type slot.

Unique piece identity is deferred.

## Contested target regions

Different sides can project into overlapping physical core cells. That is intentional for the future Asgard/Meru design.

P2E adds CoreCell stacks for stack-enabled profiles, so overlapping target regions can now contain multiple side/type entries in one Forbidden Core cell. The legacy board projection still shows only the top stack entry.

P2F adds fusion descriptors, so a contested cell can now be detected and reported. Contested-anchor scoring and dislodging rules are still later work.
