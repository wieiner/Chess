# Chess3D SignalR Hub Contract

Hub path:

```text
/chess3d/relay
```

## Hub Methods

- `Hello`
- `CreateRoom`
- `JoinRoom`
- `LeaveRoom`
- `ListRooms`
- `CreateTable`
- `JoinTableSeat`
- `LeaveTableSeat`
- `Ready`
- `StartGame`
- `SubmitAction`
- `RequestSnapshot`
- `RequestActionLog`
- `JoinMatchmaking`
- `CancelMatchmaking`
- `GetMatchmakingStatus`
- `ListMatchmakingQueues`
- `Ping`
- `Diagnostics`

Each method accepts an `OnlineProtocolMessage` and returns an `OnlineProtocolMessage`.

## Server Events

- `ReceiveWelcome`
- `ReceiveRoomCreated`
- `ReceiveRoomJoined`
- `ReceiveRoomLeft`
- `ReceiveRoomList`
- `ReceiveTableCreated`
- `ReceiveTableState`
- `ReceiveSeatAssigned`
- `ReceiveGameStarted`
- `ReceiveActionAccepted`
- `ReceiveActionRejected`
- `ReceiveAuthoritativeSnapshot`
- `ReceiveActionLogChunk`
- `ReceiveResyncRequired`
- `ReceiveMatchmakingStatus`
- `ReceiveMatchmakingCancelled`
- `ReceiveMatchFound`
- `ReceiveMatchmakingError`
- `ReceivePong`
- `ReceiveError`
- `ReceiveDiagnostics`

## Groups

SignalR group names are implementation details:

- `room:{roomId}`
- `table:{tableId}`

Groups are used only after the registry accepts membership. They are not authorization and are not durable state.

## Result Semantics

- Accepted action: broadcast `ReceiveActionAccepted` to the table group.
- Rejected action: send `ReceiveActionRejected` to the caller.
- Stale hash: send `ReceiveResyncRequired` with an authoritative snapshot.
- Matchmaking join/status/cancel: send or return matchmaking status messages.
- Match found: create the authoritative room/table/seats and send `ReceiveMatchFound`.
- Malformed or unsupported message: return/send `ReceiveError`.
