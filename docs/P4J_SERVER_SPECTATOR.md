# P4J Phase 13 - Server Spectator Mode

Date: 2026-07-01

## Scope

Phase 13 implements read-only spectator join on the server side. It does not change Chess3D rules, does not add a sixth profile, and does not modify deployment/network infrastructure.

## Hub Method

New hub method:

- `JoinSpectator`

On success the hub sends:

- `ReceiveJoinSpectatorResult`

The method uses the append-only DTOs from Phase 12:

- `OnlineJoinSpectatorRequest`
- `OnlineJoinSpectatorResult`
- `OnlineSpectatorState`

## Validation

The hosted hub still runs the normal envelope/auth validation path before calling the registry. For P4J, spectators are expected to be temporary authenticated users.

The registry validates:

- room/table exists;
- optional expected ruleset matches;
- table is in game;
- active native session exists.

## Read-Only Authority

`JoinSpectator` does not allocate an `OnlineSeat`.

This keeps existing authority checks intact:

- `Ready` requires a seat;
- `StartGame` requires a ready seat;
- `SubmitAction` requires a seat and actor ownership;
- `RequestLegalPreview` still requires a seat because it is tied to actionable player-side ownership.

Spectators may request:

- authoritative snapshot;
- action-log chunk.

Spectators may receive table-group broadcasts after joining the SignalR table group.

## Result Payload

Successful spectator join returns:

- `roomId`;
- `tableId`;
- `rulesetId`;
- `spectatorId`;
- read-only `state`;
- authoritative `snapshot`;
- action-log tail from `lastKnownServerSeq + 1`.

The same snapshot and action log are also placed on the top-level `OnlineProtocolMessage` fields for client compatibility.

## Diagnostics

`/chess3d/diagnostics` now reports:

- `spectatorMode=true`;
- `supportedHubMethods` includes `JoinSpectator`.

Remote Hetzner will not show these fields until a new server package is deployed. This phase only changes the repo and CI artifact.

## Tests

`ChessOnlineContractTests` verifies:

- diagnostics advertises spectator mode;
- active spectator join succeeds;
- spectator receives snapshot/action log;
- spectator join does not mutate state hash;
- spectator can request snapshot/action log;
- spectator `SubmitAction` is rejected because no seat is assigned;
- missing table fails with `tableNotFound`.

## Security

Spectator payloads do not contain:

- access tokens;
- refresh tokens;
- passwords;
- authorization headers;
- private keys;
- raw runtime stores.

HTTP 80 remains diagnostic/dev only.
