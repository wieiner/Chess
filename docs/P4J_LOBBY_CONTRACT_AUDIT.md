# P4J Phase 17 - Lobby Contract Audit

Date: 2026-07-01

## Goal

Define a safe online lobby contract for active ChessOnline rooms/tables without exposing secrets or changing the five Chess3D rule profiles.

This phase is docs-only. It does not add server methods yet.

## Current Runtime Shape

`OnlineRoomRegistry` owns the active in-memory online state:

- `OnlineRoom`
  - `RoomId`
  - `DisplayName`
  - `MaxTables`
  - `State`
  - `CreatedAtUtc`
  - `Players`
  - `Tables`
- `OnlineTable`
  - `RoomId`
  - `TableId`
  - `RulesetId`
  - `ProfileFileName`
  - `SeatCount`
  - `State`
  - `CreatedAtUtc`
  - `StartedAtUtc`
  - `ServerSeq`
  - `LastStateHash`
  - `Seats`
  - `ActionLog`
  - `Session`
- `OnlineSeat`
  - `SeatIndex`
  - `SideId`
  - `MacroPlayer`
  - `PlayerId`
  - `IsReady`
  - `IsConnected`
  - `LastSeenUtc`

Current `/chess3d/diagnostics` exposes aggregate `roomCount`, `tableCount`, active connections and counters, but it does not expose a user-facing active table list.

## Safe Lobby Row

Future lobby rows should expose only:

- `roomId`;
- `tableId`;
- `rulesetId`;
- `tableState`;
- `seatsOccupied`;
- `maxSeats`;
- `spectatorCount`;
- `started`;
- `lastServerSeq`;
- `createdUtc`;
- `updatedUtc`;
- optional short seat summaries:
  - `seatIndex`;
  - `sideId`;
  - `macroPlayer`;
  - `ready`;
  - `connected`;
  - short/redacted player label only.

The lobby should include all five real Chess3D rule profiles when active, and must not create or imply a sixth profile.

## Fields To Avoid

Lobby responses must not expose:

- access tokens;
- refresh tokens;
- passwords;
- Authorization headers;
- private keys;
- keyrings;
- raw persistent stores;
- full player IDs unless the player is the current user and the UI already knows it;
- full SignalR connection IDs;
- server filesystem paths;
- raw action payloads beyond sanitized last notation/hash metadata.

## Recommended DTOs

Phase 18 should add append-only DTOs:

- `OnlineLobbySnapshotRequest`
- `OnlineLobbySnapshot`
- `OnlineLobbyTableRow`
- `OnlineLobbySeatSummary`

Suggested `OnlineLobbySnapshot` fields:

- `createdUtc`;
- `serverSeq`;
- `roomCount`;
- `tableCount`;
- `activeTableCount`;
- `tables[]`;
- `warningText` optional.

Suggested `OnlineLobbyTableRow` fields:

- `roomId`;
- `tableId`;
- `rulesetId`;
- `tableState`;
- `seatsOccupied`;
- `maxSeats`;
- `spectatorCount`;
- `started`;
- `lastServerSeq`;
- `createdUtc`;
- `updatedUtc`;
- `seatSummaries[]`.

## Access Model

For P4J, prefer an authenticated SignalR hub method:

`RequestLobbySnapshot`

Reasons:

- it can reuse the existing authenticated SignalR client;
- it avoids introducing a second public HTTP surface for active table metadata;
- it aligns with the existing `RequestSnapshot`, `RequestActionLog`, `RequestDiagnostics` hub style;
- it can later be permission-gated without changing client UI flow.

A read-only HTTP endpoint can be reconsidered later if an operator dashboard needs it.

## UI Usage

The lobby should support:

- refresh active table list;
- filter by ruleset;
- select a row;
- populate spectator room/table fields;
- join/spectate selected table;
- resume selected table if it matches the current player context;
- show why an action is unavailable.

## Known Limitations

- Current deployed Hetzner HTTP server does not yet expose `JoinSpectator`, so lobby-to-spectator remote smoke requires a later deployment.
- `OnlineRoomRegistry.Rooms` currently returns shallow room clones without table details; Phase 18 should add a dedicated lobby snapshot builder rather than exposing raw registry objects.
- `spectatorCount` may require explicit server-side tracking if SignalR group membership count is not directly available.
