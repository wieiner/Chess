# P4J Phase 18 - Server Lobby Snapshot

Date: 2026-07-01

## Summary

Phase 18 adds an append-only, safe lobby snapshot to the online protocol and server registry.

It does not change Chess3D rules, does not add a sixth profile, and does not expose credentials or SignalR connection IDs.

## Message Types

New message types:

- `RequestLobbySnapshot`
- `LobbySnapshot`

`OnlineProtocolJson` accepts both as known protocol messages.

## DTOs

New DTOs:

- `OnlineLobbySnapshotRequest`
- `OnlineLobbySnapshot`
- `OnlineLobbyTableRow`
- `OnlineLobbySeatSummary`

`OnlineLobbySnapshotRequest` supports:

- `rulesetIdFilter`
- `includeWaitingTables`
- `includeInGameTables`
- `includeFinishedTables`

`OnlineLobbySnapshot` reports:

- `createdUtc`
- `serverSeq`
- `roomCount`
- `tableCount`
- `activeTableCount`
- `warningText`
- `tables[]`

Each table row reports:

- room/table ID;
- ruleset ID;
- table state;
- occupied/max seats;
- spectator count;
- started flag;
- last server sequence;
- created/updated timestamps;
- safe seat summaries.

## Server Behavior

`OnlineRoomRegistry.RequestLobbySnapshot` projects active in-memory room/table state into safe rows.

The hub method:

`RequestLobbySnapshot`

returns a `LobbySnapshot` message to the caller.

Diagnostics now report:

- `lobbySnapshot=true`
- `OnlineDiagnostics.LobbySnapshotSupported=true`
- `supportedHubMethods` contains `RequestLobbySnapshot`

## Privacy Boundary

Lobby rows do not include:

- access tokens;
- refresh tokens;
- passwords;
- Authorization headers;
- private keys;
- keyrings;
- raw stores;
- full SignalR connection IDs;
- server filesystem paths.

Seat summaries use short player labels only. They are meant for UI orientation, not identity proof.

## Limitations

`spectatorCount` is currently `0` because SignalR group membership count is not directly available in the current hub registry. A later phase can add explicit spectator tracking.

The public Hetzner server must be deployed with Phase 18 code before remote lobby smoke can pass.
