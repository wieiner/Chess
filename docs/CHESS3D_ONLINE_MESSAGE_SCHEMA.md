# Chess3D Online Message Schema

The schema descriptor is stored at:

`assets/rules/online/schemas/chess3d_relay_v0_1.schema.json`

## Envelope

Each message uses:

- `protocolId`
- `protocolVersion`
- `messageId`
- `messageType`
- `correlationId`
- `roomId`
- `tableId`
- `playerId`
- `sessionToken`
- `sentUtc`
- `payload`

Required fields are checked by `OnlineProtocolJson.ValidateEnvelope`.

## Message Types

Supported message families include:

- hello / welcome
- createRoom / joinRoom / roomList
- createTable / joinTableSeat / ready / startGame
- submitAction / actionAccepted / actionRejected
- snapshotRequest / snapshot
- actionLogRequest / actionLogChunk
- diagnosticsRequest / diagnostics
- resyncRequired / error

P3F adds the optional `sessionToken` envelope field for local hosted reconnect smoke tests. It is a development session token, not production authentication.

Unknown future properties in known DTOs are ignored by `System.Text.Json`, but unknown `messageType` values are rejected.

## Versioning

P3E/P3F only accept `protocolVersion = 0.1`. Future versions should be additive or negotiated before dispatch.
