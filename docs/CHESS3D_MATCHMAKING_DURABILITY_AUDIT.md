# Chess3D Matchmaking Durability Audit

Status: P4C Phase 06.

## Scope

This audit covers the online matchmaking MVP, reconnect expectations, and persistence boundary. It does not add Redis, Azure SignalR, public ranked queues, or cross-server matchmaking.

## Current Runtime Pieces

- `OnlineMatchmakingService` owns queue tickets in memory.
- `OnlineRoomRegistry` owns authoritative room/table/session state in memory.
- `Chess3DRelayHub` bridges SignalR commands to the registry and writes selected durable state to `IOnlineRoomPersistenceStore`.
- `JsonOnlineStore` persists players, sessions, rooms, tables, seats, and accepted action events.

## Ticket Policy

Queued tickets are in-memory P4C state.

Current behavior:

- one active ticket per player;
- queues keyed by exact `rulesetId`;
- `single-side` needs one player;
- other current profiles need two players;
- tickets expire;
- duplicate queue joins are rejected;
- cancel removes the active ticket.

Restart behavior:

- queued tickets are lost;
- lost queued tickets do not corrupt player accounts or durable sessions;
- clients should call matchmaking status again after reconnect;
- UI should treat missing ticket after reconnect as `Idle`, not as data corruption.

This is intentional for P4C. Durable queue tickets are future work.

## Matched Table Policy

Once matchmaking returns `MatchFound`, the match has crossed from queue state into room/table state.

P4C hardens this boundary:

- match-found room is persisted;
- match-found table is persisted;
- seat assignments are persisted;
- connected authenticated player sessions record last-known room/table/seat.

The JSON store remains a persistence record, not the rules authority. Gameplay legality still flows through `OnlineRoomRegistry` and the `IChessOnlineRulesAuthority` implementation.

## Action Log Policy

Accepted gameplay actions continue to be persisted by `PersistAcceptedAction` after the authoritative registry accepts them.

Rejected actions are not persisted as action log events.

## Reconnect Policy

Current reconnect support:

- active SignalR session token can reconnect to in-process room/table membership;
- authenticated sessions store last-known room/table/seat after manual seat join and after match-found;
- clients can request authoritative snapshot and action log chunks after reconnect.

Limitations:

- a server process restart does not currently reconstruct live `OnlineRoomRegistry` objects from the JSON store;
- `RestoreRoomsOnStartup` exists as an option but is not implemented as full room/table/session rehydration;
- queued tickets are intentionally not durable.

## Store Reload Reality

`JsonOnlineStore` can reload persisted data from disk. The current hosted runtime does not yet replay that durable data into an active registry after restart.

Therefore:

- durable matched room/table/seat records are useful for diagnostics, bug reports, and future restore implementation;
- they do not yet mean a restarted server automatically resumes a live table.

## Tests

P4C Phase 06 extends SignalR contract coverage so that authenticated matchmaking persists:

- Classic match-found room;
- Classic match-found table;
- Classic match-found seats;
- last-known table membership for a matched player session.

Existing tests continue to cover:

- queued ticket creation;
- duplicate ticket rejection;
- status reporting;
- Asgard matchmaking;
- matched Asgard table start;
- accepted action persistence.

## Future Work

- Implement `RestoreRoomsOnStartup` rehydration.
- Decide whether queued ticket persistence is worth the product complexity.
- Add explicit client UX for “queue lost after server restart”.
- Add reconnect screen that offers last-known table resume when durable table state exists.
- Add multi-server queue/backplane only after a single-server product is stable.
