# P4G2 Phase 24 - Rubik Layer Action UI Boundary

Date: 2026-06-28

## Change

`ChessOnlineApp` now shows a dedicated `Rubik Layer Actions` group when the selected or active ruleset is:

`rubik-convergence-3d-8x8x8-v0.1`

The group includes:

- axis selector;
- layer selector;
- quarter-turn selector;
- disabled `Submit Layer Turn` button;
- status text explaining that online layer-turn dispatch is not finalized yet.

## Boundary

This phase intentionally does not submit a Rubik layer turn from the UI. The button is disabled until explicit server-preview-backed layer-turn dispatch is wired and operator-tested.

The important P4G2 guarantee is:

- Rubik layer actions are not submitted as `NormalMove`.
- Rubik has a visible dedicated UI boundary instead of looking like the generic board is broken.
- Classic/Single/Asgard/Hodge do not show the Rubik layer panel.

## Implementation

- `OnlinePreviewActionDispatchPolicy.ShouldShowRubikLayerPanel(...)` owns the profile check.
- `ChessOnlineApp` calls `UpdateP4GSpecialActionPanels()` when the matchmaking profile changes and after authoritative snapshots.

## Verification

- `dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64`
- `tests/run-tests.ps1 -Only ChessOnlineContractTests ...`

Contract tests verify that the Rubik panel policy is true for Rubik and false for Classic.
