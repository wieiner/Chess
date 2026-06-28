# P4G2 Phase 25 - Hodge Projection UI Boundary

Date: 2026-06-28

## Change

`ChessOnlineApp` now shows a dedicated `Hodge Projection Actions` group when the selected or active ruleset is:

`hodge-projection-duel-3d-8x8x8-v0.1`

The group includes:

- source cell display;
- target/preview display;
- disabled `Submit Projection` button;
- status text explaining that projected-move dispatch requires primary and mirror preview.

## Boundary

This phase intentionally does not submit Hodge projected moves from the UI. A projection composite is not a single generic source-to-target move; it is a primary move plus mirrored moves that must be shown as an all-or-nothing action.

The P4G2 guarantee is:

- `HodgeProjectedMove` is not sent through the generic `NormalMove` path.
- Hodge has a visible dedicated UI boundary.
- Classic/Single/Asgard/Rubik do not show the Hodge projection panel.

## Implementation

- `OnlinePreviewActionDispatchPolicy.ShouldShowHodgeProjectionPanel(...)` owns the profile check.
- `ChessOnlineApp` updates the panel after profile changes and authoritative snapshots.

## Verification

- `dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64`
- `tests/run-tests.ps1 -Only ChessOnlineContractTests ...`

Contract tests verify that the Hodge panel policy is true for Hodge and false for Classic, and that `HodgeProjectedMove` is rejected by the generic dispatcher.
