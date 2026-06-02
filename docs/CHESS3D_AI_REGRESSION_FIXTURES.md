# Chess3D AI Regression Fixtures

P3D adds runnable playthrough regression JSON under `assets/rules/scenarios/chess3d/regression`.

## Added Fixtures

- `classic_ai_avoids_self_check_v0_1.json`
- `classic_ai_finds_capture_or_mate_smoke_v0_1.json`
- `single_side_ai_smoke_v0_1.json`
- `asgard_ai_stack_fusion_smoke_v0_1.json`
- `asgard_ai_reserve_restore_smoke_v0_1.json`
- `rubik_ai_layer_turn_candidate_v0_1.json`
- `rubik_ai_four_turn_no_regression_v0_1.json`
- `hodge_ai_projected_candidate_smoke_v0_1.json`
- `hodge_ai_blocked_projection_no_mutation_v0_1.json`
- `ai_search_no_mutation_all_profiles_v0_1.json`

## Runner Support

The headless playthrough runner now supports:

- `buildAiCandidates`;
- `assertAiCandidateKind`;
- `searchBestAiNoMutation`;
- `makeBestAiAction`.

These fixtures verify profile isolation, no-mutation search, Rubik layer-turn candidate visibility, Asgard reserve restore candidate visibility, and Hodge projected composite search.
