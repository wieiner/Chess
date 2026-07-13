# P4K Spectator Lifecycle Audit

Date: 2026-07-13  
Scope: read-only audit; no runtime code or server configuration changed.

## Executive Finding

The current spectator feature is functionally read-only and receives table
broadcasts, snapshots, and action logs, but it does **not** maintain an
authoritative spectator membership registry. `OnlineLobbyTableRow.SpectatorCount`
is currently a placeholder hardcoded to `0`.

SignalR group membership is therefore sufficient for live message delivery but
not for exact counting, duplicate suppression, reconnect replacement, lifecycle
diagnostics, or restart recovery.

## Current Join Path

1. `Chess3DRelayHub.JoinSpectator` validates/invokes
   `OnlineRoomRegistry.JoinSpectator`.
2. The room registry validates authenticated player identity, room/table,
   expected ruleset, and active table state.
3. It returns an authoritative snapshot, bounded action-log chunk, and an
   `OnlineSpectatorState` with `ViewerPlayerId` and a derived spectator label.
4. The hub writes room/table into the generic
   `OnlineHubConnectionRegistry` session and adds the current connection to the
   room and table SignalR groups.

The operation does not mutate the game session or state hash. Contract tests
cover snapshot/log access and rejection of spectator action submission because
the viewer has no seat.

## Membership and Identity Matrix

| Concern | Current behavior | Accuracy / risk |
| --- | --- | --- |
| Authenticated viewer identity | `PlayerId` comes from the authenticated hub envelope; public state also exposes `ViewerPlayerId`. | Stable application identity exists, but no membership record consumes it. |
| Transport identity | `ConnectionId` is retained only in the internal generic connection session. | Correctly absent from public spectator/lobby DTOs. |
| SignalR groups | Successful join adds the current connection to room and table groups. | Live broadcast works; group membership is transport state, not an enumerable authoritative registry. |
| Duplicate join, same connection | Registry repeats validation and group-add calls. | No count inflation today only because there is no count. Idempotence is not modeled or tested. |
| Same viewer, new connection | Authenticated `Hello` can reuse the session and add the new connection. | Generic room/table context can be restored, but there is no explicit spectator-role replacement record. |
| Multiple connections per viewer | Generic session owns a `HashSet<string>` of connection IDs. | Valid transport support, but future spectator count must count viewers, not connections. |
| Public spectator count | Lobby builder assigns `SpectatorCount = 0`. | Placeholder, never runtime-truth. |

## Disconnect Behavior

`Chess3DRelayHub.OnDisconnectedAsync` removes the connection from known room
and table groups, calls `OnlineHubConnectionRegistry.Disconnected`, updates the
active-connection diagnostic, and delegates to the base hub implementation.
SignalR also automatically removes disconnected connections from groups.

The generic registry removes the connection from `_byConnection` and from the
session's `ConnectionIds`, preserving the in-memory token/session object for a
possible reconnect. It does not notify `OnlineRoomRegistry`, update a spectator
count, update lobby timestamps, or mark a role-specific spectator record.

Consequences:

- a disconnected transport does not remain in `_byConnection`;
- no exact spectator record can become stale because none exists;
- equally, the server cannot prove that a viewer left or report a decrement;
- seat connectivity is not changed by this path and requires separate player
  lifecycle hardening.

## Reconnect and Restart

An authenticated reconnect can reuse the in-memory session ID, recover its
generic room/table membership, and be re-added to SignalR groups by `Hello` or
an explicit spectator join. This is process-local behavior.

On server restart, `OnlineRoomRegistry`, `OnlineHubConnectionRegistry`, and
SignalR group membership are newly constructed singletons. Spectator state is
not persisted or rehydrated. A viewer must authenticate/connect and join again;
the old transport membership is gone. This is acceptable for transient
spectators, but it must be stated explicitly and must not be reported as a
persisted count.

## Direct Answers

- **Is `spectatorCount` real?** No. It is a hardcoded placeholder `0`.
- **Can duplicate join inflate it?** Not currently, because no counter exists.
  A future implementation needs explicit idempotence tests.
- **Does disconnect decrement it?** No counter exists and no spectator-specific
  cleanup is called.
- **Can an old connection remain stale?** The normal disconnect path removes it
  from the generic registry and SignalR handles group removal. Abrupt process
  loss/restart discards all in-memory transport state. There is no independent
  registry with which to detect a missed callback.
- **Is viewer identity separated from `ConnectionId`?** At the DTO/session
  boundary, yes: authenticated `PlayerId` is distinct and no connection ID is
  serialized publicly. There is no durable spectator membership model yet.

## Required Phase 28 Boundary

Add an internal, lock-protected spectator registry keyed by table plus
authenticated viewer identity, with current connection, joined/last-seen UTC,
and reverse lookup for disconnect. The public lobby may expose only the count.
Repeated join on the same connection must be idempotent; reconnect must replace
the old connection for that viewer; distinct viewers must increment the count.

Connection IDs must remain absent from protocol DTOs, diagnostics payloads,
session reports, and logs. Joining/leaving must not mutate seats, board state,
action history, server sequence, or state hash.

## Deferred Boundaries

- Player-seat disconnect/resume markers belong to Phase 29.
- Room TTL and cleanup belong to Phases 30-31.
- Match rehydration across service restart belongs to Phases 33-36.
- No nginx, systemd, UFW, TLS/443, or neighboring service change is required.
