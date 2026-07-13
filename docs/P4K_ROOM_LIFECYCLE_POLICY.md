# P4K Room Lifecycle Policy

Date: 2026-07-13  
Status: implementation gate for Phase 31.

## Safety Principle

Cleanup may reclaim only state whose lifecycle is explicit and whose removal
cannot invalidate an active game or a supported resume path. The first
implementation must prefer retention over accidental deletion.

In particular, **active** and **disconnected-resumable** tables are never
automatically deleted by Phase 31, regardless of elapsed time.

## States

| Lifecycle state | Runtime mapping / definition | Automatic cleanup in Phase 31 |
| --- | --- | --- |
| `waiting` | `WaitingForPlayers` or `ReadyCheck`; never started. | Eligible only when no connected seat and idle beyond waiting TTL. |
| `ready` | `ReadyCheck` with at least one connected/ready seat. | Not eligible while a connected seat remains. Otherwise follows waiting TTL from last activity. |
| `active` | `InGame` with all occupied seats connected. | Never eligible. |
| `disconnected-resumable` | `InGame` with at least one retained seat disconnected. | Never eligible in Phase 31. |
| `completed` | `Finished` with a completion timestamp. | Eligible after completed retention. |
| `abandoned` | Explicit `Abandoned`, or a waiting table classified abandoned after TTL. | Eligible after abandoned retention; classification and deletion may occur in separate runs. |
| `expired` | Internal terminal classification for a record selected by a bounded cleanup run. | Removed atomically from the in-memory registry; persistence deletion remains gated by store support. |

`expired` is not a sixth game mode or a new RuleProfile. It is maintenance
state only.

## Required Timestamps

Each managed table needs UTC timestamps:

- `createdUtc`: table creation;
- `startedUtc`: first transition to `InGame`;
- `lastActivityUtc`: max of creation, start, accepted action, seat/presence, and
  lifecycle transition;
- `completedUtc`: transition to `Finished`, otherwise null;
- `disconnectedUtc`: first time an occupied active table becomes resumable,
  cleared when all retained seats reconnect;
- `expiresUtc`: derived advisory value for an eligible terminal/waiting state,
  never used alone to delete an active/resumable table.

All calculations use an injected `TimeProvider` and UTC. Production uses
`TimeProvider.System`; tests use a controllable fake provider.

## Conservative Defaults

| Setting | Default | Meaning |
| --- | ---: | --- |
| Cleanup interval | 5 minutes | Minimum delay between hosted cleanup scans. |
| Maximum removals per run | 32 | Prevents one scan from monopolizing the registry lock or persistence I/O. |
| Waiting idle TTL | 6 hours | Applies only to never-started tables with no connected seat. |
| Completed retention | 7 days | Keeps outcome/action evidence for operator debugging. |
| Abandoned retention | 7 days | Keeps abandoned context before deletion. |
| Malformed orphan grace | 1 hour | Applies only to a record with no active authority, no seats, and no resumable ownership. |
| Spectator orphan grace | 5 minutes | Applies to internal spectator records whose table no longer exists; normal disconnect remains immediate. |

Configuration values must be normalized to safe minimums and maximums. A zero
or negative retention never means immediate delete; it falls back to defaults.

## Eligibility Algorithm

For every candidate, in this order:

1. If table is `InGame`, return **retain**. This includes active and
   disconnected-resumable states.
2. If any connected seat exists, return **retain**.
3. If state is waiting/ready and `now - lastActivity < 6h`, retain.
4. If waiting/ready exceeds TTL, mark `Abandoned` and set lifecycle activity;
   do not remove in the same classification operation.
5. If `Finished` and completion age is below 7d, retain; otherwise eligible.
6. If `Abandoned` and abandoned age is below 7d, retain; otherwise eligible.
7. A malformed orphan is eligible only when it has no session/authority, no
   seats, no action log, and exceeds the 1h grace.
8. Remove at most 32 eligible items per run.

Deleting a table must dispose its native session. A room may be removed only
after all its tables are gone and it has no players requiring resume. Cleanup
must never delete profiles/assets, auth sessions, Data Protection keys, or
action history belonging to retained tables.

## Spectator Records

Normal disconnect removes spectator membership immediately. A separate bounded
prune may remove only membership whose room/table no longer exists and whose
last-seen age exceeds five minutes. It must not affect seats, action sequence,
or board hash.

## Diagnostics

Expose aggregate values only:

- active tables;
- resumable tables;
- completed tables;
- expired/removed tables;
- spectator count;
- cleanup run count;
- last cleanup UTC;
- last cleanup removed count.

No room/table/player/connection identifier is required in public diagnostics or
logs.

## Phase 31 Gate

Implementation is allowed because eligibility is now deterministic and
conservative, provided it includes:

- injectable clock;
- fake-clock tests;
- bounded batch size;
- explicit active/resumable retention tests;
- no persistence deletion unless the store gains a tested atomic delete API;
- no deployment before the subsequent package/deploy phase.
