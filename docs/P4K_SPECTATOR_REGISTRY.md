# P4K Spectator Registry

Date: 2026-07-13

## Purpose

`OnlineSpectatorRegistry` is a server-internal source of truth for live
spectator membership. It closes the placeholder-count gap documented in
`P4K_SPECTATOR_LIFECYCLE_AUDIT.md` without moving transport concepts into the
game/protocol authority.

## Model

Each membership contains:

- normalized room/table key;
- authenticated viewer/player identity;
- current SignalR connection ID;
- join UTC;
- last-seen UTC.

The primary key is `(room, table, authenticated viewer)`, so the lobby count is
the number of distinct viewers, not the number of browser/windows/transports.
A reverse connection lookup is retained internally for disconnect cleanup in
Phase 29.

## Join Semantics

- First viewer on a table creates one membership.
- Repeating join from the same viewer and connection is idempotent.
- A genuinely different viewer increments the count.
- The same viewer joining after reconnect replaces the previous transport
  mapping and does not increment the count.
- If one connection moves to a different spectator table, its old membership
  is removed before the new one is registered.

When a reconnect replaces a connection, `Chess3DRelayHub` removes the
superseded transport from its room/table SignalR groups and adds the current
connection. SignalR groups remain message-routing state; the registry remains
the membership/count authority.

## Public Boundary

No protocol DTO layout changed. `RequestLobbySnapshot` overlays the existing
`OnlineLobbyTableRow.SpectatorCount` with the registry count immediately before
the response is sent.

Connection IDs remain inside `ChessOnlineServer`:

- no DTO contains one;
- lobby exposes count only;
- diagnostics and session reports are unchanged;
- the internal replacement result explicitly marks the superseded connection
  property with `JsonIgnore`;
- no connection value is logged.

## State Isolation

Spectator registration does not call the native authority and does not mutate
seats, board state, action history, server sequence, or state hash. The five
RuleProfiles and their rules remain unchanged.

## Verification

`ChessOnlineSignalRContractTests` covers both the registry and an actual
in-process hub:

- first join gives count `1`;
- duplicate join remains `1`;
- second viewer gives count `2`;
- first viewer reconnect remains `2` and replaces the transport;
- seats are unchanged;
- authoritative state hash is unchanged;
- lobby JSON contains neither a connection ID field nor its runtime value.

Targeted command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 `
  -Only SignalR -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 `
  -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 `
  -GlobalTimeoutSeconds 300
```

Result on 2026-07-13: PASS, no timeout.

## Deferred to Phase 29

This phase records reverse connection membership but deliberately does not yet
decrement it from `OnDisconnectedAsync`. Phase 29 will add idempotent transport
cleanup, keep seated-player ownership resumable, and test disconnect state/hash
invariants.
