# P4G2 Seat And Turn UI

Date: 2026-06-27

Scope: P4G2 Phase 09. This phase adds visible online seat/turn indicators to `ChessOnlineApp` and pre-submit UI gating for the primary local player. It does not change server authority, Chess3D rules, native ABI, or the five existing RuleProfiles.

## What The UI Shows

The online tab now has a compact turn line under match status:

- primary local player id, redacted to a short id;
- passive/test opponent id, redacted to a short id;
- primary seat;
- opponent seat;
- primary actor as side or Hodge macro-player;
- current authoritative side or macro-player from the latest snapshot;
- current turn kind;
- `canAct=yes/no`;
- disabled reason when action submission is locally blocked.

For Classic, Single-Side, Asgard, and Rubik, seat index maps to side id. For Hodge Projection Duel, seat index maps to macro-player and side id stays `0`.

## Where The Data Comes From

`OnlineMatchmakingStatus.Tickets` provides player-to-seat assignments after `MatchFound`. `OnlineSnapshot.SaveGameJson` is parsed into `OnlineChess3DBoardSnapshot`, which exposes:

- `CurrentSide`;
- `CurrentMacroPlayer`;
- `CurrentTurnKind`;
- state hash and action count.

The helper `OnlineSeatTurnState` combines those two sources into a UI-friendly state object.

## Submit Gating

The UI now checks `OnlineSeatTurnState.CanPrimaryAct` before:

- `Submit Safe Asgard Test Action`;
- manual `Submit Normal Move`;
- one-click `Submit Selected Preview Action`.

If the primary local player does not own the current side/macro-player, the UI shows a readable reason such as:

- `waiting for side 2`;
- `waiting for macro-player 2`;
- `no authoritative snapshot`;
- `no primary seat assigned`.

This is a convenience guard only. The server still validates seat ownership, actor side/macro-player, state hash, and legal action shape.

## Testing

`ChessOnlineContractTests` covers the pure client helper for:

- disconnected / no online match;
- assigned seat without snapshot;
- Classic primary turn;
- Classic opponent turn;
- Hodge primary macro turn;
- Hodge opponent macro turn;
- deriving primary/opponent seats from `OnlineMatchmakingStatus.Tickets`.

WPF behavior is verified by building `ChessOnlineApp`; remote Hetzner smoke remains manual/operator-run and is not a CI requirement.

## Limits

This phase does not add:

- two-window identity persistence;
- spectator/reconnect ownership;
- full Hodge projection group visualization;
- automatic passive opponent move generation.

Those remain later online UX phases.
