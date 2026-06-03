# Chess3D Online Action Log

Accepted online actions produce `OnlineActionEvent` records.

## Event Fields

Each event contains:

- `serverSeq`;
- original action kind;
- actor player/side/macro-player;
- notation;
- state hash before;
- state hash after;
- accepted UTC timestamp.

## Chunks

`OnlineActionLogChunk` returns events from a requested sequence offset. It is enough for reconnect smoke tests and future replay/export integration.

## Replay

P3E tests replay online action logs through `OnlineRoomRegistry.ReplayActionLogToHash`. Replay uses existing engine action entry points and compares the final state hash with the authoritative snapshot.

## Deferred

Long-term log persistence, signed logs, compact binary logs, and cross-version migration are future work.
