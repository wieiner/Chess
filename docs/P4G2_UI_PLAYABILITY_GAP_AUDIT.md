# P4G2 UI Playability Gap Audit

Date: 2026-06-27

Stage: P4G2 phase 01.

Scope: audit the current `ChessOnlineApp` click path after `P4G phase 05: add online click to move MVP`. This phase documents what still blocks a comfortable manual online match. No code or server behavior changes are made here.

## Current Working Path

The current UI can perform a technical online match smoke from one `ChessOnlineApp` instance:

1. choose the Hetzner HTTP diagnostic endpoint;
2. check health and diagnostics;
3. create two temporary users;
4. create two local SignalR clients;
5. join matchmaking for one of the five existing Chess3D RuleProfiles;
6. ready both clients;
7. start the game;
8. request an authoritative snapshot;
9. render an 8x8 layer slice from `OnlineSnapshot.SaveGameJson`;
10. click a source cell and target cell manually;
11. submit a `NormalMove`;
12. show accepted/rejected result;
13. refresh the authoritative snapshot after accepted action.

This is a real server-authoritative path, but it is still closer to an operator tool than a playable board game.

## What Is Uncomfortable Today

### Too Many Manual Steps

The user must explicitly press:

- `Create Two Test Players`;
- `Create Test Match With Two Local Clients`;
- `Ready Both`;
- `Start Game`;
- `Request Snapshot`;
- source cell;
- `Use Selected as From`;
- target cell;
- `Use Selected as To`;
- `Submit Normal Move`.

This is acceptable for a diagnostic smoke, but not for natural game play. The quickest improvement is to collapse the board path into:

1. click source;
2. see legal targets;
3. click target.

### No Online Legal Preview

The local Chess3D UI has legal action preview through the native engine, but the online UI does not have an authoritative server-backed preview endpoint yet.

Current behavior:

- the client chooses `From` and `To`;
- the client builds `OnlineActionCommand`;
- the server accepts or rejects;
- rejection text is shown after the attempted action.

Missing behavior:

- click source should request legal options for that exact cell;
- legal targets should be highlighted;
- unsupported mode-specific actions should be shown honestly;
- stale hash should trigger resync before action submit.

### No Clear "My Seat / My Side / My Turn"

The protocol and registry do track seats and actors:

- matchmaking assigns `SeatIndex`;
- non-Hodge profiles map `SeatIndex` to side;
- Hodge maps `SeatIndex` to macro-player;
- `OnlineRoomRegistry.ActorMatchesSeat(...)` rejects wrong actors;
- snapshots expose `TurnSummary`, `currentSide`, `currentMacroPlayer` through `SaveGameJson`.

The UI does not yet surface this clearly as:

- my player id;
- my seat;
- my side or macro-player;
- current side;
- can act now yes/no;
- why action is disabled.

This makes wrong-side and wrong-turn errors feel mysterious.

### Two-Window Play Is Not First-Class

One-app two-client test pair is useful and currently works. Two-window manual play needs better affordances:

- single-user temporary registration per window;
- join matchmaking as one player;
- show assigned room/table/seat;
- independent event logs per app instance;
- clear "waiting for opponent" state.

Currently, the one-app test pair hides some of the real two-player UX problems because both clients are controlled by the same process.

### Realtime Sync Is Minimal

`ChessOnlineRelayClient.MessageReceived` now surfaces SignalR callbacks, and the UI can redraw snapshot-bearing events. Still missing:

- duplicate event suppression;
- server sequence gap detection;
- automatic resync on stale/reconnect;
- connection state badge;
- reconnecting/resync required badge;
- robust action-log tail refresh after every accepted action.

### Special Actions Are Not Natural Yet

`Submit Normal Move` is intentionally narrow. For five-profile play:

- Classic: normal moves can become natural once legal preview exists.
- Single-Side: normal moves can use the same path.
- Asgard: normal moves work, but core/fusion/reserve overlays are not visible enough yet.
- Rubik: layer turns must remain separate actions, not fake normal moves.
- Hodge: projected composite moves need dedicated preview and mirror display.

The UI should avoid pretending that all special actions are already click-to-move.

## What The User Still Does Manually

- Picks the profile.
- Starts the test match manually.
- Requests snapshot manually.
- Chooses From/To manually.
- Infers current side from compact status text.
- Learns invalid moves by server rejection.
- Requests action log manually.
- Uses one-app pair instead of two independent windows.

## What Can Be Automated Safely

Near-term safe improvements:

- after selecting an occupied cell, request server-side legal preview;
- highlight returned targets;
- if a target has exactly one option, submit that option on click;
- refresh snapshot/action log after accepted action;
- show seat/side/turn labels from existing DTO/snapshot fields;
- show stale/resync error and call `RequestSnapshot`;
- preserve manual From/To as an advanced fallback.

Riskier or later improvements:

- full local `Chess3DWindow` embedding;
- optimistic local board mutation;
- Rubik/Hodge universal click dispatch without preview;
- two-window UX before seat/turn display is visible.

## Shortest Path To Actually Playable UI

The shortest safe path is:

1. add append-only legal preview DTOs;
2. add server hub method `RequestLegalPreview`;
3. add client SDK method;
4. click source -> request preview;
5. highlight legal targets;
6. click legal target -> submit exact preview option;
7. add seat/side/current-turn status;
8. improve two-window manual mode.

This preserves the server-authoritative model and avoids changing Chess3D rules or adding a sixth profile.

## Current File Notes

- `docs\P4G_ONLINE_BOARD_UI.md` was referenced by the phase prompt but does not exist in the repository at this baseline.
- `docs\P4G_ONLINE_CLICK_TO_MOVE_MVP.md` documents the current manual From/To MVP and its limitations.
- `docs\P4F_PLAYABLE_ONLINE_USER_GUIDE.md` documents the P4F operator path, not yet the full P4G2 legal-preview path.

## Security Boundary

The current UI remains diagnostic/dev only:

- public HTTP 80 only;
- temporary users only;
- no real passwords;
- no token/password logging;
- no runtime stores/keyrings/certs committed;
- no 443/TLS/x-ui/Xray/nginx/systemd/UFW/firewall changes.
