# P4G Realtime Online Board Baseline

Date: 2026-06-27

## Start Point

- Branch: `main`.
- Start commit: `7bbe1acc6c69eee35e3f8ca22dcfd0c2aec0fdca`.
- `origin/main`: `7bbe1acc6c69eee35e3f8ca22dcfd0c2aec0fdca`.
- Last completed stage: P4F playable online client MVP.
- Latest known CI before P4G: GitHub Actions `28288227570`, success.

## Local State

The working tree was clean before P4G Phase 00 edits.

P4F added:

- `src/ChessOnlineClient`;
- health/ready/diagnostics client;
- auth client;
- in-memory session;
- SignalR relay client;
- redacted event log;
- `ChessOnlineApp` connection, auth/test-user, matchmaking, snapshot/action-log, and safe Asgard action controls.

## Public HTTP 80 Health

Read-only public HTTP checks against the current Hetzner diagnostic deployment passed:

```text
GET /healthz/live -> Healthy
GET /healthz/ready -> ready JSON with profileCount=5 and authEnabled=true
GET /chess3d/diagnostics -> Linux native authority supported
```

Diagnostics reported:

- `authorityRuntimeKind = LinuxNativeFuture`;
- `authorityPlatform = Linux`;
- `authorityNativeLibraryName = libChess3DEngine.so`;
- `authorityNativeLibraryPath = /opt/chessonline/server/libChess3DEngine.so`;
- `authorityIsSupported = true`;
- `profileCount = 5` through readiness;
- `authEnabled = true`.

## P4F Smoke Result

The current public HTTP smoke command passed:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://<HETZNER_HOST>" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -TimeoutSeconds 180 `
  -NoSecretLog
```

Result summary:

```text
STEP PASS health
STEP PASS register
STEP PASS login
STEP PASS SignalR connect
STEP PASS matchmaking room=match-4-asgard table=table-4
STEP PASS Asgard start hash=a0296f7e94a22346
STEP PASS Asgard action notation=#1 S1 MOVE P (2,3,0)->(2,3,1)
STEP PASS snapshot/actionlog finalHash=1116b19374131cc4
SMOKE PASS
```

The smoke used temporary users and did not print tokens or passwords.

## Current Limitation

P4F made the online client practical, but it is still not a real online board:

- snapshot/action-log UI exists;
- safe Asgard action helper exists;
- the user cannot yet click cells on an online board to request legal targets;
- online legal preview is not yet displayed as target highlights;
- click-to-move dispatch is not yet connected to exact server preview actions;
- realtime server events are not yet projected into a board sync model.

## P4G Target

P4G targets a playable online board over diagnostic HTTP 80:

- visible logical Chess3D board in `ChessOnlineApp`;
- source-cell selection;
- legal target preview;
- exact server action dispatch;
- snapshot/action-log sync;
- action accepted/rejected visibility;
- two-test-player and two-window manual play paths;
- sanitized bug-repro reports.

## Deployment Boundary

P4G does not touch:

- port `443`;
- TLS/domain setup;
- x-ui/Xray;
- Outline;
- Albatronix Docker;
- Unreal SYServer on UDP 7777;
- Nginx/systemd/UFW server configuration.

HTTP 80 remains diagnostic/dev only. Do not use real accounts or passwords.

