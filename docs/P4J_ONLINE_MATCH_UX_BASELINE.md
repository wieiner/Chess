# P4J Online Match UX Baseline

Date: 2026-06-28

## Repository State

- Branch: `main`
- Start commit: `9fa27197f6a8d849a48c97d76b8ed8b1ba36eac2`
- Start commit message: `P4I phase 05: highlight selected online action`
- `HEAD` and `origin/main`: matched at baseline
- Working tree: clean before P4J Phase 00 documentation changes
- Latest known GitHub Actions before P4J: `28312178896`, success

## Hetzner HTTP 80 Boundary

P4J continues to use the existing diagnostic/dev deployment:

- Public base URL: `http://178.105.220.117`
- Public HTTP health: `Healthy`
- Ready response: `status=ready`, `profileCount=5`, `authEnabled=true`, `persistenceProvider=json`
- Diagnostics: `requestLegalPreview=true`, `realtimeResync=true`, `actionLog=true`, `matchmaking=true`
- Native authority: `authorityPlatform=Linux`, `authorityIsSupported=true`, `authorityNativeLibraryName=libChess3DEngine.so`

The following remain explicitly out of scope:

- `443`;
- TLS/domain setup;
- x-ui/Xray;
- Outline;
- Albatronix Docker;
- Unreal SYServer;
- nginx/UFW/firewall changes.

## Playable State

The deployed server and current client already support real online play for normal legal-preview actions:

- Classic Six-Side: remote smoke passed through `action-source=server-preview`; accepted notation `#1 S1 MOVE K (4,4,0)->(3,5,1)`.
- Asgard Convergence: remote smoke passed through `action-source=server-preview`; accepted notation `#1 S1 MOVE R (2,2,0)->(1,2,0)`.
- Single-Side Training: previously verified as one-player startup/snapshot coverage.
- Rubik Convergence: previously verified startup/snapshot coverage; layer-turn online UX remains a dedicated boundary.
- Hodge Projection Duel: previously verified startup/snapshot coverage; projection online UX remains a dedicated boundary.

## P4I Improvements Already Present

- readable compact online board grid;
- X/Y coordinate headers and Z-layer selector;
- legal/capture/special markers;
- selected source/target markers;
- legal-target layer navigation;
- selected action-history from/to board highlight;
- action history copy/export;
- automatic action-log refresh after accepted actions.

## P4J Gaps

P4J starts from a playable but still operator-style client. Missing product UX areas:

- reconnect state and automatic resync after reconnect;
- resume current match after disconnect/app restart boundaries;
- spectator/read-only join;
- lobby/table list;
- clearer match lifecycle state;
- opponent status;
- stronger UI guards during reconnect/disconnect;
- richer network bug reports.

## Phase 00 Verification

```powershell
git status --short
git branch --show-current
git log --oneline --decorate -25
git rev-parse HEAD
git rev-parse origin/main
gh run list --limit 10
curl.exe http://178.105.220.117/healthz/live
curl.exe http://178.105.220.117/healthz/ready
curl.exe http://178.105.220.117/chess3d/diagnostics
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 -BaseUrl "http://178.105.220.117" -ProfileId "asgard-convergence-3d-8x8x8-v0.1" -TimeoutSeconds 180 -NoSecretLog
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 -BaseUrl "http://178.105.220.117" -ProfileId "classic-six-side-3d-8x8x8-v0.1" -TimeoutSeconds 180 -NoSecretLog
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
```

Result: all Phase 00 baseline checks passed.
