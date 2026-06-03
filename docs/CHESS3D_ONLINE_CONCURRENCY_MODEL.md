# Chess3D Online Concurrency Model

P3F keeps concurrency simple and deterministic.

## Authority Registry

`OnlineRoomRegistry` serializes authority mutations with an internal gate. This protects:

- room and table creation;
- seat claims;
- ready/start transitions;
- action validation and `serverSeq`;
- diagnostics counters.

This coarse lock is acceptable for the local hosted prototype. It favors deterministic tests over high throughput.

## Connection Registry

`OnlineHubConnectionRegistry` separately tracks:

- connection id to session;
- session token to session;
- active connection count;
- lightweight per-connection command throttling.

## Guarantees Tested

- Parallel duplicate seat claims produce exactly one winner.
- Parallel accepted submissions keep unique monotonic `serverSeq` values.
- Rejected and stale actions do not replace authoritative state.

## Future Work

Production scale should introduce finer-grained locks or actor/mailbox ownership per table, durable session storage, and a backplane if multiple server instances are used.

