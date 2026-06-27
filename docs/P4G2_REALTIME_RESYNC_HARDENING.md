# P4G2 Realtime Resync Hardening

Date: 2026-06-27

Scope: P4G2 Phase 11. This phase adds client-side realtime sequence diagnostics and a lightweight resync path for `ChessOnlineApp`. It does not change server protocol, server deployment, Chess3D rules, native ABI, or the five existing RuleProfiles.

## Client Sync State

`OnlineRealtimeSyncState` tracks:

- latest server sequence;
- latest snapshot hash;
- duplicate event count;
- sequence gap count;
- whether a resync is required;
- last realtime reason/message type.

It is intentionally a client-side diagnostic helper. The server remains authoritative for state hash, action legality, seat ownership, and snapshots.

## Duplicate And Gap Handling

For sequenced events:

- `seq <= lastSeq` is treated as duplicate and does not advance the local sequence.
- `seq > lastSeq + 1` is treated as a gap and marks `ResyncRequired`.
- `ResyncRequired` and stale-hash messages mark resync even if they reuse an already observed sequence number.
- A fresh authoritative snapshot clears the resync flag.

## UI Status

`ChessOnlineApp` now shows:

- last server sequence;
- short snapshot hash;
- duplicate count;
- gap count;
- resync yes/no;
- last realtime reason.

The status line updates through the existing WPF dispatcher callback path.

## Automatic Soft Resync

When a SignalR callback indicates a gap or resync requirement, the UI attempts a bounded soft refresh:

1. request authoritative snapshot;
2. request action log;
3. append recent action-log tail entries;
4. clear the local resync flag after a fresh snapshot.

The refresh is best-effort and guarded by a pending flag so repeated callbacks do not start many overlapping refreshes.

## Safety

Realtime logs include only message type, server sequence, state hash snippets, and reason text. Tokens, passwords, authorization headers, key material, and runtime stores are not logged or committed.

## Verification

`ChessOnlineContractTests` cover:

- first sequenced event;
- duplicate detection;
- sequence gap detection;
- stale/resync duplicate response;
- fresh snapshot clearing resync.

`ChessOnlineApp` build verifies the WPF integration. Remote Hetzner smoke remains manual/operator-run and is not part of CI.
