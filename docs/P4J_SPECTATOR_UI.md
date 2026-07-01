# P4J Phase 15 - Spectator UI

Phase 15 adds a minimal spectator surface to `ChessOnlineApp`. It does not change the five Chess3D rule profiles and does not add a new game mode. Spectator is an online client role for observing an existing room/table.

## UI Entry Points

The Online tab now has:

- `Play Mode` option: `Spectator`;
- `Room` and `Table` text boxes for manual room/table IDs;
- `Join as Spectator`;
- `Request Snapshot`;
- `Request Action Log`;
- `Follow Last Move`;
- `Save Spectator Report`;
- a `SPECTATOR` status line.

If the operator has already joined or created a match in the same app, the spectator controls can reuse the current room/table IDs. Otherwise, paste IDs manually.

## Read-Only Behavior

When spectator mode is selected or a spectator join succeeds:

- `Ready Both` is disabled;
- `Ready This Window` is disabled;
- `Start Game` is disabled;
- `Start This Window` is disabled;
- safe test action submit is disabled;
- normal move submit is disabled;
- selected preview submit is disabled;
- `CanP4FPrimaryAct` returns `Spectator mode is read-only.`;
- board navigation, action history selection, layer navigation, snapshot refresh, and action-log refresh remain available.

The server remains the real authority. Spectator UI disablement is for clarity; mutating hub methods still require a seated player server-side.

## Snapshot And Action Log

`Join as Spectator` calls `JoinSpectator` through `ChessOnlineRelayClient`. On success the response can carry:

- an authoritative snapshot;
- an action-log tail;
- spectator state with room/table/ruleset/server sequence.

The standalone `Request Snapshot` and `Request Action Log` buttons use the same read-only hub paths available to connected table viewers.

`Follow Last Move` refreshes the action log and snapshot together, then updates the board from the latest authoritative server state. It is intentionally simple; no timeline scrubber is added in this phase.

## Reports

`Save Spectator Report` writes a sanitized JSON report under:

`.tmp/manual-smoke/p4j-spectator-report-YYYYMMDD-HHMMSS.json`

The report includes endpoint URLs, room/table IDs, ruleset, short spectator ID, last server sequence, snapshot hash, and action-log lines. It does not include access tokens, refresh tokens, passwords, Authorization headers, keyrings, stores, certificates, or private keys.

## Deployment Note

Local source now contains the spectator DTOs, server method, client method, and UI. A remote Hetzner server must be deployed with the Phase 13+ server package before `JoinSpectator` works remotely. Older deployments may reject the hub method as missing.

## Verification

Phase 15 verification:

- build `ChessOnlineApp`;
- run targeted online contract tests;
- confirm spectator mode disables submit controls in the UI code path;
- keep remote spectator smoke manual/operator-only, not a CI requirement.
