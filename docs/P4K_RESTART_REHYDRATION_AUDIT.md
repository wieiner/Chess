# P4K Restart Rehydration Audit

Date: 2026-07-13

## Scope And Conclusion

Phase 33 audits whether the current JSON persistence layer can recreate an
authoritative native Chess3D match after `chessonline.service` restarts. It does
not change code, native ABI, Chess3D rules, the five RuleProfiles, persistence
files, or the deployed server.

**Conclusion: the available data is close, but exact restart rehydration is not
currently guaranteed.** A valid latest `SaveGameJson` is sufficient to recreate
native state and verify its deterministic hash. The online write path, however,
stores a start snapshot and then appends accepted actions separately. Native
mutation and persistence are not one atomic transaction, and startup does not
validate/replay/install these records into `OnlineRoomRegistry`.

`RestoreRoomsOnStartup` exists as a disabled option but has no implementation.

## Current Runtime Path

The in-memory authority is:

1. `OnlineRoomRegistry` owns rooms, tables, seats, server sequence, action log,
   and `IChessOnlineRulesAuthority` sessions.
2. `OnlineGameSession` owns `NativeChess3DEngine` and loads one of the five
   profile JSON files.
3. `SubmitAction` checks seat/turn/hash, mutates the native session, increments
   `ServerSeq`, and appends an in-memory `OnlineActionEvent`.
4. `Chess3DRelayHub` persists the accepted event after the registry operation.
5. Snapshot requests export the current native savegame but do not themselves
   update the persistent table record.

Legal preview, failed actions, spectator requests, and snapshot reads do not
advance native state, server sequence, or persistent action history.

## Persisted Data Inventory

### Store document

`JsonOnlineStore` writes one document with schema version `0.1` using a temporary
file followed by replace/copy fallback. The document contains accounts, auth
sessions, rooms, tables, seats, and actions.

The store file write is atomic at document granularity in the normal replace
path, but a game operation performs several separate store calls. There is no
single match transaction spanning native mutation, table snapshot, action log,
seat state, and player-session metadata.

### Room

`PersistentRoomEntity` contains:

- `RoomId`, display name, owner player;
- created/updated UTC;
- string state;
- last server sequence.

The state is currently a protocol message type, not a versioned lifecycle enum.

### Table

`PersistentTableEntity` contains:

- composite persistence key in `TableId` (`roomId/tableId`);
- separate `RoomId`;
- `RulesetId` and `ProfileKind`;
- string state;
- `ServerSeq`, `StateHash`, `SaveGameJson`;
- created/started/finished/updated UTC.

At matchmaking, the table has no savegame. At `StartGame`, `PersistTable` stores
the initial authoritative snapshot at sequence 0 and clears any old action log
for the reused persistence key.

**Accepted actions do not upsert the table.** Consequently its persisted
savegame/hash/sequence remain at game start while the action log advances.

### Seats

`PersistentSeatEntity` contains table key, seat index, side/macro-player,
stable `PlayerId`, ready/connected flags, and last-seen UTC. This is enough to
restore ownership if validated against the profile seat model.

`IsConnected` cannot be trusted after process restart. Every restored seat must
start disconnected and regain presence only after authenticated `Hello` plus
explicit `RequestResumeMatch`.

### Action log

`PersistentActionLogEntity` contains:

- table key and `ServerSeq`;
- action index, kind, serialized command, notation;
- actor `PlayerId`;
- before/after state hash fields;
- previous/event hash chain;
- created UTC.

Current hub persistence writes `StateHashAfter` but leaves `StateHashBefore`
empty. `JsonOnlineStore` rejects duplicate `(table, serverSeq)` appends and
computes an event hash, but load does not validate sequence continuity or the
stored hash chain.

### Auth session relationship

`PlayerSessionEntity` contains stable `PlayerId` and last-known
room/table/seat. Refresh token hashes remain auth-store data and are not needed
to reconstruct game state. Resume authorization must compare the authenticated
player identity with restored seat ownership; it must not trust a client-sent
player ID or old SignalR connection/session token.

## Native Restore Capability

The native savegame v0.1 contains ruleset JSON/id, board, core stacks, reserve,
turn/macro-player, game outcome, and native action history. Load is
transactional and recomputes derived fusion, anchors, and outcomes.

Existing tests prove save/load hash equality for Classic, Single-Side, Asgard,
Rubik, and Hodge. `NativeChessOnlineGameSessionFactory.HashFromSaveGameJson`
already loads a save into a fresh engine and returns its deterministic hash.

