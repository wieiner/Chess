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
- Malformed or unsupported message: return/send `ReceiveError`.

