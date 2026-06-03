# Chess3D SignalR Connection Identity

P3F uses development session identity for reconnect smoke tests.

## Identity Pieces

- `connectionId`: SignalR transport connection id.
- `sessionToken`: opaque local reconnect token returned by `Hello`.
- `playerId`: protocol actor id used by the authority registry.

## Reconnect

A reconnecting client can pass the session token to `Hello`. If the token is still known, the server associates the new connection with the previous player/room/table membership and lets the client request a fresh authoritative snapshot.

Invalid tokens are rejected cleanly.

## Limits

The session token is not production authentication. It is not an account credential, not a secret suitable for logging, and not a durable identity. P4 work must replace or wrap this with production identity/session persistence before public multiplayer.

