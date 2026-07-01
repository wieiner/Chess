# P4J Phase 19 - Client Lobby Support

Date: 2026-07-01

## Summary

Phase 19 adds client-side support for the Phase 18 lobby snapshot protocol. It does not add UI yet; that is Phase 20.

## Relay Client

`ChessOnlineRelayClient` now exposes:

- `RequestLobbySnapshotAsync(clientId, request, cancellationToken)`
- `LastLobbySnapshot`

It also registers:

- `ReceiveLobbySnapshot`

The client follows the existing SignalR pattern used for snapshots, action logs, legal previews, resume and spectator results.

## Client Display Models

New helper models:

- `OnlineLobbyFilterState`
- `OnlineLobbyTableDisplayRow`

`OnlineLobbyFilterState.ToRequest()` creates an `OnlineLobbySnapshotRequest` with trimmed ruleset filters and table-state toggles.

`OnlineLobbyTableDisplayRow.FromSnapshot(...)` converts protocol rows into compact UI rows:

- room/table;
- ruleset;
- table state;
- seats occupied/max;
- spectator count;
- last server sequence;
- updated time;
- short seat summary;
- `CanJoinAsPlayer`;
- `CanSpectate`;
- `DisplayLabel`.

## Privacy

The client display row uses the server-provided short player labels. It does not add tokens, passwords, Authorization headers, refresh tokens, or SignalR connection IDs.

## Verification

Phase 19 verification:

- `ChessOnlineClient` build;
- targeted `ChessOnlineContractTests`;
- tests assert callback registration, initial lobby state, filter conversion, and display-row behavior.
