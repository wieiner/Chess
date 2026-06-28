# P4J Reconnect UI Guards

Date: 2026-06-28

P4J Phase 04 wires the shared SignalR reconnect state into `ChessOnlineApp` without changing Chess3D rules, hub DTO layout, or server deployment.

## What Changed

- `ChessOnlineApp` now shows a visible `Reconnect:` status line next to realtime sync status.
- The status is driven by `ChessOnlineRelayClient.ReconnectStateChanged`.
- UI updates are marshalled through the WPF Dispatcher.
- Ready, start, snapshot, action-log, legal-preview, and generic submit paths now check the primary relay before calling the hub.
- The compact status line includes both raw SignalR state and the higher-level reconnect state.
- Reset/session clear resets reconnect status to `disconnected`.

## Guard Behavior

The primary relay is considered unusable when:

- no primary relay exists;
- `OnlineReconnectSummary.ShouldDisableSubmit` is true;
- the underlying `HubConnectionState` is not `Connected`.

When guarded actions are attempted in those states, the UI reports a readable reason such as `connection state is Reconnecting` or `SignalR state is Disconnected`.

## Reconnect Resync

After a `Reconnected` state, the shared client marks that snapshot and action-log refresh are needed. The UI owns room/table context, so it performs the authoritative refresh through the existing `RefreshP4FAfterRealtimeResyncAsync` path and then clears the reconnect resync request.

## Boundaries

- No server changes.
- No remote Hetzner deployment changes.
- No changes to the five Chess3D RuleProfiles.
- No tokens, passwords, or auth headers are logged.
- HTTP 80 remains diagnostic/dev only.

## Verification

Phase 04 verification:

```powershell
dotnet build src\ChessOnlineClient\ChessOnlineClient.csproj -c Release -p:Platform=x64
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 -GlobalTimeoutSeconds 300
git diff --check
```
