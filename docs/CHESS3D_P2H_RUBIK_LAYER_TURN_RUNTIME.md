# Chess3D P2H Rubik Layer Turn Runtime

P2H turns `rubik-convergence-3d-8x8x8-v0.1` layer turns into runtime behavior.

## Implemented

- `rubik_convergence_3d_v0_1.json` enables `layerTurnProfile.type = ritualTurn`.
- Valid axes are ABI `0=Z`, `1=Y`, `2=X`.
- Valid layers are `0..7`.
- Valid quarter turns are `-1` and `+1`.
- The projected 512-cell board rotates through a snapshot.
- Whole CoreCell stacks move with their source cells.
- Projected core cells are resynchronized from top stack entries.
- Fusion descriptors are recomputed after successful turns.
- Anchors, implosion progress, and centerAssembly victory are recomputed after successful turns.
- Reserve counts are unaffected.
- `actionCost = oneTurn` advances `sideToMove` for ritual turns.

## Profile Gating

- Rubik convergence: enabled.
- Asgard convergence: disabled and clean-fails.
- Classic six-side: disabled.
- Single-side training: disabled.
- Legacy draft profile: keeps old non-stack debug rotation for compatibility.

## Deferred

- UI animation.
- Drag-based layer-turn input.
- Notation/replay.
- Online serialization.
- AI/search generation of layer-turn actions.
- Reserve restore action.
- Destructive implosion.
- GPU stack snapshots.

