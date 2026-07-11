# P4K Hetzner Deploy Result

Date: 2026-07-11

## Scope

Phase 13 completed the immediate post-deploy health and capability gate for the P4K server package deployed in Phase 12. This phase did not change the server; it only verified loopback, nginx-local, public HTTP, service state, diagnostics, journal risk scan, and neighboring ports.

## Deployed Package

- Deployed commit: `f33240e87cd39ed6d2cfb7b612a8504c28f85586`
- Package id: `chessonline-linux-x64-f33240e87cd3`
- Package archive SHA-256: `2868635C362DA78BFA2CDD2796AB31EFE7CBEE610D277D7DB2DB192539CE8A1D`
- Rollback backup: `/opt/chessonline/backups/server-before-p4k-20260711-184042.tar.gz`
- Previous server directory: `/opt/chessonline/server.prev.20260711-191019`
- Rollback needed: no

## Health Checks

Loopback Kestrel:

- `http://127.0.0.1:5077/healthz/live`: `Healthy`
- `http://127.0.0.1:5077/healthz/ready`: ready JSON with `profileCount=5`
- `http://127.0.0.1:5077/chess3d/diagnostics`: PASS

Local nginx:

- `http://127.0.0.1/healthz/live`: `Healthy`
- `http://127.0.0.1/healthz/ready`: ready JSON with `profileCount=5`
- `http://127.0.0.1/chess3d/diagnostics`: PASS

Public HTTP:

- `http://178.105.220.117/healthz/live`: `Healthy`
- `http://178.105.220.117/healthz/ready`: ready JSON with `profileCount=5`
- `http://178.105.220.117/chess3d/diagnostics`: PASS

## Diagnostics Assertions

Diagnostics now report:

- `serverCommit`: `f33240e87cd39ed6d2cfb7b612a8504c28f85586`
- `build.commit`: `f33240e87cd39ed6d2cfb7b612a8504c28f85586`
- `build.packageId`: `chessonline-linux-x64-f33240e87cd3`
- `profileCount`: `5`
- `authEnabled`: `true`
- `authorityIsSupported`: `true`
- `authorityPlatform`: `Linux`
- `authorityNativeLibraryName`: `libChess3DEngine.so`
- `requestLegalPreview`: `true`
- `realtimeResync`: `true`
- `actionLog`: `true`
- `matchmaking`: `true`
- `resumeMatch`: `true`
- `spectatorMode`: `true`
- `lobbySnapshot`: `true`

`supportedHubMethods` contains:

- `RequestResumeMatch`
- `JoinSpectator`
- `RequestLobbySnapshot`
- `RequestLegalPreview`

## Service State

- `chessonline.service`: `active`
- Main PID: `3457320`
- Working directory: `/opt/chessonline/server`
- User/group: `chessonline` / `chessonline`
- Server `startedUtc`: `2026-07-11T19:10:22.0861339Z`

## Journal Check

Command:

```bash
journalctl -u chessonline.service --since "-10 minutes" --no-pager -n 300 |
  grep -Ei 'crash|native load failure|persistence failure|permission denied|duplicate sequence|unhandled exception|fail|error' || true
```

Result:

- no matching crash/native/persistence/permission/duplicate/unhandled failure lines found in the post-deploy window

## Neighboring Ports

Ports remained scoped as expected:

- `127.0.0.1:5077`: `dotnet`, ChessOnline Kestrel
- `0.0.0.0:80`: `nginx`
- `*:443`: `xray-linux-amd6`
- `*:22527`: `outline-ss-serv`
- `0.0.0.0:3000`: Docker proxy
- `0.0.0.0:22`: SSH

No nginx, firewall, TLS/443, x-ui/Xray, Outline, Albatronix Docker, Unreal, PostgreSQL container, DNS, or `/var/lib/chessonline` mutation was performed.

## Result

Status: PASS

The deployed Hetzner server now exposes the P4J/P4K resume, spectator, and lobby capability surface over public HTTP 80. Gameplay smoke and UI scenarios remain separate follow-up phases.
