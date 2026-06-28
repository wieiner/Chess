# P4G2 Phase 23 - Special Action Guardrails

Date: 2026-06-28

## Change

The generic online board dispatch path now accepts only server-previewed `NormalMove` actions. Profile-specific special actions are preserved as their own action kinds but blocked from the generic click-to-move submit button until dedicated controls are available.

## Guardrail Policy

`src/ChessOnlineClient/OnlinePreviewActionDispatchPolicy.cs` is the shared policy used by `ChessOnlineApp` and contract tests:

- `NormalMove`: allowed through generic board submit.
- `RubikLayerTurn`: rejected with `Rubik layer action requires the Rubik Layer Actions panel.`
- `HodgeProjectedMove`: rejected with `Hodge projection action requires the Hodge Projection Actions panel.`
- `ReserveRestore`: rejected with `Reserve restore requires an explicit reserve restore control.`
- unknown or empty action kinds: rejected safely.

## UI Behavior

`ChessOnlineApp` now shows the rejection reason in the online move status and writes the same redacted status to the event log. The app does not submit the command to SignalR when the generic dispatcher rejects the kind.

## Why

Rubik layer turns, Hodge projection composites, and reserve restores are not ordinary source-to-target piece moves. They need mode-specific controls so the player can see axis/layer/direction, mirror moves, or reserve inventory before submission.

## Verification

- `dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64`
- `tests/run-tests.ps1 -Only ChessOnlineContractTests ...`

Contract tests cover accepted `NormalMove` and rejected `RubikLayerTurn`, `HodgeProjectedMove`, `ReserveRestore`, and unknown future action kinds.
