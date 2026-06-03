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

## P3D.1 Added Fixtures

- `classic_ai_iterative_depth2_smoke_v0_1.json`
- `classic_ai_node_limit_smoke_v0_1.json`
- `classic_ai_quiescence_capture_recapture_v0_1.json`
- `single_side_ai_ordering_deterministic_v0_1.json`
- `asgard_ai_anchor_eval_smoke_v0_1.json`
- `asgard_ai_tt_or_hash_no_mutation_v0_1.json`
- `rubik_ai_layer_turn_ordering_smoke_v0_1.json`
- `rubik_ai_search_does_not_break_four_turn_v0_1.json`
- `hodge_ai_macro_eval_smoke_v0_1.json`
- `hodge_ai_timeout_no_partial_apply_v0_1.json`
- `ai_summary_json_v2_all_profiles_v0_1.json`
- `ai_search_repeated_no_history_growth_v0_1.json`

The P3D.1 fixtures cover iterative completed-depth reporting, node-limit stop behavior, deterministic ordering, summary JSON v2 fields, quiescence-lite smoke, and repeated search no-mutation/no-history-growth. They keep all five real Chess3D RuleProfiles isolated and do not introduce a new mode.

## P3D.1 Runner Support

The headless playthrough runner also supports:

- `searchBestAiSummaryNoMutation`;
- `assertAiCandidateOrderStable`.

These steps are intentionally search diagnostics. They do not apply the chosen action unless a fixture explicitly uses the existing `makeBestAiAction` step.
