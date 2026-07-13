# P4K Restart Rehydration Design

Date: 2026-07-13

## Status

This document specifies the P4K restart-resume persistence contract. It does
not implement or deploy the contract. The current `RestoreRoomsOnStartup`
option remains disabled and server-restart resume remains deferred.

The design preserves the existing five RuleProfiles, native ABI, savegame v0.1,
action history, state hash, active in-memory resume, auth/session model, and
HTTP deployment boundary.

## Product Invariant

After a process restart, the server may expose a match as resumable only when it
can prove all of the following:

1. the descriptor schema is supported;
2. room/table/profile identity is valid and unambiguous;
3. the profile is one of the existing five runtime profiles;
4. the native savegame loads transactionally into a private session;
5. loaded ruleset and computed state hash equal the descriptor;
6. server sequence and action-log continuity agree;
7. seat ownership is valid for the profile;
8. the entire candidate can be installed atomically in the registry;
9. no client event is broadcast during restore.

Any failed invariant quarantines that descriptor. One broken match must not
prevent healthy matches or the server itself from starting.

## Versioned Persisted Descriptor

Introduce a new descriptor format rather than inferring lifecycle truth from
protocol message strings.

```json
{
  "format": "chessonline-match-checkpoint",
  "schemaVersion": "0.2",
  "checkpointId": "opaque-generated-id",
  "roomId": "logical-room-id",
  "tableId": "logical-table-id",
  "persistenceKey": "room-id/table-id",
  "rulesetId": "asgard-convergence-3d-8x8x8-v0.1",
  "lifecycleState": "inGame",
  "saveGameJson": "{...chess3d-savegame v0.1...}",
  "authoritativeStateHash": "...",
  "lastServerSeq": 12,
  "lastEventHash": "...",
  "nativeActionCount": 12,
  "seats": [],
  "createdUtc": "...",
  "updatedUtc": "..."
}
```

Required fields:

- `format` and `schemaVersion`;
- logical `roomId` and `tableId` as separate fields;
- stable persistence key used only for repository indexing;
- exact `rulesetId`;
- versioned lifecycle state;
- current full `SaveGameJson`;
- authoritative native state hash;
- last committed server sequence;
- final action-event hash and native action count;
- complete seat ownership snapshot;
- created and updated UTC.

Optional/future fields must be ignored only when the schema version explicitly
permits them. Unknown schema versions are quarantined, not guessed.

## Lifecycle Values

Version 0.2 supports:

- `waitingForPlayers`;
- `readyCheck`;
- `inGame`;
- `finished`;
- `abandoned`;
- `quarantined` (repository status, never a playable table).

Only `inGame` checkpoints with a valid savegame are restored as resumable native
matches. Waiting/ready descriptors may be restored later without native state,
but are outside the first implementation. Finished/abandoned records remain
retained for lifecycle cleanup and history, not active resume.

## Seat Snapshot

Each persisted seat contains:

- `seatIndex`;
- `sideId`;
- `macroPlayer`;
- stable authenticated `playerId`;
- `isReady`;
- last-seen UTC.

Connection state and SignalR membership are intentionally absent from the
checkpoint. On restore every seat is installed as disconnected. Connection IDs,
hub session tokens, access/refresh tokens, passwords, and authorization headers
must never be persisted in a match descriptor.

Validation requires:

- unique seat indices;
- unique non-empty player IDs;
- indices inside the selected profile seat range;
- side/macro-player mapping matching the profile contract;
- no more seats than the profile permits.

## Atomic Checkpoint API

Add a versioned repository operation conceptually equivalent to:

```csharp
Task CommitMatchCheckpointAsync(
    PersistedMatchCheckpoint checkpoint,
    PersistentActionLogEntity? acceptedAction,
    MatchCheckpointExpectation expected,
    CancellationToken cancellationToken);
```

The expectation contains prior checkpoint id, server sequence, state hash, and
event hash. The repository must reject stale or duplicate writers.

For `JsonOnlineStore`, descriptor replacement and optional action append occur
under its existing lock and are serialized into one temporary document before
the atomic file replacement. A future database implementation uses one database
transaction with the same semantics.

Starting/reusing a table commits sequence 0, the initial full savegame, seat
snapshot, and cleared action chain in one operation.

## Accepted Action Commit Protocol

The hub must not announce acceptance before durable commit.

1. under the table authority lock, capture pre-action savegame, hash, sequence,
   and in-memory action count;
2. validate expected sequence/hash and seat/turn;
3. apply the action once through the existing authority;
4. create the post-action full savegame and hash;
5. create event `N+1` with before/after hash and previous event hash;
6. atomically commit checkpoint plus event with prior-state expectation;
7. update in-memory sequence/log/checkpoint identity;
8. release the authority lock;
9. broadcast `ActionAccepted` and snapshot/realtime event.

If durable commit fails:

1. load the captured pre-action savegame back into the same private authority;
2. verify restored hash, sequence, and action count;
3. keep the prior in-memory log/sequence;
4. return a safe persistence rejection without broadcasting acceptance.

If rollback itself fails, mark the in-memory table faulted/quarantined, reject
further actions, and require operator investigation. Never continue from an
unverified state.

