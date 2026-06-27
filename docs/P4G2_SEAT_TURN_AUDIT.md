# P4G2 Seat And Turn Audit

Date: 2026-06-27

Scope: P4G2 Phase 08. This is a documentation-only audit of how online seat ownership and current turn state are represented today. It prepares Phase 09 UI indicators.

## Server Seat Model

`OnlineRoomRegistry.JoinTableSeat` assigns `OnlineSeat` records:

- `SeatIndex`
- `SideId`
- `MacroPlayer`
- `PlayerId`
- ready/connection timestamps

Mapping:

- Classic, Single-Side, Asgard, Rubik: `SeatIndex` maps to `SideId`.
- Hodge Projection Duel: `SeatIndex` maps to `MacroPlayer`, and `SideId` is `0`.

The profile catalog still contains exactly five Chess3D RuleProfiles.

## Matchmaking Payload

Matchmaking returns `OnlineMatchmakingStatus` with:

- `TicketId`
- `PlayerId`
- requested ruleset id;
- state;
- `RoomId`
- `TableId`
- `SeatIndex`
- ticket list.

`ReceiveMatchFound` gives each client enough information to know the room/table and assigned seat, but the current WPF UI mostly surfaces it as text rather than a persistent "my seat / my side" indicator.

## Ready / Start Flow

Current flow:

1. Player joins a table seat.
2. `Ready` sets `seat.IsReady`.
3. `StartGame` requires the caller's seat to be ready.
4. The server creates `OnlineGameSession`.
5. The returned snapshot includes authoritative ruleset, state hash, server sequence, game phase/outcome, and turn summary.

The current one-app test-pair flow drives both clients, but a two-window manual flow should show which window owns which seat.

## Current Turn Data

`OnlineSnapshot` exposes:

- `TurnSummary`
- `GamePhase`
- `GameOutcome`
- `StateHash`
- `SaveGameJson`

The savegame parsed by the online board adapter includes:

- current side;
- current macro-player;
- current turn kind.

This is enough for the UI to show:

- current side;
- current macro-player;
- whether the primary local player probably owns the turn actor.

## Authority Enforcement

The server enforces actor ownership in two places:

- `SubmitAction` calls `ActorMatchesSeat`.
- `RequestLegalPreview` also checks the requested actor against the caller seat.

For non-Hodge profiles:

- `ActorSide` or `Side` must match `seat.SideId`.

For Hodge:

- `MacroPlayer` or fallback `ActorSide` must match `seat.MacroPlayer`.

The UI can pre-disable actions when it knows the actor is wrong, but server validation remains authoritative.

## Current UI Gaps

The current `ChessOnlineApp` shows:

- match status text;
- snapshot turn summary text;
- board status with current side/macro-player indirectly through parsed board state;
- action accepted/rejected counters.

It does not yet show a stable compact panel for:

- my player id;
- opponent player id;
- my seat;
- my side or macro-player;
- current side;
- current macro-player;
- can act now yes/no;
- reason why submit is disabled.

## Phase 09 Recommendation

Add UI fields derived from existing data, without changing protocol semantics:

- `My player`
- `Opponent`
- `My seat`
- `My side/macro`
- `Current side`
- `Current macro-player`
- `Can act now`
- `Disabled reason`

For the one-app test-pair flow, the primary client is "player A" and should control the assigned primary seat. The secondary client is the passive/test opponent until Phase 10 two-window mode is hardened.

## Limitations

The audit does not add:

- spectator roles;
- reconnect/resume ownership;
- persisted per-window identity;
- full Hodge side-to-macro visual grouping.

Those remain later online UX phases.
