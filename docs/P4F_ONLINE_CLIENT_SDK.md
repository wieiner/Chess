# P4F Online Client SDK

Date: 2026-06-27

`src/ChessOnlineClient` is a small shared `net8.0` client layer for P4F. It is deliberately not a new game mode, not a new server protocol, and not a replacement for `ChessOnlineProtocol`.

## Purpose

The SDK gives UI and operator tooling one place for:

- base URL normalization;
- health/ready/diagnostics URLs;
- HTTP auth request/response DTOs;
- temporary test-user registration helpers;
- in-memory auth session state;
- SignalR hub connection creation;
- matchmaking/snapshot/action-log helper calls;
- token/password redaction for UI logs.

This keeps the P4F WPF work from copying the already-proven remote smoke logic into a long code-behind block.

## Project

```text
src/ChessOnlineClient/ChessOnlineClient.csproj
```

Target:

```text
net8.0
```

References:

- `Microsoft.AspNetCore.SignalR.Client`;
- `ChessOnlineProtocol`.

Consumers wired in this phase:

- `src/ChessOnlineApp`;
- `tools/HetznerSignalRSmoke`;
- `tests/ChessOnlineContractTests`.

`tools/HetznerSignalRSmoke` still keeps its proven end-to-end implementation for now. Later phases can move more of the smoke sequence onto the SDK once the WPF MVP has settled.

## Classes

`ChessOnlineServerEndpoint`

- accepts either a base URL or a full `/chess3d/relay` hub URL;
- normalizes to a base URL;
- computes `/healthz/live`, `/healthz/ready`, `/chess3d/diagnostics`, `/api/auth/*`, and `/chess3d/relay`;
- flags non-loopback HTTP as diagnostic-only.

`ChessOnlineHealthClient`

- calls health live;
- parses ready status/profile count;
- parses diagnostics including auth/native authority values.

`ChessOnlineAuthClient`

- posts register/login/refresh/logout requests;
- can create a temporary runtime user with a generated suffix;
- does not print passwords or tokens.

`ChessOnlineClientSession`

- holds one endpoint/client identity and optional token response in memory;
- exposes a redacted status string for UI use;
- does not persist tokens.

`ChessOnlineRelayClient`

- builds a SignalR `HubConnection` using the endpoint hub URL and in-memory access token provider;
- registers the current relay events;
- wraps `Hello`, `JoinMatchmaking`, `Ready`, `StartGame`, `RequestSnapshot`, `RequestActionLog`, and `SubmitAction`;
- tracks last snapshot/action-log/matchmaking status for UI display.

`ChessOnlineClientEventLog`

- stores redacted event messages.

`ChessOnlineSecretRedactor`

- redacts access tokens, refresh tokens, passwords, authorization fields, and bearer strings from client logs.

## Security Boundary

P4F still targets the diagnostic Hetzner HTTP deployment on port 80. The SDK treats non-loopback HTTP as diagnostic-only. UI phases must keep the warning visible:

```text
HTTP 80 is diagnostic/dev only. Do not use real credentials.
```

The SDK does not save tokens, passwords, runtime stores, keyrings, certificates, or server secrets.

## Tests

`tests/ChessOnlineContractTests` now covers:

- base URL and hub URL normalization;
- health/diagnostics URL construction;
- diagnostic HTTP flagging;
- token/password redaction;
- in-memory session status;
- event-log redaction;
- SignalR `HubConnection` construction without contacting a network.

Remote Hetzner smoke remains an operator/manual step and must not be required by GitHub Actions.

## Next UI Use

The P4F ChessOnlineApp panels should use this SDK in order:

1. `ChessOnlineServerEndpoint` from Base URL text box.
2. `ChessOnlineHealthClient` for live/ready/diagnostics.
3. `ChessOnlineAuthClient` for temp user/login.
4. `ChessOnlineClientSession` for in-memory token state.
5. `ChessOnlineRelayClient` for SignalR connect, matchmaking, snapshot, safe action, and action log.

