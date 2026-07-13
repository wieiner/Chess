# P4K Disconnect Cleanup

Date: 2026-07-13

## Result

The SignalR disconnect path now distinguishes transient spectator membership
from durable player seat ownership.

## Spectators

`OnDisconnectedAsync` removes the current connection from the internal
spectator registry. Removal is idempotent:

- the active spectator record is removed once;
- the distinct-viewer lobby count decrements;
- the lobby table `UpdatedUtc` advances for the membership change;
- a duplicate disconnect is a no-op;
- a superseded connection from a prior reconnect cannot remove the current
  viewer mapping.

Registry cleanup and generic connection cleanup run in `finally`, so an error
while removing a SignalR group cannot leave application membership registered.

## Seated Players

Disconnect never removes a seat. When the last live connection in an
authenticated session closes, the matching room player and seat are marked
`IsConnected=false` with an updated last-seen timestamp. The persistent seat
presence record is updated best-effort.

If the same session still has another connection, its seat remains connected.
This avoids false offline state for multi-window/multi-transport clients.

On authenticated `Hello` after reconnect, the retained room/table context
marks the same seat connected again. `RequestResumeMatch` continues to validate
the stable player identity and seat ownership; no new seat is allocated.

Presence persistence failures are logged only as connected/disconnected plus
exception type. Player IDs, connection IDs, tokens, and paths are not logged.

## State Invariants

Disconnect/reconnect changes presence metadata only. It does not mutate:

- native board or stacks;
- action history;
- authoritative server sequence;
- state hash;
- profile/ruleset selection;
- seat ownership.

## Verification

The bounded `ChessOnlineSignalRContractTests` run covers:

- spectator disconnect changes lobby count `2 -> 1`;
- lobby timestamp changes;
- duplicate spectator disconnect is safe;
- seated player disconnect keeps both seats but marks the affected seat offline;
- reconnect marks it online;
- resume succeeds on the original seat;
- state hash remains equal across spectator disconnect, player disconnect, and
  resume.

Targeted command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 `
  -Only SignalR -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 `
  -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 `
  -GlobalTimeoutSeconds 300
```

Result on 2026-07-13: PASS, watchdog did not time out.

## Boundaries

This phase does not delete rooms/tables and introduces no TTL. Conservative
room lifecycle policy and bounded cleanup remain Phases 30-31. No deployment,
network, nginx, systemd, UFW, TLS/443, or neighboring-service change occurred.
