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

Unknown future properties in known DTOs are ignored by `System.Text.Json`, but unknown `messageType` values are rejected.

## Versioning

P3E only accepts `protocolVersion = 0.1`. Future versions should be additive or negotiated before dispatch.
