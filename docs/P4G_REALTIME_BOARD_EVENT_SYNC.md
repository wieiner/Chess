# P4G Realtime Board Event Sync

Date: 2026-06-27

`ChessOnlineRelayClient` now exposes a `MessageReceived` event for every remembered hub callback or invoked method result. `ChessOnlineApp` uses it conservatively for realtime board synchronization.

## Client Event Boundary

The shared client still owns the SignalR `HubConnection` and registers the same hub callbacks:

- `ReceiveGameStarted`;
- `ReceiveActionAccepted`;
- `ReceiveActionRejected`;
- `ReceiveAuthoritativeSnapshot`;
- `ReceiveActionLogChunk`;
- `ReceiveResyncRequired`;
- matchmaking and diagnostics events.

The new event:

```csharp
public event Action<string, OnlineProtocolMessage>? MessageReceived;
```

passes the source label and the protocol message. It does not change the wire protocol and does not require a server deployment.

## UI Sync Policy

`ChessOnlineApp` listens only to labels beginning with `Receive`. This avoids double-processing direct method results such as `RequestSnapshotAsync(...)` while still letting server-pushed events update the UI.

When a received message contains:

- `Snapshot`: the app parses `SaveGameJson` into the P4G board snapshot and redraws the selected layer;
- `ActionLog`: the app appends sanitized notation lines;
- any server sequence: the app updates last-seq counters.

## No Optimistic Board Mutation

The online board remains authoritative-read-only:

1. actions are submitted to the server;
2. the server accepts/rejects;
3. the client refreshes from snapshot/action events or an explicit snapshot request.

This keeps state hash, server sequence, and board cells aligned with the Linux-native authority.

## Limitations

- Accepted action events currently do not always include a fresh snapshot, so the safe Asgard action button still requests a snapshot after success.
- Gap detection and automatic resync policy are still future work.
- Arbitrary click-to-move still needs server-backed legal preview.
