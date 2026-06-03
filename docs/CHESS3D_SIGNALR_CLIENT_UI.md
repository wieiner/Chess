# Chess3D SignalR Client UI

P3F adds a hosted transport panel to the `3D Relay` tab in `ChessOnlineApp`.

## Controls

- Server URL.
- Connect / Disconnect.
- Hello.
- Hub Create Room.
- Hub Create Table.
- Hub Join Seat.
- Hub Ready + Start.
- Hub Submit Move.
- Hub Snapshot.
- Hub Action Log.
- Hub Diagnostics.

The panel reuses the existing P3E room/table/player/seat/profile/action fields where practical.

## Status

The status area prints message type, accepted/rejected result, state hash, action counts, diagnostics, or readable errors. Session tokens are not printed.

## Scope

This is not a public matchmaking UI. It is a local developer/control panel for validating the hosted transport and registry authority path.

