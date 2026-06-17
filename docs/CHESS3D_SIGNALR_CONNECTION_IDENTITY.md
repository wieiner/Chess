# Chess3D SignalR Connection Identity

P3F uses development session identity for reconnect smoke tests. P4A adds optional authenticated session identity for production-oriented local runs.

## Identity Pieces

- `connectionId`: SignalR transport connection id.
- `sessionToken`: opaque local reconnect token returned by `Hello`.
- `playerId`: protocol actor id used by the authority registry.
- `accessToken`: P4A Data Protection protected bearer token used by SignalR clients when authentication is enabled.
- `refreshToken`: P4A protected token whose hash is stored in the durable session record.

## Reconnect

A reconnecting client can pass the session token to `Hello`. If the token is still known, the server associates the new connection with the previous player/room/table membership and lets the client request a fresh authoritative snapshot.

Invalid tokens are rejected cleanly.

## Limits

The development session token is not production authentication. In P4A authenticated mode, the hub derives `playerId` from the protected bearer session and rejects envelopes that claim a different player. This still does not make the server a public production deployment: OAuth, cloud hosting, matchmaking, and internet hardening remain future work.
