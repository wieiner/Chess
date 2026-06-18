# Chess3D Screenshot TODO

Status: P4C Phase 11.

The product deck intentionally avoids embedding stale screenshots. Capture fresh images from the current build when preparing public material.

## Capture Rules

- Use a clean working tree and a Release x64 build.
- Do not show secrets, tokens, local server IPs, private key paths, or runtime data stores.
- Use the five real Chess3D profiles only.
- Label scenario/playthrough JSON as support data, not modes.
- Prefer neutral board positions that demonstrate one feature clearly.

## Suggested Screenshots

| Shot | App/profile | Purpose |
| --- | --- | --- |
| Classic overview | Chess3DApp / Classic | normal 8x8x8 board, selected piece, legal targets |
| Classic check | Chess3DApp / Classic | king-safety/check UI |
| Single-Side training | Chess3DApp / Single-Side | central training setup |
| Asgard core | Chess3DApp / Asgard | CoreCube, stack count, fusion/anchor status |
| Rubik layer turn | Chess3DApp / Rubik | layer-turn controls and last action |
| Hodge projection | Chess3DApp / Hodge | primary side, mirror preview, HPD notation |
| Action log | Chess3DApp | notation and action count |
| Online table | ChessOnlineApp | hosted/local authority surface without secrets |
| Production package | file explorer or terminal | `ProductionOutput` layout after verify |
| Model assets | ChessApp or Chess3DApp | readable pieces and non-black fallback material |

## Deferred Visuals

- Animated Rubik turn sequence.
- Full Asgard fusion/implosion visual drama.
- Hodge mirror arrows as polished presentation graphics.
- Online spectator/reconnect UX.

