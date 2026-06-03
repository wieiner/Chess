# Chess3D Online Room And Table State

`OnlineRoomRegistry` owns the local authoritative multiplayer state for P3E tests.

## Room

A room contains players and tables. Joining a room registers a `playerId`.

## Table

A table contains:

- `tableId`
- `rulesetId`
- table state: waiting, ready, playing, finished
- seats
- server sequence
- authoritative `OnlineGameSession`
- accepted action events

## Seats

Seat ownership is explicit. A player can claim an available side/macro-player seat. Duplicate seat claims are rejected.

## Starting

The table starts only after players are seated and ready enough for the profile contract. Start creates an authoritative engine session and emits a snapshot.

## Profile Isolation

Seat counts and actor interpretation come from the five-profile catalog. Hodge seats are macro-player seats; Classic, Single-Side, Asgard, and Rubik use side seats.
