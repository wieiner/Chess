# Chess3D P2D Runtime Profile Selection

P2D creates the first runtime bridge from machine-readable rule profiles to `Chess3DEngine.dll`.

## Loader model

Two loaders now coexist:

- `Chess3D_LoadRulesJson`: legacy/draft JSON loader for older `Assets/Rules3D/*.json` files.
- `Chess3D_LoadRuleProfileJson`: strict RuleProfile loader for `assets/rules/profiles/*.json`.

The strict loader validates required profile fields and stores a current profile summary. If a profile load fails, the previous valid profile state is kept and `Chess3D_GetLastProfileError` exposes the reason.

## Stored runtime fields

The engine stores:

- `currentRulesetId`;
- `currentRulesetVersion`;
- `currentRulesetDisplayName`;
- `goalProfileType`;
- `captureProfileType`;
- `occupancyProfileType`;
- `fusionProfileType`;
- `corePhysicsProfileType`;
- `layerTurnProfileType`;
- `victoryProfileType`;
- `coreCube`;
- `anchorMode`;
- `requiredAnchorCount`;
- `gameOver`;
- `winnerSide`.

## ABI

P2D adds append-only getters for profile summary, core cube, anchor progress, target-slot checks, game-over state, and profile-load errors.

The existing board ABI remains unchanged and remains single-occupancy.

## Chess3DApp visibility

`Chess3DApp` can load profile JSON files when the selected rules file contains a RuleProfile shape. The status area now shows the active ruleset id, goal/capture/occupancy/fusion/layer profile types, anchor progress for the active side, and winner information when the simple centerAssembly projection reaches victory.

This is intentionally not a full profile-management UI yet.
