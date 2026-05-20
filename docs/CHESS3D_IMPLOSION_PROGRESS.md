# Chess3D Implosion Progress

P2F treats implosion as a progress descriptor, not an event.

## Runtime Formula

For profiles with `implosionProfile.mode = progressState`, side progress is currently:

```text
anchorCount + friendlyFusionCount + royalPairBonusCount
```

This is intentionally simple and stable for contract tests.

## Meaning

Progress means the side is assembling a stronger central formation. It does not:

- destroy pieces;
- merge entries;
- trigger animations;
- apply Volume-Surface 216 victory;
- override normal centerAssembly victory.

## Reset

Reset and clear operations recompute progress from current stacks. Empty core stacks produce zero implosion progress.

## Future

P2F leaves real implosion behavior, six-gate coronation, surface/volume completion, and transformation rules to later stages.
