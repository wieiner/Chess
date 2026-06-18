# Chess3D Feature Inventory

Status: P4C Phase 11.

## Product Surfaces

| Surface | Status | Notes |
| --- | --- | --- |
| ChessApp | Windows playable | 2D chess/advisor plus shared 3D model asset support |
| Chess3DApp | Windows playable | five-profile control center |
| RubikApp | Windows playable | standalone Rubik 8x8x8 app |
| ChessOnlineApp | Windows operator/control UI | local/hosted online integration surface |
| ChessOnlineServer | Windows-hosted prototype | SignalR authority server over existing rules engine |

## Five Chess3D Profiles

| Ruleset | Status | Special capability |
| --- | --- | --- |
| `classic-six-side-3d-8x8x8-v0.1` | playable | king safety, checkmate, stalemate |
| `single-side-3d-8x8x8-v0.1` | training | one-side setup and movement practice |
| `asgard-convergence-3d-8x8x8-v0.1` | experimental-playable | core stacks, fusion descriptors, reserve, anchors |
| `rubik-convergence-3d-8x8x8-v0.1` | experimental-playable | Asgard-like state plus layer turns |
| `hodge-projection-duel-3d-8x8x8-v0.1` | experimental-playable | projected all-or-nothing composite moves |

## Cross-Cutting Runtime Capabilities

- RuleProfile JSON loading.
- Legal action preview.
- Invalid-action reason reporting.
- Action history and deterministic notation.
- Save/load/replay/state hash.
- Headless playthrough/regression fixtures.
- Profile-aware AI/search candidates.
- Online authority command validation.
- SignalR transport and reconnect/snapshot smoke.

## Visual And Asset Capabilities

- Canonical OBJ/MTL piece catalog.
- Readable fallback materials for white and black pieces.
- Shared model assets in Chess2D and Chess3D outputs.
- Generated-piece manifest policy without tracked generated meshes.
- Manual visual smoke checklist.

## Deployment Capabilities

- Release x64 build.
- Portable `ProductionOutput`.
- GitHub Actions Windows Build.
- Short-retention `ProductionOutput` workflow artifact.
- Windows server start/stop/test scripts.
- Windows server production runbook.

## Explicit Non-Claims

- No sixth Chess3D RuleProfile.
- No Linux-native server authority yet.
- No Redis/Azure SignalR/backplane.
- No public ranked matchmaking.
- No final destructive Asgard implosion.
- No full anti-cheat or online tournament authority.
- No production secret material in the repository.

