# Chess3D Regression Fixtures

P2O adds runnable regression playthroughs under:

`assets/rules/scenarios/chess3d/regression`

## Fixtures

- `invalid_click_no_mutation_v0_1.json`: invalid move must not mutate state or action history.
- `rubik_four_turn_roundtrip_v0_1.json`: layer turns move stacks safely and four turns restore geometry.
- `hodge_blocked_mirror_rollback_v0_1.json`: blocked mirror rejects the whole projected move.
- `asgard_stack_fusion_anchor_v0_1.json`: core stack, friendly fusion, and anchor status remain coherent.
- `classic_turn_progression_v0_1.json`: Classic remains non-Asgard and records ordinary capture/turn history.

These fixtures are not RuleProfiles. They are bug reproduction scripts for the existing headless playthrough runner.
