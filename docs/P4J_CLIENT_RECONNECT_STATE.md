# P4J Client Reconnect State

Date: 2026-06-28

## Scope

This phase adds a testable reconnect state model to `ChessOnlineClient`. It does not wire SignalR lifecycle callbacks into the UI yet and does not change server behavior, hub contracts, deployment, rule profiles, or native ABI.

## Added Types

File: `src/ChessOnlineClient/OnlineReconnectState.cs`

- `OnlineConnectionState`
  - `Disconnected`
  - `Connecting`
  - `Connected`
  - `Reconnecting`
  - `Reconnected`
  - `Closed`
- `OnlineReconnectEvent`
- `OnlineReconnectState`
- `OnlineReconnectSummary`
- `OnlineConnectionHealthSnapshot`

## State Semantics

`OnlineReconnectState` tracks:

- current connection state;
- last connection id;
- shortened connection id for display/reporting;
- reconnect attempt count;
- last safe/redacted error;
- last transition UTC;
- whether submit should be disabled;
- whether the UI should request snapshot after reconnect;
- whether the UI should request action log after reconnect;
- whether the session is currently playable.

## Transition Rules

- `Disconnected` / `Connecting` / `Reconnecting` / `Closed` disable submit.
- `Connected` and `Reconnected` are playable.
- `Reconnected` requests snapshot and action-log refresh.
- `Closed` marks the session not playable.
- `ClearResyncRequest()` clears the post-reconnect refresh flags after UI has handled them.

## Security

Reconnect state summaries do not print tokens or generated passwords:

- exception messages are passed through `ChessOnlineSecretRedactor`;
- display summaries use shortened connection ids;
- no access/refresh tokens are stored in the reconnect model.

## Tests

`tests/ChessOnlineContractTests` covers:

- disconnected initial state;
- connecting disables submit;
- connected is playable;
- reconnecting disables submit and increments attempts;
- reconnected requests snapshot/action-log refresh;
- closed is not playable;
- token-like strings in errors are redacted.

## Verification

```powershell
dotnet build src\ChessOnlineClient\ChessOnlineClient.csproj -c Release -p:Platform=x64
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 -GlobalTimeoutSeconds 300
```
