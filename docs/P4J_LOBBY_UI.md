# P4J Phase 20 - Lobby UI

Date: 2026-07-01

## Summary

Phase 20 adds a compact lobby panel to `ChessOnlineApp`.

It does not change Chess3D rules, does not add a sixth RuleProfile, and does not touch Hetzner networking, TLS, nginx, systemd, UFW, x-ui/Xray, Outline, Albatronix, or Unreal.

## Controls

The Online tab now includes:

- ruleset filter with exactly the five Chess3D profiles plus `All`;
- `Refresh Lobby`;
- active table list;
- selected table status/details;
- `Use Selected For Spectator`;
- `Spectate Selected`;
- `Resume Selected`;
- `Join Player Selected`.

## Behavior

`Refresh Lobby`:

- ensures a temporary authenticated primary session;
- connects SignalR if needed;
- calls `RequestLobbySnapshot`;
- displays safe `OnlineLobbyTableDisplayRow.DisplayLabel` values.

`Use Selected For Spectator`:

- copies selected room/table IDs into the spectator fields.

`Spectate Selected`:

- switches play mode to `Spectator`;
- copies room/table IDs;
- invokes the existing spectator join flow.

`Resume Selected`:

- populates current room/table context;
- invokes the existing `Resume Current Match` path.

`Join Player Selected`:

- currently reports a clear limitation. Direct seat join from lobby is not yet wired through the shared client SDK; use matchmaking or existing manual player flow.

## Privacy

Lobby UI displays:

- room/table IDs;
- ruleset;
- table state;
- seats occupied/max;
- spectator count;
- last server sequence;
- short seat summaries.

It does not display access tokens, refresh tokens, passwords, Authorization headers, private keys, keyrings, raw stores, or SignalR connection IDs.

## Deployment Note

Remote Hetzner lobby UI requires a server package that includes Phase 18 `RequestLobbySnapshot`. Older deployments will reject the hub method.

## Verification

Phase 20 verification:

- build `ChessOnlineApp`;
- run targeted online contract tests;
- confirm lobby rows are built by the client SDK without secrets;
- keep remote lobby smoke manual/operator-only.
