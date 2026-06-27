# P4F Playable Online Client Baseline

Date: 2026-06-27

This document records the starting point for P4F, the playable online client MVP over the existing diagnostic HTTP deployment.

## Repository baseline

- Branch: `main`.
- Start commit: `34d2e85b370873a47f469affbfbacc1ac2b1ab32`.
- Commit message: `NextEra: fix Hetzner SignalR smoke tooling`.
- `HEAD` equals `origin/main`.
- Working tree before Phase 00 docs: clean.

Latest CI before P4F:

- GitHub Actions run `28282611013`.
- Workflow: `Windows Build`.
- Result: success.
- Commit: `34d2e85b`.

## Smoke wrapper baseline

The Hetzner smoke wrapper exists and accepts the new public HTTP parameters:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -DryRun `
  -BaseUrl "http://178.105.220.117"
```

Dry-run result:

- command completed successfully;
- resolved hub URL: `http://178.105.220.117/chess3d/relay`;
- no network calls were made in dry-run mode.

## Public HTTP health

Current public HTTP endpoint:

```text
http://178.105.220.117
```

Read-only checks:

```powershell
curl.exe --connect-timeout 10 http://178.105.220.117/healthz/live
curl.exe --connect-timeout 10 http://178.105.220.117/healthz/ready
curl.exe --connect-timeout 10 http://178.105.220.117/chess3d/diagnostics
```

Observed result:

- `/healthz/live`: `Healthy`.
- `/healthz/ready`: ready JSON with `profileCount: 5`, `authEnabled: true`, `persistenceProvider: json`.
- `/chess3d/diagnostics`:
  - `authorityRuntimeKind`: `LinuxNativeFuture`;
  - `authorityIsSupported`: `true`;
  - `authorityPlatform`: `Linux`;
  - `authorityNativeLibraryName`: `libChess3DEngine.so`;
  - `authorityNativeLibraryPath`: `/opt/chessonline/server/libChess3DEngine.so`;
  - `authEnabled`: `true`;
  - `acceptedActionCount`: `1`;
  - `rejectedActionCount`: `0`;
  - `profileCount` is represented by the ready endpoint and remains exactly `5`.

## Deployment boundary

The current deployment boundary is intentionally narrow:

- ChessOnlineServer is reachable through public HTTP port 80.
- Nginx proxies HTTP 80 to Kestrel on `127.0.0.1:5077`.
- Linux native `libChess3DEngine.so` is loaded from `/opt/chessonline/server/libChess3DEngine.so`.
- TLS/domain/443 are deferred.
- Port 443 and any x-ui/Xray services are out of scope and must not be modified in P4F.
- Other Hetzner services such as Unreal UDP 7777, Outline, Albatronix Docker, and x-ui/Xray are read-only context for this phase.

## P4F next phase

Next phase:

```text
P4F phase 01: audit online client UI boundaries
```

The audit will decide where the playable manual client belongs: `ChessOnlineApp`, `Chess3DApp`, a shared client SDK layer, or a small dedicated tool. The current expectation is to improve `ChessOnlineApp` without a large WPF rewrite.
