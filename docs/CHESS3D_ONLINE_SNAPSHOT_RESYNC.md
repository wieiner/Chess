# Chess3D Online Snapshot And Resync

Snapshots are the recovery mechanism for stale or reconnecting clients.

## Snapshot Contents

`OnlineSnapshot` includes:

- ruleset id;
- savegame JSON;
- state hash;
- action count;
- game phase;
- game outcome;
- turn summary;
- last action notation.

## Stale Hash

If `OnlineActionCommand.expectedStateHashBefore` is present and differs from the authoritative hash, the command is rejected with `ResyncRequired`. The response includes a fresh snapshot.

## Transactionality

Invalid commands and stale commands do not mutate authority state. Clients should replace local state from the snapshot before retrying.

## Reconnect

P3E does not implement durable sessions. Reconnect behavior is represented by requesting snapshot/action-log chunks from the in-process authority.
