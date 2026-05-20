# Chess3D Rule Profiles

P2B adds profile JSON files under `assets/rules/profiles`. P2D makes those profiles available to Chess3D runtime code and portable builds.

## Profiles

| File | Ruleset id | Goal | Capture | Layer turns |
| --- | --- | --- | --- | --- |
| `classic_six_side_3d_v0_1.json` | `classic-six-side-3d-8x8x8-v0.1` | `classicCheckmate` | `classicCapture` | `disabled` |
| `single_side_3d_v0_1.json` | `single-side-3d-8x8x8-v0.1` | `sandbox` | `classicCapture` | `disabled` |
| `asgard_convergence_3d_v0_1.json` | `asgard-convergence-3d-8x8x8-v0.1` | `centerAssembly` | `knockbackCapture` | `disabled` |
| `rubik_convergence_3d_v0_1.json` | `rubik-convergence-3d-8x8x8-v0.1` | `centerAssembly` | `knockbackCapture` | `ritualTurn` |

## Capture / Reserve Fields

P2G extends profiles with:

- `knockbackProfile`: `none` or `homeOrReserve`.
- `reserveProfile`: `none`, `disabled`, or `sidePieceTypeCounts`.

Classic and single-side profiles keep `classicCapture`, `knockbackProfile: none`, and `reserveProfile: none`.

Asgard and Rubik convergence profiles use:

```text
captureProfile.type = knockbackCapture
knockbackProfile.type = homeOrReserve
reserveProfile.type = sidePieceTypeCounts
```

At runtime, ordinary outer-field captures return the captured piece to a matching free home slot when possible. If no matching home slot is free, the captured piece increments reserve count for its side and type. Core captures remain non-destructive stack co-occupancy.

## Core Physics Fields

P2C extends profiles with:

- `occupancyProfile`: `exclusive`, `coreStack`, or future `quantumCore`.
- `fusionProfile`: `none`, `anchorOnly`, `pairFusion`, `stackFusion`, `colorPermutation`, or `volumeSurface216`.
- `corePhysicsProfile`: optional binding for core zone physics, including Asgard/Meru stack/fusion rules.

Classic and single-side profiles remain `exclusive` with fusion `none`. Asgard and Rubik convergence profiles use `coreStack` and `stackFusion`. P2E implements the stack overlay; P2F implements the runtimePartial fusion descriptor layer.

`chess3d_rule_profile.schema.json` documents the minimal schema. Contract tests also perform manual validation so the baseline does not require an external JSON-schema tool.

## Runtime Status

The native engine now has two loader paths:

- `Chess3D_LoadRulesJson`: legacy draft rules loader.
- `Chess3D_LoadRuleProfileJson`: strict RuleProfile loader.

P2D stores profile summary fields, exposes append-only ABI getters, derives CoreCube target slots, and computes simple centerAssembly anchor/victory projection. P2E implements runtime CoreCell stacks for `coreStack` / `asgardCorePhysics` profiles and makes anchors stack-aware. P2F evaluates fusion descriptors and implosion progress for stack-enabled Asgard/Rubik profiles. P2G implements runtimePartial knockback/reserve capture routing for Asgard/Rubik profiles. P2H implements runtimePartial `ritualTurn` layer actions for Rubik convergence.

Profile files are copied to:

```text
Assets/Rules3D/Profiles
```

inside Chess3D development and portable output.

## Why Profiles

Profiles keep the game from becoming a pile of hardcoded branches:

- movement can stay reusable;
- goals can vary between checkmate, center assembly, hybrid, and sandbox;
- capture behavior can differ by mode;
- Rubik layer turns can be disabled, sandbox-only, or legal actions. In P2H, `rubik-convergence-3d-8x8x8-v0.1` rotates the projected board and whole CoreCell stacks, recomputes fusion/anchors, and leaves reserve counts unaffected;
- myth/narrative can change without breaking headless engine tests.
