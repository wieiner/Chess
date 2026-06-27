# P4F Online Client UI Audit

Date: 2026-06-27

Scope: audit only. No server settings, Chess3D rules, profile JSON, SignalR hub contract, or deployment paths were changed in this phase.

## Current App Boundary

The practical P4F MVP should live in `src/ChessOnlineApp`, not `Chess3DApp`.

`ChessOnlineApp` already has:

- a WPF app target (`net8.0-windows`) with `Microsoft.AspNetCore.SignalR.Client`;
- an existing P3E local authority harness tab;
- an existing P3F hosted SignalR transport panel;
- a hub URL text box (`P3FServerUrlBox`);
- buttons for `Connect`, `Disconnect`, `Hello`, room/table/seat actions, `Ready + Start`, `Submit Move`, `Snapshot`, `Action Log`, `Diagnostics`;
- a matchmaking profile selector with exactly the five real Chess3D RuleProfiles;
- event handlers for the current server-to-client SignalR events;
- sanitized logging for local session token receipt.

`Chess3DApp` is still the right place for rich local 3D visualization and native engine interaction. It currently links `Chess3DInternetRelayClient`, but P4F does not need to embed the full realtime Chess3D board yet. The safer MVP is a readable online control client in `ChessOnlineApp`, with a snapshot/action-log viewer and a safe action button.

## Current ChessOnlineApp Capabilities

The current P3F panel can manually call the relay hub after the operator enters a full hub URL such as:

```text
http://127.0.0.1:5077/chess3d/relay
```

It can:

- build and start a `HubConnection`;
- invoke hub methods using `OnlineProtocolMessage`;
- receive and log hub events;
- join matchmaking for one of five profile ids;
- request snapshots and action logs;
- submit a manually entered normal move.

It does not yet provide the P4F "touch it by hand" flow:

- no base URL level server panel;
- no `Use Hetzner HTTP` preset;
- no `/healthz/live`, `/healthz/ready`, or `/chess3d/diagnostics` HTTP checks from the UI;
- no auth registration/login panel;
- no temporary two-player setup;
- no bearer token handoff to SignalR;
- no one-button two-client matchmaking smoke;
- no compact room/table/seat/current-player dashboard;
- no structured snapshot viewer beyond log text;
- no session report export;
- no explicit HTTP-80 diagnostic-only warning next to auth controls.

## Current Chess3DApp Capabilities

`Chess3DApp` remains a local Chess3D playability surface:

- profile selection and mode-specific control center;
- local native rule authority;
- visual board, legal preview, action log, save/replay, and visual diagnostics.

For P4F it should not become the primary online MVP target. Pulling online auth, two SignalR clients, matchmaking, and remote snapshot/action log into the already-dense Chess3D visual window would increase risk. A later P4G can bridge the online snapshot stream into the full Chess3D visual board.

## Reusable Online Client Code

Reusable online client code is currently partial:

- `src/ChessOnlineProtocol` contains the shared DTOs, message types, action kinds, snapshots, action logs, diagnostics, room/table/matchmaking commands, and authority registry models.
- `tools/HetznerSignalRSmoke` contains a proven command-line remote smoke client. It registers two temporary users, logs them in, connects two authenticated SignalR clients, joins Asgard matchmaking, starts the game, submits a safe action, requests snapshot/action log, and avoids printing tokens.
- `scripts/deploy/Test-HetznerSignalRMatchmaking.ps1` is a thin operator wrapper around the smoke tool and the C# watchdog.
- `tests/ChessOnlineSignalRContractTests` contains contract-level hub patterns and safe action examples.

The gap is a reusable client SDK layer that UI code and smoke tooling can share. Without that layer, P4F would duplicate the smoke logic inside WPF code-behind. The recommended Phase 02 direction is a small shared client layer for endpoint normalization, health/diagnostics, auth, SignalR connection, matchmaking, snapshot/action log, and token redaction.

## Auth Endpoints

