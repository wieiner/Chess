# P4G2 Current Online Playability Baseline

Date: 2026-06-27

Stage: P4G continuation / P4G-2, phase 00.

## Baseline

- Branch: `main`
- Start commit: `abb7c5d1b04a1886668fcd97f1258a4f96fdc678`
- `origin/main`: `abb7c5d1b04a1886668fcd97f1258a4f96fdc678`
- Previous GitHub Actions: `28290007196`, Windows Build success for `P4G phase 05: add online click to move MVP`
- Working tree before phase: clean

## Public HTTP Server Check

Hetzner public HTTP 80 remains the diagnostic/dev deployment boundary.

- `curl.exe http://178.105.220.117/healthz/live`: `Healthy`
- `curl.exe http://178.105.220.117/healthz/ready`: ready JSON with `profileCount=5`, `authEnabled=true`, `persistenceProvider=json`
- `curl.exe http://178.105.220.117/chess3d/diagnostics`: Linux native authority supported, `authorityPlatform=Linux`, `authorityNativeLibraryName=libChess3DEngine.so`, `authEnabled=true`

No 443/TLS/x-ui/Xray/nginx/systemd/UFW/firewall changes were made.

## Remote Smoke

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 -BaseUrl "http://178.105.220.117" -ProfileId "asgard-convergence-3d-8x8x8-v0.1" -TimeoutSeconds 180 -NoSecretLog
```

Result: PASS.

Important observed steps:

- health PASS;
- temporary register/login PASS;
- SignalR connect PASS;
- Asgard matchmaking PASS with room/table assignment;
- game start PASS;
- action PASS with notation `#1 S1 MOVE P (2,3,0)->(2,3,1)`;
- snapshot/action log PASS with final hash `1116b19374131cc4`.

The smoke logs are under ignored `.tmp\test-logs`; no tokens/passwords are committed or printed in this document.

## UI Build

Command:

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
```

Result: PASS with 0 warnings and 0 errors.

## Can A User Already Play?

Short answer: technically yes for a narrow diagnostic path, but not yet comfortably like a normal online board game.

What works today:

- open `ChessOnlineApp`;
- select Hetzner HTTP diagnostic endpoint;
- check health/diagnostics;
- create temporary test users;
- create a two-client test match inside one app instance;
- ready/start the match;
- request a snapshot;
- see a rendered 8x8 layer slice from the authoritative server snapshot;
- select source/target cells manually;
- submit a `NormalMove`;
- see accepted/rejected result;
- refresh authoritative snapshot after accepted action;
- see action log/counters.

What "technically playable" means:

- the server remains authoritative;
- actions are sent through SignalR to the live Hetzner server;
- the Linux-native authority accepts/rejects actions;
- the client renders server snapshots instead of mutating a private local board.

What is still awkward:

- no server-backed legal target preview in the UI;
- no automatic "click source -> legal targets -> click target" flow;
- no clear "my seat / my side / my turn" indicator;
- the one-app test-pair flow is easier than real two-window play;
- special actions for Rubik/Hodge/reserve remain separate or deferred;
- rejected/stale/resync states need stronger user-facing handling.

## What P4G2 Must Add

The next practical steps are:

1. audit and add server-side legal preview;
2. add client SDK method for preview;
3. show legal target highlights;
4. submit exactly matched preview actions;
5. show seat/side/turn state;
6. support two-window manual play;
7. harden realtime resync;
8. document Classic and Asgard manual play paths.

HTTP 80 remains diagnostic/dev only. Use temporary users only.
