# P4K Remote Online UX User Guide

Date: 2026-07-13

The current public server is a diagnostic/development deployment over plain
HTTP 80. Use only generated temporary users. Never enter a real or reused
password.

## Build And Launch

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
.\src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe
```

Open the `3D Relay` tab before using the P4K controls.

## One-app Play

1. Click `Use Hetzner HTTP`.
2. Click `Check Health`, then `Check Diagnostics`.
3. Select one of the five profile IDs.
4. Click `Create Two Test Players`.
5. Click `Create Test Match With Two Local Clients`.
6. Click `Ready Both`, `Start Game`, and `Request Snapshot`.
7. Click an occupied source cell belonging to the current seat.
8. Click one of the highlighted server legal-preview targets.
9. Confirm accepted action, updated sequence/hash, board, and action log.
10. Click `Save Session Report` only when a local ignored report is useful.

Classic, Single-Side, Asgard, Rubik, and Hodge all start and provide snapshot
and preview. Rubik layer turns, Hodge projection composites, and Asgard
reserve/core actions keep their special action types; the generic board must not
send them as `NormalMove`.

## Two-window Play

In each window:

1. click `Use Hetzner HTTP` and check health;
2. create/authenticate a separate temporary player;
3. select `Manual Player` mode and the same profile;
4. click `Manual Join Matchmaking`;
5. click `Ready This Window`.

After both players receive the same match, one eligible window clicks
`Start This Window`. Both use `Snapshot This Window`. The current player clicks
an occupied source and highlighted legal target; the other window should receive
the realtime sequence update and refresh to the same authoritative hash.

## Disconnect, Reconnect, And Resume

From a retained active match:

1. click `Disconnect Primary Relay`;
2. confirm move submission is disabled;
3. click `Reconnect Primary Relay`;
4. click `Resume Current Match`;
5. request snapshot/action log and confirm seat, sequence, hash, and action count.

This proves transport reconnect plus explicit in-process match resume. It does
not promise resume after the server process itself restarts.

## Spectator

1. In a player window, note the displayed room and table.
2. In another window create an independent temporary user.
3. select `Spectator` mode;
4. enter the room/table and click `Join as Spectator`;
5. click `Request Snapshot` and request/follow the action log;
6. click `Follow Last Move` after a player action;
7. optionally click `Save Spectator Report` to an ignored `.tmp` location.

A spectator must show `SPECTATOR`/read-only state, have no player seat, and
keep ready/start/action-submission controls disabled.

## Lobby

1. Select the desired profile filter.
2. Click `Refresh Lobby`.
3. Select the intended room/table row.
4. Click `Use Selected For Spectator`, then `Spectate Selected`.
5. `Resume Selected` is valid only for the authenticated player who owns a
   retained seat; other users receive `playerNotInTable` without mutation.

The compact WPF list can virtualize off-screen rows. Use normal keyboard or
scrollbar navigation if the newest row is outside the current viewport.

## Network Bug Report

Click `Save Network Bug Report` after reproducing a problem. Save under
`.tmp/manual-smoke`. Review the JSON before sharing. The tracked report writer
is designed to omit access/refresh tokens, passwords, authorization headers,
private keys, and raw credentials; never attach an unsanitized journal or
runtime store.

Useful facts for a report are profile, user role, operation, safe reject reason,
server sequence, state hash, action count, reconnect/resume state, and app/server
build identity.

## Common Results

- `rateLimited`: wait for the fixed window; do not restart the server.
- `playerNotInTable`: the authenticated user does not own a seat in that table.
- disconnected submit disabled: expected; reconnect and explicitly resume.
- no special-action button: the online UI boundary for that profile is not yet complete.
- server unavailable: check live/ready/diagnostics; do not change 443 or neighboring services.

