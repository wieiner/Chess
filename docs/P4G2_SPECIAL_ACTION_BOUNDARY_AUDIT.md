# P4G2 Phase 22 - Special Action Boundary Audit

Date: 2026-06-28

## Scope

This audit defines how online UI dispatch must separate ordinary board-cell moves from profile-specific special actions. It does not change rules, server authority, native ABI, or the five RuleProfile set.

The real Chess3D online profiles remain exactly:

- `classic-six-side-3d-8x8x8-v0.1`
- `single-side-3d-8x8x8-v0.1`
- `asgard-convergence-3d-8x8x8-v0.1`
- `rubik-convergence-3d-8x8x8-v0.1`
- `hodge-projection-duel-3d-8x8x8-v0.1`

## Action Kinds

The online protocol currently defines these gameplay action kinds in `OnlineActionKinds`:

| Action kind | Meaning | Generic board-click submit? | Required UI boundary |
| --- | --- | --- | --- |
| `NormalMove` | A piece move/capture from one board cell to another. | Yes, when returned by server legal preview and the user owns the turn. | Generic click-to-move path. |
| `ReserveRestore` | Restore a reserve piece to a board target. | No. | Explicit reserve/restore control because it needs inventory context. |
| `RubikLayerTurn` | Rotate a Rubik profile layer. | No. | Dedicated Rubik layer action panel with axis/layer/direction. |
| `HodgeProjectedMove` | Submit all-or-nothing Hodge primary plus mirror projection move. | No. | Dedicated Hodge projection panel that shows primary and mirrors. |
| `AiActionRequest` | Ask server-side AI/search for a candidate. | No. | Explicit AI/search control; not part of P4G2 generic click-to-move. |
| `Resign` / `OfferDraw` | Table-control actions. | No. | Future table controls. |

## Current Generic Dispatch Risk

`src/ChessOnlineApp/MainWindow.xaml.cs` has a generic legal-preview submit path:

- `P4GSubmitSelectedPreviewAction_Click`
- `SubmitP4GPreviewOptionAsync`
- `IsSupportedP4GPreviewAction`

At the time of this audit, `IsSupportedP4GPreviewAction` accepts `NormalMove`, `RubikLayerTurn`, `HodgeProjectedMove`, and `ReserveRestore`. That is too broad for a generic board-click button because:

- Rubik layer turns are board transformations, not piece-to-cell moves.
- Hodge projected moves require mirror preview and all-or-nothing explanation.
- Reserve restore requires reserve inventory context.
- Asgard core/fusion/reserve mechanics may need profile-specific UI before arbitrary submission is honest.

## Safe Boundary

The generic online board can submit only:

- server-previewed `NormalMove`;
- with a current snapshot hash;
- while the primary local client can act;
- after the selected preview option exactly matches the source/target/action kind.

The generic board must not:

- convert `RubikLayerTurn` to `NormalMove`;
- convert `HodgeProjectedMove` to `NormalMove`;
- convert `ReserveRestore` to `NormalMove`;
- auto-submit unknown future action kinds.

Unsupported special actions should produce a readable UI status such as:

- `Rubik layer action requires the Rubik Layer Actions panel.`
- `Hodge projection action requires the Hodge Projection Actions panel.`
- `Reserve restore requires an explicit reserve restore control.`
- `Unsupported online action kind: <kind>.`

## Profile Notes

Classic and Single-Side:

- Generic `NormalMove` is the normal online path.
- Single-Side matchmaking can be one-player and may return `MatchFound` on the first join.

Asgard:

- Generic `NormalMove` can work for ordinary board moves returned by server preview.
- Core/fusion/reserve-specific actions must remain explicit and should not be silently mapped to normal movement.

Rubik:

- Startup and snapshot are online-proven.
- Layer turns are special actions and need a dedicated UI boundary.

Hodge:

- Startup and snapshot are online-proven.
- Projection composite actions need primary/mirror UI and must not be hidden behind normal click-to-move.

## Verification Plan

Phase 23 should add code guardrails:

- `NormalMove` option maps to submit command.
- `RubikLayerTurn` is rejected by the generic dispatcher with a clear status.
- `HodgeProjectedMove` is rejected by the generic dispatcher with a clear status.
- `ReserveRestore` is rejected by the generic dispatcher with a clear status.
- Unknown future action kinds are rejected safely.

This keeps P4G2 playable for Classic/Asgard normal legal moves while protecting Rubik/Hodge/Reserve semantics for dedicated controls.
