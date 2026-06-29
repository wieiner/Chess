# P4J Client Resume Support

Date: 2026-06-29

## Scope

P4J Phase 09 adds client-side support for the active-match resume method introduced in Phase 08. This is intentionally narrower than full persisted resume after a server restart:

- supported: resume an active in-memory room/table after a client disconnects or reconnects;
- supported: request authoritative snapshot and action-log tail from the server;
- supported: show the resume result in `ChessOnlineApp`;
- deferred: rehydrate native game sessions from the persistent store after `chessonline.service` restarts.

No new Chess3D rule profiles are added. The online client still uses the same five profile IDs.

## Client SDK

`ChessOnlineRelayClient` now exposes:

- `RequestResumeMatchAsync(clientId, OnlineResumeRequest, cancellationToken)`;
- `LastResumeResult`, populated when a `ResumeMatchResult` message is received;
- `ReceiveResumeMatchResult` in the shared SignalR event registration list.

The request carries only non-secret context:

- `playerId`;
- `roomId`;
- `tableId`;
- `seatIndex`;
- `expectedRulesetId`;
- `lastKnownStateHash`;
- `lastKnownServerSeq`.

It does not carry access tokens, refresh tokens, temporary passwords, or local key material.

## ChessOnlineApp UI

The online play panel now has a `Resume Current Match` operator button next to the reconnect controls. The button:

1. validates that the primary player has an authenticated in-memory session;
2. validates that room/table context is available;
3. sends `RequestResumeMatch`;
4. on success, renders the returned authoritative snapshot and action log;
5. on failure, shows the server failure reason without mutating local board state.

The sanitized session report includes a `resume` block with room/table/seat/hash/seq and the last resume result. It intentionally does not include tokens or passwords.

## Expected Results

For an active in-memory match:

- `Resume Current Match` should show `Resume succeeded`;
- the board snapshot should refresh from the server;
- the action log should show the returned tail from `lastKnownServerSeq + 1`.

For a missing table, wrong player, server restart without rehydration, or stale context:

- the UI should show `Resume rejected`;
- the reason should come from `OnlineResumeFailureReasons`;
- no action should be submitted as part of resume.

## Verification

Phase 09 verification:

- build `ChessOnlineClient`;
- build `ChessOnlineApp`;
- run `ChessOnlineContractTests`;
- confirm the relay client registers `ReceiveResumeMatchResult`;
- confirm resume DTOs remain token-free.

Manual remote smoke remains operator-driven and is not a GitHub Actions requirement.
