# Chess3D Rubik Product Refresh

Phase: P4C phase 09.

## Identity

Rubik Convergence is one of the five real Chess3D RuleProfiles:

- file: `assets/rules/profiles/rubik_convergence_3d_v0_1.json`
- rulesetId: `rubik-convergence-3d-8x8x8-v0.1`
- product role: experimental-playable Asgard-like profile with ritual layer turns

It is not the standalone `RubikApp`, and it is not a sixth mode.

## Runtime Capabilities

- CoreCell stacks and fusion descriptors follow the Asgard convergence foundation.
- Knockback/reserve and reserve restore remain profile-aware.
- `layerTurnProfile.type = ritualTurn` enables X/Y/Z layer turns.
- Layer turns move projected board cells and whole CoreCell stacks.
- Fusion, anchors, victory state, action history, and state hash are recomputed after a successful layer turn.
- Reserve counts are not moved by layer turns.

## Online / Matchmaking

Online authority accepts `RubikLayerTurn` only under the Rubik profile. Classic, Asgard, Single-Side, and Hodge must reject it.

P4C phase 09 adds explicit SignalR matchmaking smoke for the Rubik ruleset so exact-profile queues do not silently regress.

## AI / Replay / Diagnostics

- AI/search can include Rubik layer-turn candidates when the active profile enables them.
- Replay/save/load preserve layer-turn state through action history and state hash.
- Four-turn roundtrip fixtures remain the quick correctness smoke.

## Known Gaps

- No full Rubik animation in the online server.
- No public ranked matchmaking or cross-server queue.
- No standalone RubikApp network integration.
- No deep Rubik-specific AI strategy.
