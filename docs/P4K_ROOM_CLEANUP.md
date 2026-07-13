# P4K Bounded Room Cleanup

Date: 2026-07-13

## Implementation

Phase 31 implements the conservative policy from
`P4K_ROOM_LIFECYCLE_POLICY.md` with three components:

- `OnlineRoomRegistry.RunCleanup`: locked, deterministic classification and
  bounded in-memory table removal;
- `OnlineRoomCleanupCoordinator`: one callable cleanup unit shared by tests and
  the hosted loop;
- `OnlineRoomCleanupService`: cancellable `PeriodicTimer` loop using the
  configured interval and injected `TimeProvider`.

Production resolves `TimeProvider.System`. Tests supply a manual provider and
advance UTC explicitly; no test sleeps for TTL boundaries.

## Defaults

- interval: 5 minutes;
- maximum removals per run: 32;
- idle waiting classification: 6 hours;
- completed retention: 7 days;
- abandoned retention: 7 days;
- malformed orphan grace: 1 hour;
- spectator orphan grace: 5 minutes.

Invalid zero/negative values fall back to defaults and all values are clamped.

## Cleanup Semantics

1. An idle never-started waiting/ready table with no connected seat is first
   classified `Abandoned`; it is not removed in that run.
2. An abandoned table is eligible only after its retention window.
3. A finished table is eligible only after completed retention.
4. A malformed orphan requires no authority, no seats, no actions, invalid
   profile metadata, and the orphan grace period.
5. At most the configured number of eligible tables are removed per run.
6. A removed in-memory table disposes its native authority session.
7. A spectator record is pruned only when its table is absent and its last-seen
   timestamp exceeds the spectator grace.

`InGame` tables are retained unconditionally. This covers both fully connected
active tables and disconnected-resumable tables. Cleanup never changes their
board, action log, sequence, state hash, or seats.

## Persistence Boundary

The current `IOnlineRoomPersistenceStore` has read/upsert/append/clear-action-log
methods but no atomic room/table delete operation. Phase 31 therefore deletes
only eligible **in-memory** tables and orphan spectator records. It does not
delete persistent table, seat, action, auth session, keyring, or account data.

Persistent deletion requires a later versioned repository contract plus
transactional tests. The hosted cleanup log contains aggregate removal counts
only, never room/table/player/connection identifiers.

## Diagnostics

The existing diagnostics payload is extended append-only with:

- `activeTableCount`;
- `resumableTableCount`;
- `completedTableCount`;
- `expiredTableCount` (cumulative in-process removals);
- `spectatorCount`;
- `cleanupRunCount`;
- `lastCleanupUtc`;
- `lastCleanupRemovedCount`.

These are aggregate process-local values and contain no transport IDs or
secrets.

## Verification

Fake-clock contract coverage proves:

- three waiting tables classify only after the waiting TTL;
- classification and deletion do not happen in one operation;
- batch size `2` removes `2`, then `1` on the next run;
- active and disconnected-resumable tables survive a one-year clock advance;
- their authoritative state hashes remain unchanged;
- orphan spectator membership survives before grace and is removed after it;
- diagnostics expose active/resumable/expired/run counters.

The full bounded Online suite passed:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 `
  -Suite Online -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 `
  -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 `
  -GlobalTimeoutSeconds 420
```

Result: `ChessOnlineContractTests` PASS and
`ChessOnlineSignalRContractTests` PASS; no watchdog timeout.

## Deployment

No server deployment occurs in this commit. Phase 32 owns reproducible package,
backup, guarded deploy, remote regression, and neighboring-service checks.
