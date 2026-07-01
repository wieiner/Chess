# P4J Phase 12 - Spectator DTOs

Date: 2026-07-01

## Scope

Phase 12 adds append-only protocol payloads for spectator mode. It does not yet implement the server `JoinSpectator` hub method and does not change table authority rules.

## New Message Types

- `JoinSpectator`
- `JoinSpectatorResult`

Both are added to the known protocol message registry so old messages continue to parse and new clients can prepare requests.

## Request

`OnlineJoinSpectatorRequest` contains:

- `playerId`
- `roomId`
- `tableId`
- optional `expectedRulesetId`
- optional `lastKnownServerSeq`

It does not contain access tokens, refresh tokens, passwords, authorization headers, private keys, or connection ids.

## Result

`OnlineJoinSpectatorResult` contains:

- `success`
- `failureReason`
- `failureText`
- `roomId`
- `tableId`
- `rulesetId`
- `spectatorId`
- `state`
- optional authoritative `snapshot`
- optional `actionLog`

`OnlineSpectatorState` carries the read-only UI state:

- `isSpectator`
- `roomId`
- `tableId`
- `rulesetId`
- `spectatorId`
- `viewerPlayerId`
- `lastKnownServerSeq`
- `submitDisabledReason`

The default submit disabled reason is `Spectator mode is read-only.`

## Failure Reasons

`OnlineSpectatorFailureReasons` currently defines:

- `none`
- `notAuthenticated`
- `roomNotFound`
- `tableNotFound`
- `rulesetMismatch`
- `tableNotActive`
- `unsupported`

## Diagnostics

`OnlineDiagnostics` now has:

- `SpectatorModeSupported`

The value remains `false` in Phase 12, and `SupportedHubMethods` does not list `JoinSpectator` yet. Phase 13 is responsible for flipping the capability when the server method exists.

## Verification

Contract tests cover:

- spectator request JSON roundtrip;
- spectator failure/read-only result JSON roundtrip;
- no token/password/Authorization strings in serialized spectator payloads;
- diagnostics does not advertise spectator mode before server implementation.

## Deployment Boundary

This phase does not require remote deployment. Hetzner HTTP 80 remains diagnostic/dev only, and the currently deployed server will not support spectator mode until a future server package is deployed.
