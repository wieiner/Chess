# Chess3D P2H Rubik Layer Turn Audit

P2H starts from the P2G runtime: the projected board is still `Position::board[512]`, Forbidden Core cells can also hold `Game::coreStacks[index]`, fusion descriptors live in `Game::fusionStates[index]`, and reserve counts live outside the board as `reserveCounts[side][pieceType]`.

## Existing RotateLayer

Before P2H, `Chess3D_RotateLayer` rotated only the projected board when core stacks were disabled. When `coreStack` / `asgardCorePhysics` was active, it returned failure with an informational message to avoid corrupting stacks.

The existing ABI axis convention is preserved:

- `axis = 0`: fixed `z` layer, displayed as `Z`;
- `axis = 1`: fixed `y` layer, displayed as `Y`;
- `axis = 2`: fixed `x` layer, displayed as `X`.

## Missing Before P2H

- CoreCell stacks did not move with the rotated layer.
- Fusion descriptors were not recomputed after a stack-moving rotation.
- Anchors and victory were not recomputed after ritual turns.
- `rubik_convergence_3d_v0_1` exposed `ritualTurn` as data, but it was not an executable runtime action.
- Reserve counts had no explicit layer-turn invariant.

## Safe P2H Scope

P2H can safely add a profile-gated runtime path for `layerTurnProfile.type = ritualTurn`:

- rotate the projected board through a temporary snapshot;
- move whole CoreCell stacks from source core cells to rotated destination core cells;
- resynchronize projected core cells from top stack entries;
- recompute fusion, anchors, implosion progress, and centerAssembly victory;
- leave reserve counts untouched;
- keep classic/single/asgard profiles disabled for ritual turns;
- keep the legacy draft debug rotation without stacks.

The CoreCube `2..5` is invariant under 8x8 quarter turns, so a non-empty core stack in a rotated layer maps to another core cell.

