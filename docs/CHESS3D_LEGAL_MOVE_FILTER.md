# Chess3D Legal Move Filter

The P3A filter sits between pseudo-legal generation and public legal-action consumers.

## Inputs

- active `Game`;
- scoped `Position`;
- pseudo-legal `Move` records from the existing generator.

## Rejections

For Classic/Single-Side profiles with a side king present:

- direct king capture;
- self-check after applying the move to a temporary position;
- king move into an attacked square.

The temporary position is discarded after the check, so preview, perft, divide, and failed moves do not mutate board state, stacks, reserve, action history, replay cursor, or state hash.

## Public Consumers

The filtered legal list is used by:

- legal preview;
- `TryMakeMove`;
- `GetLegalMoves`;
- `MakeBestMove`;
- side legal-action counts;
- Classic/Single outcome checks;
- action perft/divide for Classic/Single.

Non-classic profiles keep their profile-specific action semantics.
