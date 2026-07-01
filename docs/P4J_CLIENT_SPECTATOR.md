# P4J Phase 14 - Client Spectator Support

Date: 2026-07-01

## Scope

Phase 14 adds shared client support for the server-side spectator method. This keeps WPF code from constructing raw SignalR calls directly.

## ChessOnlineRelayClient

New client method:

- `JoinSpectatorAsync(clientId, OnlineJoinSpectatorRequest, cancellationToken)`

New tracked response:

- `LastSpectatorResult`

New registered callback:

- `ReceiveJoinSpectatorResult`

The client fills missing `playerId`, `roomId`, and `tableId` from the current session/request context before invoking the hub method.

## Spectator State

`OnlineSpectatorClientState` stores:

- `IsSpectator`;
- `SpectatorRoomId`;
- `SpectatorTableId`;
- `SpectatorRulesetId`;
- `SpectatorId`;
- `LastKnownServerSeq`;
- `SubmitDisabledReason`.

On a successful join it becomes active and carries the server's read-only reason. On a failed join it clears active spectator context and keeps the failure text as a readable disabled reason.

## Security

The client state does not store:

- access tokens;
- refresh tokens;
- passwords;
- authorization headers;
- private keys.

Temporary auth tokens remain inside the existing in-memory `ChessOnlineClientSession` flow.

## Verification

`ChessOnlineContractTests` verifies:

- `ReceiveJoinSpectatorResult` is registered;
- a new relay client starts with no spectator result and inactive spectator state;
- successful spectator state disables submit;
- failed spectator join clears active spectator state with readable reason.

Remote spectator smoke is deferred until the updated server package is deployed to Hetzner.
