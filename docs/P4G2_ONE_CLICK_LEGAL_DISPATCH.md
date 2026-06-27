# P4G2 One-Click Legal Dispatch

Date: 2026-06-27

Scope: P4G2 Phase 07. This phase lets the online board submit the exact server-previewed action when the user clicks a highlighted legal target.

## Flow

The current player flow is now:

1. Request or receive an authoritative snapshot.
2. Click an occupied source cell.
3. The app requests legal preview from the server.
4. The board highlights legal targets.
5. Click a highlighted target.
6. If exactly one preview option targets that cell, the app submits that exact option as `OnlineActionCommand`.
7. If several preview options target the same cell, the app selects the first option and asks the user to choose from the preview action selector.
8. After acceptance, the app refreshes the authoritative snapshot.

## Fallback

The older manual flow remains available:

- `Use Selected as From`
- `Use Selected as To`
- `Submit Normal Move`

This is retained for diagnostics while the preview-driven flow matures.

## Pending Guard

The UI keeps a `_p4gSubmitPending` flag so rapid clicks do not submit duplicate actions while a server call is in flight.

## Supported Action Kinds

The dispatch helper can submit:

- `NormalMove`
- `RubikLayerTurn`
- `HodgeProjectedMove`
- `ReserveRestore`

The current UI experience is still best for normal moves. Rubik/Hodge/reserve special actions remain clearer in dedicated panels until later profile-specific UX work.

## Server Authority

The client still does not mutate the board locally. Every action goes through:

- `ChessOnlineRelayClient.SubmitActionAsync`
- server `SubmitAction`
- native authority validation
- accepted/rejected/resync result
- authoritative snapshot refresh.

## Verification

Verification for this phase:

- `dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64`
- `ChessOnlineContractTests` through the decomposed runner.

Manual remote smoke is deferred to the later P4G2 manual verification phases.
