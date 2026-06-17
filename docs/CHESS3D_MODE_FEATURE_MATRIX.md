# Chess3D Mode Feature Matrix

Status: P4C Phase 07.

| Capability | Classic Six-Side | Single-Side | Asgard / Meru | Rubik Convergence | Hodge Projection Duel |
| --- | --- | --- | --- | --- | --- |
| Real RuleProfile | yes | yes | yes | yes | yes |
| Intended players | 6 sides | 1 training side | 6 sides | 6 sides | 2 macro-players |
| Board | 8x8x8 | 8x8x8 | 8x8x8 with Forbidden Core | 8x8x8 with Forbidden Core | 8x8x8 |
| Movement | classic 3D piece movement | training movement | classic-style movement plus core entry | Asgard-style plus layer actions | projected composite movement |
| Capture | classic remove | classic/training | knockback/reserve outside core; co-occupancy in core | Asgard capture model | classic remove per projected action |
| Victory/outcome | check/checkmate/stalemate runtime | sandbox/training | centerAssembly anchors | centerAssembly anchors | projection duel outcome draft |
| CoreCell stacks | no | no | yes | yes | no |
| Fusion descriptors | no | no | yes | yes | no |
| Reserve/knockback | no | no | yes | yes | no |
| Reserve restore | no | no | yes | yes | no |
| Rubik layer turns | no | no | no | yes | no |
| Hodge projection | no | no | no | no | yes |
| Action history | yes | yes | yes | yes | yes |
| Save/load/replay | yes | yes | yes | yes | yes |
| Legal preview | yes | yes | yes | yes | yes |
| AI/search | yes | yes | profile-aware | profile-aware | profile-aware composite candidates |
| Online authority | yes | accepted profile | yes | yes | yes |
| Matchmaking | two-player/six-side seat subset MVP | one-player policy | two-player MVP | two-player MVP | two macro-player MVP |
| UI support | common panel, check status | training panel | Asgard stack/fusion/reserve panel | Rubik layer panel | Hodge projection panel |
| Product status | playable | training | experimental-playable | experimental-playable | experimental-playable |

## Cross-Cutting Layers

These layers are not modes:

- save/load/replay;
- action history;
- legal preview;
- AI/search;
- online SignalR transport;
- identity/session/persistence;
- matchmaking;
- deployment scripts;
- scenario/playthrough/regression JSON;
- generated asset pipeline.

## Isolation Rules

- Classic must not show Asgard/Rubik/Hodge-only actions.
- Single-Side must not become a public competitive mode by accident.
- Asgard must not inherit Classic checkmate as its victory condition.
- Rubik layer turns must remain disabled outside Rubik profiles.
- Hodge projected moves must remain disabled outside Hodge profiles.
- Online matchmaking must select an existing RuleProfile by exact `rulesetId`.

## Current Online Matrix

| Mode | Online action authority | Matchmaking smoke | Reconnect/snapshot |
| --- | --- | --- | --- |
| Classic | normal move/capture | yes | snapshot/action log supported |
| Single-Side | profile accepted; one-player policy | catalog policy | snapshot/action log supported |
| Asgard | normal/core/reserve actions | yes | snapshot/action log supported |
| Rubik | layer turn action supported | profile accepted | snapshot/action log supported |
| Hodge | projected composite action supported | profile accepted | snapshot/action log supported |

## Current Asset / UI Matrix

All five profiles use the shared visual asset pipeline and material fallback. The asset pipeline does not create or imply a new gameplay mode.

| Mode | Visual-specific note |
| --- | --- |
| Classic | normal board, legal/capture targets, check highlight. |
| Single-Side | training setup and legal target visibility. |
| Asgard | CoreCube, stack badges, fusion/contested/anchor overlays. |
| Rubik | Asgard overlays plus layer-turn controls/overlay. |
| Hodge | primary and mirror projection hints. |
