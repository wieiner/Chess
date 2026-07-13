# P4K WPF Lobby Smoke Result

Date: 2026-07-13

## Scope

An A/B `ChessOnlineApp` process created and started a fresh Asgard match on the
Hetzner HTTP 80 diagnostic deployment. A separate C process registered an
independent temporary user and used only the tracked Lobby UI to discover and
open that match.

The bounded UIA driver and all runtime identifiers/logs remain under ignored
`.tmp/manual-smoke`.

## Verified Flow

1. A/B created, readied, and started an Asgard match.
2. C selected the Asgard lobby filter and clicked `Refresh Lobby`.
3. C selected the exact newly created room/table row.
4. Displayed row and selection status were checked against token,
   authorization, password, connection-id, email, private-key, and keyring field
   names; no forbidden field was present.
5. `Use Selected For Spectator` copied the exact room/table into the spectator
   fields.
6. `Spectate Selected` succeeded and produced explicit read-only spectator state.
7. C requested the authoritative spectator snapshot.
8. `Resume Selected` from the unseated C identity was rejected safely with
   `playerNotInTable`.

## Result

Result: **PASS** in 13.49 seconds inside a 150-second watchdog.

- exact active row selected: yes;
- safe displayed row: yes;
- spectator join: yes;
- invalid player resume reason: `playerNotInTable`;
- state hash before/after invalid resume: `a0296f7e94a22346`;
- snapshot action count remained zero;
- no WPF process remained after cleanup.

## Observed UX Limitation

The lobby list is intentionally compact and WPF virtualizes rows outside its
viewport. With accumulated diagnostic matches, the newest lexicographically
sorted row may require keyboard or scrollbar traversal before it is visible.
The smoke exercised that user-visible navigation rather than bypassing the UI.
Lifecycle cleanup, recent-first ordering, or paging would improve this in later
P4K hardening phases.

## Security Boundary

Only temporary users were used. No token or generated password was displayed or
stored in tracked files. No server configuration, firewall, TLS/443, or neighbor
service was changed. HTTP 80 remains diagnostic/development only.
