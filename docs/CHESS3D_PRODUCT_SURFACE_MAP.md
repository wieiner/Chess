# Chess Product Surface Map

Phase: P4C Phase 01

## Executables / Products

| Surface | User-facing purpose | Implemented | Online | AI/Search | Save/Replay | Current status |
| --- | --- | --- | --- | --- | --- | --- |
| Chess2D | Orthodox 2D chess/advisor app | yes | not primary | yes, native engine | FEN/draw/search support | Windows playable |
| Chess3D Classic Six-Side | Base 8x8x8 Chess3D profile | yes | yes through server actions | yes | yes | playable draft/product mode |
| Chess3D Single-Side | Training/debug profile | yes | yes as profile/table | yes | yes | training mode |
| Asgard / Meru Convergence | Core stacks, fusion descriptors, reserve, anchors | yes | yes, P4B Asgard online smoke | yes, profile-aware | yes | experimental but online-gated |
| Rubik Convergence | Asgard-like profile plus legal layer turns | yes | yes through Rubik action | yes | yes | experimental special-action mode |
| Hodge Projection Duel | 2 macro-player projected composite mode | yes | yes through composite action | yes | yes | experimental special-action mode |
| RubikApp | Standalone Rubik 8x8x8 app | yes | no | solver/history local | local app state | Windows playable |
| ChessOnlineApp | Local/hosted online control app | yes | n/a | calls existing endpoints | action log/snapshot UI | Windows operator/control app |
| ChessOnlineServer | Hosted SignalR authority server | yes on Windows | n/a | server validates submitted actions | snapshots/action logs | Windows-hosted prototype |

## Product Capabilities

- RuleProfile count remains exactly five for Chess3D.
- Scenario, regression, online, identity, persistence, matchmaking, and deployment JSON files are not modes.
- P4B matchmaking is exact-profile and single-server.
- P4C must not let infrastructure work erase Asgard/Rubik/Hodge from docs or tests.

## Product-facing Gaps

- Linux server runtime is not proven.
- Public deployment, ranked matchmaking, Redis/Azure SignalR, anti-cheat, and cloud scale are not implemented.
- Asgard still needs deeper gameplay design for final fusion/implosion beyond descriptors.
- Generated 3D piece assets need a stricter import/manifest policy before large asset commits.
