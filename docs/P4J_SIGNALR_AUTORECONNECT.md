# P4J SignalR Automatic Reconnect

Date: 2026-06-28

## Scope

This phase wires the shared `ChessOnlineRelayClient` to SignalR reconnect lifecycle events. It does not change hub methods, server deployment, rule profiles, native authority, or WPF UI behavior yet.

## Changes

`ChessOnlineRelayClient` now:

- keeps an `OnlineReconnectState`;
- publishes `ReconnectStateChanged` summaries;
- marks `Connecting` before `StartAsync`;
- marks `Connected` after `StartAsync`;
- marks `Disconnected` after explicit `StopAsync`;
- handles SignalR `Reconnecting`;
- handles SignalR `Reconnected`;
- handles SignalR `Closed`;
- writes redacted reconnect summaries to `ChessOnlineClientEventLog`.

## Resync Boundary

On `Reconnected`, the reconnect state marks:

- `ShouldRequestSnapshotAfterReconnect=true`;
- `ShouldRequestActionLogAfterReconnect=true`.

The shared relay client does not automatically request snapshot/action log because that requires UI/session context:

- client id;
- room id;
- table id;
- selected player mode.

`ChessOnlineApp` will consume the reconnect state in a later phase.

## Threading

The shared client emits a small summary event only. WPF controls must still be updated through `Dispatcher` in the UI layer.

## Security

Reconnect event summaries:

- do not print access tokens;
- do not print refresh tokens;
- use redacted exception messages;
- use shortened connection ids for display/reporting.

## Verification

```powershell
dotnet build src\ChessOnlineClient\ChessOnlineClient.csproj -c Release -p:Platform=x64
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 -GlobalTimeoutSeconds 300
```
