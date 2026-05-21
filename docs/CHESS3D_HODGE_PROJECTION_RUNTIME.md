# Chess3D Hodge Projection Runtime

P2J implements the first runtime version of Hodge Projection Duel.

## Runtime Profile

The profile file is:

```text
assets/rules/profiles/hodge_projection_duel_3d_v0_1.json
```

The engine parses:

- `projectionProfile.type`;
- `enabled`;
- `mirrorPolicy`;
- `actionHistoryMode`;
- two macro-player groups with three side ids each.

Classic, single-side, Asgard, and Rubik profiles explicitly set `projectionProfile.type = none`.

## Composite Move

`Chess3D_TryMakeProjectedMove` performs:

1. verify projection mode is enabled;
2. verify the primary side belongs to the current macro-player;
3. transform the primary `from/to` cells to the two mirror sides;
4. validate all three moves using existing legal move generation;
5. reject source/destination collisions;
6. apply all three moves;
7. advance the turn to the first side of the other macro-player;
8. append one composite action-history record.

Failed projected moves do not mutate board state and do not append history.

## Deferred

- projection-aware UI controls;
- replay/import/export;
- online serialization;
- AI/search over composite turns;
- hybrid projection plus Asgard/Rubik physics.
