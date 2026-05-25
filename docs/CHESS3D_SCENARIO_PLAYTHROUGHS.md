# Chess3D Scenario Playthroughs

P2L adds headless playthrough descriptors under:

`assets/rules/scenarios/chess3d`

## Files

- `classic_six_side_playthrough_v0_1.json`
- `single_side_training_playthrough_v0_1.json`
- `asgard_core_playthrough_v0_1.json`
- `rubik_layer_playthrough_v0_1.json`
- `hodge_projection_playthrough_v0_1.json`

Each descriptor names a RuleProfile, expected capabilities, sample action sequence, expected action-history fragments, and expected final state. These are not replay/import files yet; they are stable smoke contracts for tests and manual QA.

`scripts/verify.ps1` checks that these descriptors are copied into both development output and `ProductionOutput`.
