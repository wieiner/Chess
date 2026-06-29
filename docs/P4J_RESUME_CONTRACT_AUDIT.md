# P4J Resume Contract Audit

Date: 2026-06-29

Scope: P4J Phase 06. This audit defines the boundary for resuming an online Chess3D match. It does not change server code, hub methods, Chess3D rules, native ABI, Hetzner deployment, nginx, UFW, TLS, or the five RuleProfiles.

## Current Reconnect vs Resume

Reconnect already covers transport recovery in the same app session:

- `ChessOnlineRelayClient` uses SignalR automatic reconnect.
- `ChessOnlineApp` displays reconnect status.
- After reconnect, the UI can request authoritative snapshot and action log when room/table context is still known.

Resume is a different feature:

- the client may have restarted;
- the SignalR connection id is gone;
- the UI may need to rediscover room/table/seat;
- the server must validate that the authenticated player belongs to that table;
- the server must return a current snapshot and action-log tail without mutating game state.

## Current Auth Session

Server-side auth persists account/session data through `JsonOnlineStore`:

- `PlayerSessionEntity.SessionId`
- `PlayerSessionEntity.PlayerId`
- refresh-token hash;
- expiration/revocation;
- `LastKnownRoomId`;
- `LastKnownTableId`;
- `LastKnownSeatIndex`.

Client-side `ChessOnlineClientSession` keeps access/refresh token response in memory. `ChessOnlineApp` intentionally does not persist temporary passwords or tokens by default.

Implication: a same-process reconnect can resume because the in-memory session is still present. App-restart resume needs either a fresh login or an explicit operator-provided temporary test account. P4J should not silently store tokens in repo files or logs.

## Current Room/Table Persistence

`Chess3DRelayHub` persists:

- rooms from room creation/matchmaking;
- tables from matchmaking/start;
- seats from matchmaking/join/ready;
- accepted action-log events;
- table `StateHash`;
- table `SaveGameJson` when a snapshot-bearing event is persisted.

`PersistentTableEntity` contains enough data to describe a table:

- `RoomId`;
- persistence `TableId` key;
- `RulesetId`;
- `State`;
- `ServerSeq`;
- `StateHash`;
- `SaveGameJson`.

`PersistentSeatEntity` stores player-to-seat mapping. `PersistentActionLogEntity` stores action events and notation.

## Runtime Registry Boundary

The active authority lives in `OnlineRoomRegistry` and `OnlineGameSession`.

Current code can persist room/table/action state, but startup wiring creates a new `OnlineRoomRegistry(options.ProfileRoot)`. `RestoreRoomsOnStartup` exists in configuration/options, but this audit did not find implemented runtime rehydration from persisted tables/actions into active `OnlineRoomRegistry` sessions.

Therefore:

- active in-memory matches can support a resume hub method safely;
- after server restart, the server may have persisted table data but no active native session;
- a Phase 08 resume method must return a clear failure such as `cannotResumeAfterServerRestartYet` until registry/session rehydration is implemented.

This matches earlier durability notes and avoids pretending that persisted JSON alone is already an active authority session.

## What Survives Server Restart

Survives in store:

- accounts;
- sessions and last-known room/table/seat metadata;
- persisted room/table rows;
- persisted seats;
- persisted accepted action log rows;
- table state hash/savegame where persisted after snapshot-bearing events.

Does not currently survive as active runtime:

- SignalR connection ids;
- hub groups;
- in-memory `OnlineRoomRegistry` table/session objects;
- native `OnlineGameSession` instances unless rehydration is added later.

## What Survives Client Restart

Tracked repo files do not store credentials or tokens.

Survives only if the operator remembers/re-enters credentials:

- server-side account/session records;
- match records in the server store;
- room/table/seat ids if the user saved a sanitized report or copied them manually.

Does not survive by default:

- temporary passwords generated in UI;
- access tokens;
- refresh tokens;
- active `ChessOnlineRelayClient`;
- in-memory selected cell/legal preview state.

## Can Temporary Users Resume?

Within the same app process:

- yes, via reconnect/resync, because tokens/session and room/table context remain in memory.

After app restart:

- only if the temp user credentials are known and the server can validate the same player/session;
- the current P4F/P4G UI intentionally does not save temp passwords or tokens, so default temp users are disposable.

After server restart:

- not as a fully active game yet, because runtime registry/session rehydration is deferred.

## Minimum Append-Only Resume Contract

Recommended DTOs for Phase 07:

- `OnlineResumeRequest`
- `OnlineResumeResult`
- `OnlineResumeCandidate`
- `OnlineResumeFailureReason`

Request fields:

- `PlayerId` or authenticated player from token;
- `RoomId`;
- `TableId`;
- optional `SeatIndex`;
- expected `RulesetId`;
- last known `StateHash`;
- last known `ServerSeq`.

Result fields:

- `Success`;
- `RoomId`;
- `TableId`;
- `SeatIndex`;
- `RulesetId`;
- current snapshot if active;
- action-log tail;
- failure reason:
  - `notAuthenticated`;
  - `tableNotFound`;
  - `playerNotInTable`;
  - `rulesetMismatch`;
  - `staleState`;
  - `tableNotActive`;
  - `cannotResumeAfterServerRestartYet`.

## Minimum Hub Method

Recommended append-only method for Phase 08:

```text
RequestResumeMatch(OnlineProtocolMessage message)
```

or a typed method if existing hub style allows:

```text
RequestResumeMatch(OnlineResumeRequest request)
```

The method should:

1. Validate authentication.
2. Validate room/table exists in active registry.
3. Validate player owns a seat at that table.
4. Validate optional ruleset/hash/seq hints.
5. Add current connection back to the table group.
6. Return authoritative snapshot and action-log tail.
7. Avoid mutating board/game state.

If the table exists only in persisted storage and not in active runtime, return `cannotResumeAfterServerRestartYet` until rehydration is implemented.

## UI Implications

Future `ChessOnlineApp` resume UI should store only non-secret resume context:

- room id;
- table id;
- ruleset id;
- seat;
- last known state hash;
- last known server seq;
- short player id for display.

It should not store:

- access token;
- refresh token;
- password;
- Authorization header.

## Future Work

Phase 07 should add DTOs and diagnostics flags. Phase 08 should add an active-match resume hub method. A later durability phase should implement `RestoreRoomsOnStartup` rehydration if app/server-restart resume is required.
