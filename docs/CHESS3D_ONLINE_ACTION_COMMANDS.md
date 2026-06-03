# Chess3D Online Action Commands

`OnlineActionCommand` represents a client request to perform one authoritative Chess3D action.

## Action Kinds

- `normalMove`
- `rubikLayerTurn`
- `hodgeProjectedMove`
- `reserveRestore`
- `aiActionRequest`

`aiActionRequest` is defined as a future/diagnostic command and is not production remote AI.

## Validation

The authority validates:

- room and table existence;
- table state;
- actor player and seat;
- actor side or macro-player;
- optional expected state hash;
- profile capability;
- engine legality.

Accepted actions are applied through existing public runtime paths:

- normal move: `TryMakeMove`
- Hodge projected move: `TryMakeProjectedMove`
- Rubik layer turn: `RotateLayer`
- reserve restore: `RestoreReservePiece`

Failed commands do not append action history or advance `serverSeq`.

## Reject Reasons

Reasons include `invalidRoom`, `invalidTable`, `invalidSeat`, `wrongActor`, `gameNotStarted`, `staleStateHash`, `unsupportedAction`, `illegalAction`, `invalidPayload`, and `internalError`.