Missing managed runtime boundary:

- `IChessOnlineRulesAuthority` exposes snapshot creation and action apply but no
  restore method;
- `IChessOnlineGameSessionFactory` creates only a profile-initialized session;
- `OnlineRoomRegistry` has no validated install/rehydrate method.

No native ABI change is required: the existing `LoadSaveGameJson` and state hash
ABI are sufficient. A future managed factory overload can load and verify a
snapshot before installing the session.

## Exact Reconstruction Analysis

### Can native state be recreated exactly?

Yes from a valid **latest** savegame whose loaded hash equals the persisted
authoritative hash. Current online records do not guarantee that latest
savegame exists after actions.

Reconstruction from the initial savegame plus every accepted action is also
possible in principle:

1. load sequence-0 savegame;
2. require contiguous actions `1..ServerSeq`;
3. deserialize only known action kinds;
4. apply each through the same native authority path;
5. require each computed hash to equal `StateHashAfter`;
6. require final hash/sequence to match the descriptor.

Current records lack a trustworthy final table sequence/hash after actions, and
the log chain is not validated on load, so this is not yet a complete invariant.

### How should state hash be verified?

Load into a private fresh session, compute native `StateHash`, compare using
ordinal equality, and install the session only after all descriptor/log/seat
checks pass. Never mutate or expose a shared table while validation is running.

### How should server sequence be restored?

The descriptor sequence must equal the maximum contiguous action sequence and
the final replayed hash. Sequence 0 requires an empty action log. The next
accepted action is exactly `restoredServerSeq + 1`.

### How are duplicate sequences avoided?

- validate uniqueness and strict continuity during restore;
- keep the existing duplicate append guard;
- install one table under the registry lock only once;
- preserve optimistic `ExpectedServerSeq`/state-hash checks;
- never replay persistent actions through the hub persistence path.

### How are seats restored safely?

- require unique seat indices and player IDs;
- validate seat range, side, macro-player, and count against the resolved
  profile;
- restore ownership/readiness only;
- force `IsConnected=false`;
- require authenticated player identity on resume before adding new SignalR
  group membership.

### How is a partial persisted match handled?

Do not expose it. Examples include missing room, unknown ruleset, empty/corrupt
savegame, action gap, duplicate sequence, hash mismatch, malformed action JSON,
invalid seat, and snapshot/log disagreement. A future loader must quarantine the
descriptor with aggregate safe diagnostics and continue loading healthy
matches. It must not crash server startup or create a half-populated table.

### How is auth/session continuity preserved?

Accounts and durable auth sessions already reload independently from the same
store. Game ownership is restored from seats by `PlayerId`. A still-valid
authenticated session may use its last-known match as a client hint, but the
server must authorize against the restored seat. Expired/revoked sessions do
not invalidate game ownership; they require login before resume.

## Crash Windows And Gaps

1. Native action succeeds, process stops before `AppendActionAsync`: persistent
   snapshot/log remain behind and exact current state is lost.
2. Action append succeeds but no updated table snapshot is written: replay may
   recover state, but no authoritative table-level final seq/hash is available.
3. Start persistence clears old actions and upserts table in separate writes.
4. Matchmaking room/table/seats/session hints are separate writes and can be
   partially present.
5. Store schema is global `0.1`; table descriptors have no own schema version or
   quarantine metadata.
6. Stored string states are protocol messages rather than a stable lifecycle
   contract.
7. Action load does not validate event hashes, previous hash linkage, sequence
   continuity, or command/profile compatibility.
8. Existing `RestoreRoomsOnStartup=false` is copied into options but never
   consumed by a restore service.

## Phase 35 Gate

The Phase 35 implementation gate is **not yet satisfied by the current schema
and write ordering**. Phase 34 should design a versioned descriptor and atomic
checkpoint policy first. A safe minimal implementation needs at least:

- versioned match descriptor;
- current post-action savegame/hash/sequence checkpoint;
- validated seat ownership snapshot;
- transactional or explicitly ordered persistence update with crash semantics;
- action continuity/hash-chain validation;
- private-session load and quarantine-before-install path;
- deterministic tests for valid and partially persisted records.

Until those requirements exist, service restart resume remains honestly
deferred. Active in-memory disconnect/resume continues to work and Phase 32's
cleanup retains those matches.
