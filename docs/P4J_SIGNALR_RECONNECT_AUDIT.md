# P4J SignalR Reconnect Audit

Date: 2026-06-28

## Scope

This audit covers the current online client reconnect path after P4I Phase 05. It does not change server code, hub contracts, rule profiles, native authority, deployment, or firewall/nginx/systemd settings.

## Sources Checked

- Microsoft Learn, ASP.NET Core SignalR .NET client.
- Microsoft Learn, SignalR security guidance.
- Microsoft Learn, WPF threading model / Dispatcher guidance.
- Current repo code under `src/ChessOnlineClient`, `src/ChessOnlineApp`, `tests`, and `docs`.

## Current Shared Relay Client

File: `src/ChessOnlineClient/ChessOnlineRelayClient.cs`

Current behavior:

- owns one `HubConnection` per `ChessOnlineClientSession`;
- builds the connection with `HubConnectionBuilder`;
- configures `.WithUrl(endpoint.HubUri, options => options.AccessTokenProvider = ...)`;
- already calls `.WithAutomaticReconnect()`;
- registers all `Receive*` callbacks before `StartAsync`;
- exposes `MessageReceived` for protocol messages;
- records `LastSnapshot`, `LastActionLog`, `LastLegalPreview`, and `LastMatchmakingStatus`;
- handles only `Closed` as a connection lifecycle event.

Gap:

- no explicit `Reconnecting` or `Reconnected` event is surfaced;
- no typed reconnect state object exists;
- no reconnect attempt count / last safe error / resync-after-reconnect flag exists;
- UI can see `_p4fPrimaryRelay.State`, but not enough context to guide the player.

## Current ChessOnlineApp Usage

File: `src/ChessOnlineApp/MainWindow.xaml.cs`

P4F/P4G online play currently:

- creates one or two `ChessOnlineRelayClient` instances;
- subscribes to `MessageReceived`;
- calls `ConnectAsync`, `HelloAsync`, matchmaking, ready/start, snapshot, preview, and submit methods;
- updates realtime state through `OnlineRealtimeSyncState`;
- marshals SignalR protocol callbacks through `Dispatcher.Invoke` in `P4FRelayMessageReceived`.

P3F legacy/local hub panel:

- creates a raw `HubConnection` directly in code-behind;
- registers `Receive*` callbacks;
- handles `Closed`;
- does not configure `WithAutomaticReconnect`.

P4J should focus on the shared P4F/P4G relay path first, because that is the playable Hetzner path.

## Callback Registration

Good:

- `ChessOnlineRelayClient` registers hub callbacks in the constructor before `ConnectAsync`.
- P3F registers callbacks before `StartAsync`.

Needs hardening:

- lifecycle callbacks should be as cheap as possible;
- reconnect state should be emitted as small typed state changes;
- UI should request snapshot/action log after reconnect from the UI layer, where room/table context exists.

## Existing Resync Model

`OnlineRealtimeSyncState` already tracks:

- duplicate events;
- server sequence gaps;
- resync required;
- last observed server sequence;
- connection state text.

This is useful but not enough for reconnect UX because it does not distinguish:

- connecting;
- connected;
- reconnecting;
- reconnected;
- closed;
- should disable submit;
- should request snapshot/action log after reconnect.

## Security Boundary

Reconnect/status UI must not display:

- access tokens;
- refresh tokens;
- Authorization headers;
- raw private connection secrets;
- generated temporary passwords.

HTTP 80 remains diagnostic/dev-only and should use temporary users.

## Recommended First Changes

1. Add a testable reconnect state model in `src/ChessOnlineClient`.
2. Surface `Reconnecting`, `Reconnected`, and `Closed` transitions from `ChessOnlineRelayClient`.
3. Update `ChessOnlineApp` to:
   - show reconnect state;
   - disable submits during reconnect;
   - request snapshot/action log after reconnect when room/table is known;
   - keep manual refresh buttons.
4. Keep P3F legacy direct hub path unchanged until the P4F/P4G path is stable.

## Verification Plan

Phase 01 is docs-only:

```powershell
rg -n "HubConnection|WithAutomaticReconnect|Reconnecting|Reconnected|Closed|StartAsync|StopAsync|connection.On|Dispatcher|MessageReceived|ConnectionState|Reconnect|Closed" src\ChessOnlineClient src\ChessOnlineApp tests docs
git diff --check
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
```
