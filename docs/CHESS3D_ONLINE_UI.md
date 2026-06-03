# Chess3D Online UI

P3E adds a small local authority panel to `ChessOnlineApp`.

## Controls

The `3D Relay` tab can:

- select one of the five Chess3D rulesets;
- create a local room;
- create a table;
- join a seat;
- mark ready and start;
- submit a coordinate normal move;
- request snapshot;
- request action log;
- request diagnostics.

## Status

The panel prints readable status messages with message type, ruleset, state hash, action count, reject reason, or diagnostic counters.

## Limits

This is not a full multiplayer lobby. It is a local authority exercise panel for protocol and packaging verification.

## P3F Hosted SignalR Panel

P3F adds a hosted transport section to the same `3D Relay` tab. The panel can connect to a locally running `ChessOnlineServer`, call `Hello`, create/join rooms and tables through hub methods, start a table, submit a move, request a snapshot/action log, and request diagnostics.

The default URL is:

```text
http://127.0.0.1:5077/chess3d/relay
```

The server must be started separately. The UI is a developer/control surface, not production matchmaking. Session tokens are used for reconnect tests and are not displayed as credentials.
