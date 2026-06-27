# P4F Online Snapshot Viewer

Date: 2026-06-27

`ChessOnlineApp` now has a compact online snapshot/action-log viewer in the P3F/P4F hosted SignalR area.

## Visible State

The UI shows:

- selected ruleset id;
- room id;
- table id;
- latest snapshot ruleset;
- server sequence;
- turn summary;
- action count;
- state hash;
- accepted/rejected local action counts;
- last known server sequence;
- action-log list items.

This is not the full realtime Chess3D visual board. It is the practical P4F operator view needed to prove the online server can be touched by hand.

## Safe Asgard Action

The new `Submit Safe Asgard Test Action` button sends the same kind of action used by the remote smoke:

```text
NormalMove
actorSide = 1
from = (2,3,0)
to = (2,3,1)
expectedStateHashBefore = latest snapshot hash
```

It is intentionally limited to:

```text
asgard-convergence-3d-8x8x8-v0.1
```

For other profiles, the UI reports that the safe test action is not defined. No new mode or rule is added.

## Session Report

`Save Session Report` writes a sanitized JSON report under:

```text
.tmp/manual-smoke
```

The report includes:

- base URL;
- hub URL;
- ruleset id;
- room/table ids;
- short player ids;
- snapshot summary;
- accepted/rejected counts;
- last server sequence;
- action-log display lines.

The report does not include:

- access tokens;
- refresh tokens;
- passwords;
- runtime store/keyring content;
- certs;
- private keys.

`.tmp` is ignored runtime output and must not be committed.

## Current Limitations

- The viewer is a compact inspector, not the full 3D board.
- Only one safe Asgard action helper is wired in P4F.
- Rich realtime board integration is a P4G task.
- Reconnect/resume/spectator flows are future P4H work.

## Verification

Local verification:

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
```

Manual remote verification is recorded separately in the P4F UI smoke result.

