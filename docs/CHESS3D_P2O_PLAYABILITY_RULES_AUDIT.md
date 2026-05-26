# Chess3D P2O Playability / Rules Audit

P2O starts from commit `18d7bca`: Chess3D has five RuleProfiles, legal-action preview, click-to-move dispatch, action history, save/load/replay, runnable playthrough JSON, visual asset packaging, and green CI.

## Real RuleProfiles

There are exactly five real Chess3D RuleProfile JSON files:

- `classic_six_side_3d_v0_1.json`
- `single_side_3d_v0_1.json`
- `asgard_convergence_3d_v0_1.json`
- `rubik_convergence_3d_v0_1.json`
- `hodge_projection_duel_3d_v0_1.json`

Scenario, smoke, playthrough, and regression JSON files are not modes.

## Runtime Implemented

- Profile loading and capability summaries.
- 8x8x8 board and 3D movement/capture contracts.
- Legal-action preview and exact click-to-action dispatch.
- CoreCell stacks, fusion descriptors, reserve/knockback, reserve restore.
- Rubik layer turns for `rubik_convergence`.
- Hodge projected composite moves.
- Action history, notation, save/load/replay, deterministic state hash.

## Spec / Draft

- Full Classic Six-Side king safety/check/mate/stalemate is draft.
- Asgard contested anchors and destructive implosion are deferred.
- Rubik animation, notation import UI, online sync, and AI/search are deferred.
- Hodge checkmate, AI/search, and online serialization are deferred.

## Runtime Boundaries

- Current side lives in `Position::sideToMove`.
- Current macro-player is derived from `projectionProfile` and side mapping.
- Preview is built by `Chess3D_BuildLegalActionPreviewForCell`.
- Actions are applied through `TryMakeMove`, `TryMakeProjectedMove`, `RotateLayer`, and `RestoreReservePiece`.
- Action history is appended only after successful turn actions.
- `gameOver/winnerSide` currently comes from centerAssembly anchors; Classic checkmate is exposed as draft status only.

## UX Issues Addressed in P2O

- Mode outcome and game phase need to be visible next to turn summary.
- Legal action availability needs a profile-aware diagnostic count.
- Failed actions need stable legality reasons.
- Reproducible bugs need regression playthrough files, not only manual notes.

## Safe P2O Changes

- Append-only ABI for phase/outcome/check summaries and action perft/divide.
- UI status text that consumes existing/new summary getters.
- Headless regression fixtures using the existing P2N runner.
- Documentation that separates implemented runtime from draft rule scope.