The server maps these HTTP auth endpoints in `ChessOnlineServerHost`:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`

`register` and `login` issue token responses for authenticated hub connections. The public HTTP deployment is diagnostic/dev only; P4F UI must warn users not to enter real passwords over HTTP 80. Temporary users should be generated with random suffixes, tokens must stay in memory, and access/refresh token values must not be printed or saved.

## SignalR Hub Contract

The active hub path is:

```text
/chess3d/relay
```

Current hub methods include:

- `Hello`
- `CreateRoom`
- `JoinRoom`
- `LeaveRoom`
- `ListRooms`
- `CreateTable`
- `JoinTableSeat`
- `LeaveTableSeat`
- `Ready`
- `StartGame`
- `SubmitAction`
- `RequestSnapshot`
- `RequestActionLog`
- `Ping`
- `Diagnostics`
- `JoinMatchmaking`
- `CancelMatchmaking`
- `GetMatchmakingStatus`
- `ListMatchmakingQueues`

Current server-to-client events registered by `ChessOnlineApp` include:

- `ReceiveWelcome`
- `ReceiveRoomCreated`
- `ReceiveRoomJoined`
- `ReceiveRoomLeft`
- `ReceiveRoomList`
- `ReceiveTableCreated`
- `ReceiveTableState`
- `ReceiveSeatAssigned`
- `ReceiveGameStarted`
- `ReceiveActionAccepted`
- `ReceiveActionRejected`
- `ReceiveAuthoritativeSnapshot`
- `ReceiveActionLogChunk`
- `ReceiveResyncRequired`
- `ReceiveMatchmakingStatus`
- `ReceiveMatchmakingCancelled`
- `ReceiveMatchFound`
- `ReceiveMatchmakingError`
- `ReceivePong`
- `ReceiveError`
- `ReceiveDiagnostics`

## Manual Playable Flow Needed

The P4F UI should make this flow visible and repeatable:

1. Choose or enter a base URL.
2. Check live health.
3. Check readiness and profile count.
4. Check diagnostics and native authority status.
5. Register or login a test user.
6. Optionally create two temporary test players.
7. Connect one or two authenticated SignalR clients.
8. Select exactly one of the five real Chess3D RuleProfiles.
9. Join matchmaking.
10. Show match-found room/table/seat/current player.
11. Ready both seats and start.
12. Request snapshot.
13. Submit one safe action when available.
14. Request action log.
15. Save a sanitized client session report.

## Minimal UI That Avoids a Large Rewrite

Recommended MVP panels inside `ChessOnlineApp`:

- **Server Connection**: base URL, `Use Hetzner HTTP`, health, ready, diagnostics, SignalR connect/disconnect, status line.
- **Auth / Test Users**: register temp user, login, logout, create two test players, clear session, token-redacted auth status.
- **Online Match**: five-profile selector, join/cancel/status matchmaking, create two-client test match, ready, start, snapshot, action log.
- **Snapshot Viewer**: ruleset id, room/table, seat/player, current turn summary, state hash, action count, last notation, action-log list.
- **Session Report**: save sanitized JSON under ignored `.tmp/manual-smoke`.

This can be done as incremental WPF controls and code-behind first. A fuller MVVM split can wait until P4G/P4H if the online UI grows.

## Risks

- Public HTTP token issuance is acceptable only as a diagnostic/dev deployment. Real credentials must wait for TLS/domain policy.
- The smoke tool currently has the most robust two-client flow; UI code should reuse or mirror its logic carefully.
- Manual move entry is too brittle for a "touch it" MVP; the UI needs a safe action helper for the Asgard smoke state.
- The existing P3F panel uses hub URL directly. P4F should normalize base URL to hub URL to avoid user confusion.
- The current UI log is unstructured. P4F should retain it, but also show room/table/seat/current-player fields explicitly.

## Phase 02 Recommendation

Add a shared online client SDK layer before expanding UI:

- `ChessOnlineServerEndpoint`
- `ChessOnlineHealthClient`
- `ChessOnlineAuthClient`
- `ChessOnlineRelayClient`
- `ChessOnlineClientSession`
- `ChessOnlineClientEventLog`

The SDK should be network-capable for manual use but testable in CI without contacting Hetzner. It should centralize URL normalization and token redaction so the WPF app and smoke tools do not drift apart.

