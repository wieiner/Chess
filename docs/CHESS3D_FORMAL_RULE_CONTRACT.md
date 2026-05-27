# Chess3D Formal Rule Contract

This document is the P2O rule contract for the five existing Chess3D profiles. It does not add a sixth mode.

## Classic Six-Side

- Profile: `classic_six_side_3d_v0_1.json`
- Ruleset: `classic-six-side-3d-8x8x8-v0.1`
- Board: 8x8x8, exclusive occupancy.
- Players: six side ids, side turn.
- Actions: normal 3D moves and classic captures.
- Disabled: Asgard core, fusion, reserve, Rubik layer turns, Hodge projection.
- Victory: `classicCheckmate` / `checkmate` runtime. P3A enforces king safety, rejects self-check, detects checkmate/stalemate from legal action counts, and reports winner/no-winner consistently.
- Save/replay: supported.
- UI: must show Classic as first-class, not as Asgard without features.

## Single-Side Training

- Profile: `single_side_3d_v0_1.json`
- Ruleset: `single-side-3d-8x8x8-v0.1`
- Board: 8x8x8, central 4x4 training setup.
- Players: one side, sandbox/training turn.
- Actions: normal movement/capture preview for movement QA.
- Victory: sandbox/training. P3A applies the same king-safety legal filter when a king is present, but Single-Side remains a training profile rather than a full competitive six-side game.
- Save/replay: supported.
- UI: should make clear that this is a training board.

## Asgard / Meru Convergence

- Profile: `asgard_convergence_3d_v0_1.json`
- Ruleset: `asgard-convergence-3d-8x8x8-v0.1`
- Board: 8x8x8 with Forbidden Core x/y/z 2..5.
- Players: six sides.
- Actions: normal moves, core stack entry/move/exit, reserve restore.
- Capture: knockback home-or-reserve outside core; co-occupancy in core.
- Victory: centerAssembly anchors; checkmate is not automatically active.
- Draft: contested anchor resolution, destructive fusion/implosion.
- Save/replay: supported, including stacks/fusion/reserve.

## Rubik Convergence

- Profile: `rubik_convergence_3d_v0_1.json`
- Ruleset: `rubik-convergence-3d-8x8x8-v0.1`
- Board/action base: Asgard-like convergence.
- Additional action: legal Rubik layer turn when `layerTurnProfile.type = ritualTurn`.
- Layer turns move projected board and whole CoreCell stacks, then recompute fusion/anchors/victory.
- Reserve is unaffected by layer turns.
- Draft: animation, online sync, AI/search, layer-turn notation import UI.
- Save/replay: supported.

## Hodge Projection Duel

- Profile: `hodge_projection_duel_3d_v0_1.json`
- Ruleset: `hodge-projection-duel-3d-8x8x8-v0.1`
- Board: classic exclusive 8x8x8.
- Players: two macro-players, each with three side projections.
- Action: all-or-nothing projected composite move.
- Disabled by default: Asgard core/fusion/reserve and Rubik layer turns.
- Victory: sandbox/checkmate deferred for macro-player semantics. Hodge does not inherit Classic checkmate automatically.
- Save/replay: supported.

## Cross-Profile Contract

- Failed actions do not mutate board, stacks, reserve, action history, replay cursor, or state hash.
- Preview is non-mutating.
- Save/load/replay must preserve profile-specific state.
- Scenario and regression JSON files are executable test artifacts, not RuleProfiles.