This protocol defines durability in terms of **acknowledged actions**. A process
crash after native apply but before commit/broadcast legitimately restores the
last committed state; the client never received acceptance and may retry using
the authoritative restored hash/sequence.

## Startup Algorithm

A bounded startup rehydration service runs only when
`RestoreRoomsOnStartup=true`:

1. enumerate versioned descriptors from the store;
2. reject unsupported schema/format without throwing out of startup;
3. parse and validate logical IDs and lifecycle state;
4. resolve `rulesetId` from the exactly-five-profile catalog;
5. validate seat ownership snapshot;
6. validate action sequence uniqueness/continuity and event hash chain;
7. create a private native authority for the profile;
8. load `SaveGameJson` through the existing transactional ABI;
9. require embedded save ruleset to match descriptor ruleset;
10. recompute and compare state hash;
11. compare native action count with descriptor/log semantics;
12. require descriptor sequence, last event, and final event hash to agree;
13. build a complete private `OnlineRoom`/`OnlineTable` candidate;
14. set all seats disconnected and restore ownership/readiness;
15. install the complete candidate under one registry lock;
16. expose it as `InGame` and resumable;
17. dispose rejected private native sessions;
18. record only aggregate restore diagnostics.

No group join, hub broadcast, client callback, matchmaking enqueue, or action
replay persistence occurs during startup.

## Snapshot Versus Replay

The current full checkpoint is the primary restore source. The action log is an
audit/continuity proof and client history source, not the ordinary mechanism for
rebuilding every match from sequence 0.

Replay from an older snapshot is permitted only as a future recovery mode when:

- the checkpoint declares its base sequence/hash;
- every later action is contiguous and known;
- each replayed state hash matches the event;
- the replay path does not append events or broadcast;
- final hash/sequence match a committed descriptor.

No best-effort replay is allowed.

## Duplicate And Concurrency Protection

- one checkpoint id per committed version;
- compare-and-swap expectation on prior seq/hash/checkpoint/event hash;
- existing unique `(table, serverSeq)` action constraint retained;
- strict `1..N` continuity;
- one registry install per logical room/table pair;
- startup restore completes before readiness;
- resumed clients retain existing expected-hash/expected-sequence checks;
- cleanup skips every restored `InGame` table, connected or disconnected.

## Partial Persistence Policy

Quarantine reason codes:

- `unsupportedSchema`;
- `malformedDescriptor`;
- `missingRoom`;
- `unknownRuleset`;
- `invalidLifecycle`;
- `invalidSaveGame`;
- `rulesetMismatch`;
- `stateHashMismatch`;
- `actionSequenceGap`;
- `duplicateActionSequence`;
- `eventHashMismatch`;
- `nativeActionCountMismatch`;
- `invalidSeatOwnership`;
- `duplicateRuntimeTable`;
- `internalRestoreError`.

Quarantine records stay server-side. Public diagnostics expose only aggregate
counts by safe reason category and the last restore UTC. Logs omit save/action
JSON, player/room/table/connection IDs, tokens, passwords, and keyring paths.

Healthy descriptors continue loading after an invalid one. The server starts
with `ready` status only after the bounded restore scan completes or reports a
configured safe timeout. A timeout does not expose half-installed candidates.

## Auth And Resume

Authentication remains independent of match reconstruction:

1. account/session stores load normally;
2. player logs in or refreshes a valid auth session;
3. last-known room/table/seat is a UI hint only;
4. `RequestResumeMatch` checks authenticated `PlayerId` against restored seat;
5. successful resume marks presence connected and joins new room/table groups;
6. authoritative snapshot/log are returned from the restored runtime.

Revoked or expired auth sessions cannot resume until re-authentication, but
their seat ownership remains intact.

## Diagnostics

Append-only aggregate fields for a future implementation:

- restore enabled;
- restore completed;
- descriptors scanned;
- matches restored;
- descriptors quarantined;
- aggregate quarantine reason counts;
- last restore UTC/duration;
- restore startup timeout count.

Do not expose descriptor contents or identifiers.

## Test Plan

Required deterministic tests before enabling the option:

1. commit active match checkpoint after accepted action;
2. dispose original registry/session/store;
3. instantiate fresh store, factory, registry, and restore coordinator;
4. restore equal profile/hash/seq/native action count/action log/seats;
5. prove all restored seats disconnected;
6. authenticate the owner and resume successfully;
7. submit next action at exactly `N+1` without duplication;
8. reject stale expected seq/hash;
9. quarantine invalid JSON, unknown profile, hash mismatch, seq gap, duplicate
   seq, broken event hash, and invalid seat independently;
10. continue loading a healthy descriptor beside each broken one;
11. persistence failure rolls native state back to the pre-action hash;
12. startup exposes no candidate before full validation;
13. exactly five profiles remain available;
14. existing save/load/replay/active-resume/cleanup tests remain green.

## Implementation Gate

Phase 35 may proceed only after the repository gains the atomic checkpoint
contract and the tests above can be written without relying on current
best-effort multi-write entities. As of Phase 34, those prerequisites do not yet
exist. Therefore implementation and controlled service-restart smoke are not
authorized by this design phase.
