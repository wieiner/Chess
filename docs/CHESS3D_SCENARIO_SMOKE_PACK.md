# Chess3D Scenario Smoke Pack

P2K adds lightweight scenario descriptors in:

```text
assets/rules/scenarios/chess3d
```

Runtime builds copy them to:

```text
Assets/Rules3D/Scenarios
```

Descriptors:

- `classic_six_side_smoke_v0_1.json`
- `asgard_core_fusion_smoke_v0_1.json`
- `rubik_layer_turn_smoke_v0_1.json`
- `hodge_projection_smoke_v0_1.json`

Each descriptor contains `scenarioId`, `displayName`, `rulesetId`, `purpose`, `expectedCapabilities`, `sampleActions`, and `knownLimitations`.

They are not savegames and not replay scripts. They are smoke-pack metadata for manual QA, UI listing, packaging verification, and contract-test parsing.

