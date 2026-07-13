# P4K Remote Spectator Smoke Result

Date: 2026-07-13

## Result

Read-only spectator mode passed against the public Hetzner HTTP 80 deployment for Asgard and Classic.

The tested server advertised `spectatorMode=true` and `JoinSpectator`; both bounded runs ended with `SMOKE PASS`. No server deployment or network/service configuration changed.

## Asgard

Final seat-proof run:

- run ID: `p4k-phase18-asgard-spectator-seat-proof-20260713`;
- table: `match-7-asgard` / `table-7`;
- state at spectator join: `e8443902f01a9450`, seq `1`;
- lobby `SeatsOccupied` and `MaxSeats` were identical before and after spectator C joined;
- spectator snapshot and action-log tail matched the authoritative table;
- spectator `Ready`, `StartGame`, and `SubmitAction` were rejected;
- those rejected calls left the state hash unchanged;
- player B then submitted a legal server-preview move;
- spectator C received `ReceiveActionAccepted` and refreshed to hash `9f9bb519247e6186`, seq `2`;
- result: PASS, smoke duration approximately 3.30 seconds.

## Classic

- run ID: `p4k-phase18-classic-spectator-20260713`;
- table: `match-6-classic` / `table-6`;
- state at spectator join: `679085fef5801b2a`;
- spectator joined read-only and all three mutation attempts were rejected;
- player B submitted a second legal server-preview move;
- spectator received the accepted-action broadcast and refreshed to hash `658df4a5d5b5e1b9`, seq `2`;
- result: PASS, smoke duration approximately 2.30 seconds.

The explicit no-seat lobby counter assertion was added after the first Asgard/Classic runs and repeated successfully for Asgard. The underlying no-seat authority was also demonstrated in both profiles by the rejected seat/action mutations.

## Privacy and lifecycle boundary

- generated spectator credentials remained in memory;
- stdout used shortened spectator/player identifiers only;
- no token, password, Authorization header, connection token, or private key was printed;
- raw logs remain under ignored `.tmp/remote-ux-smoke/`;
- the current lobby `spectatorCount` is still documented by the server as best-effort/placeholder until durable spectator tracking is implemented in later P4K lifecycle phases.

This phase proves live in-memory spectator membership and read-only authority. It does not yet prove disconnect decrement, duplicate-join idempotence, or restart persistence.
