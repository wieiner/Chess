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

This is not a full multiplayer lobby. It is a local authority exercise panel for protocol and packaging verification. Hosted transport and polished multiplayer UX are future work.
