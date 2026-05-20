# Chess3D P2E CoreCell Stack Runtime

P2E implements runtime stacks inside the Forbidden Core while preserving the old projected board ABI.

## Runtime Shape

The native game state now has:

```text
Position::board: projected 512-int board
Game::coreStacks: 512 optional vectors of CoreStackEntry
```

Only core cells use non-empty stacks. The projection is synchronized after stack mutations.

## Enabled Profiles

Stacks are enabled for profiles with:

- `occupancyProfile.type = coreStack`; or
- `corePhysicsProfile.type = asgardCorePhysics`.

This includes:

- `asgard-convergence-3d-8x8x8-v0.1`;
- `rubik-convergence-3d-8x8x8-v0.1`.

## Reset and Profile Loading

`Reset`, `Clear`, `SetBoard`, and profile loading keep stacks consistent:

- reset clears explicit stacks;
- clear clears board and stacks;
- set board clears existing stacks and creates one-entry stacks for occupied core projection cells when stack mode is enabled;
- profile loading resets the game state.

## Anchor Integration

Anchor recomputation is now stack-aware:

- if stacks are enabled, each target slot searches every entry in the stack;
- a slot counts once even if multiple matching pieces exist in the same cell;
- wrong side or wrong piece type does not anchor;
- victory still uses `anchorCount >= requiredAnchorCount`.

## Deferred Mechanics

P2E does not implement:

- fusion entities;
- implosion;
- contested anchor resolution;
- knockback/reserve;
- dislodging;
- Volume-Surface 216 victory;
- Rubik rotations moving stacks.

These remain staged for P2F and later.
