# P4J Reconnect Manual Smoke Result

Date: 2026-06-29

## Scope

P4J Phase 05 prepares the manual reconnect/resync smoke for `ChessOnlineApp` without touching Hetzner service configuration, nginx, UFW, 443, x-ui/Xray, Outline, Albatronix, or Unreal.

The smoke is client-side only:

- start an online match through the existing HTTP 80 diagnostic deployment;
- disconnect only the local primary SignalR relay;
- reconnect the primary relay;
- request authoritative snapshot and action log after reconnect;
- verify that guarded actions stay disabled while disconnected.

## UI Support Added

`ChessOnlineApp` now includes two operator buttons in the online match panel:

- `Disconnect Primary Relay`
- `Reconnect Primary Relay`

`Disconnect Primary Relay` disposes the primary `ChessOnlineRelayClient`, keeps room/table/session/snapshot context, clears legal preview, and marks the reconnect status as manually disconnected.

`Reconnect Primary Relay` rebuilds the primary relay from the existing authenticated temporary session, reconnects SignalR, sends `Hello`, then requests authoritative snapshot and action log through the existing resync path.

## Manual Steps

1. Build and launch:

   ```powershell
   dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
   .\src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe
   ```

2. In the UI:

   - click `Use Hetzner HTTP`;
   - click `Check Health`;
   - click `Check Diagnostics`;
   - select `asgard-convergence-3d-8x8x8-v0.1` or `classic-six-side-3d-8x8x8-v0.1`;
   - click `Create Test Match With Two Local Clients`;
   - click `Ready Both`;
   - click `Start Game`;
   - click `Request Snapshot`;
   - select an occupied source cell and confirm legal targets appear.

3. Reconnect smoke:

   - click `Disconnect Primary Relay`;
   - confirm `Reconnect:` status reports manual disconnect;
   - confirm legal preview is cleared;
   - try `Request Snapshot` or submit action and confirm the UI reports relay-not-ready instead of crashing;
   - click `Reconnect Primary Relay`;
   - confirm `Reconnect:` reports reconnected/refreshing;
   - confirm snapshot and action log refresh;
   - select a source cell again and confirm legal preview works;
   - submit one legal preview action and confirm action accepted/action log updated.

## PASS Criteria

- The app does not crash on disconnect.
- Submit/preview/snapshot/action-log operations are guarded while the relay is disconnected.
- Reconnect does not require server restart.
- Reconnect refreshes snapshot and action log from the server.
- A legal action can be submitted after reconnect.
- No access token, refresh token, password, or Authorization header is shown or saved.

## Result in This Run

Automated/terminal verification passed:

- `ChessOnlineApp` build passed.
- Targeted `ChessOnlineContractTests` passed.
- `git diff --check` passed.

The interactive WPF click-through itself was not marked PASS in this run because it requires a human/operator UI session. The repo now contains the operator controls and exact manual checklist needed to perform and record that smoke honestly.

## Boundary

- HTTP 80 remains diagnostic/dev only.
- Temporary users only.
- No changes to Chess3D rules or the five RuleProfiles.
- No remote Hetzner service/network changes.
