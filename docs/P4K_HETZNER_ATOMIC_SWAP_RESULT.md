# P4K Hetzner Atomic Swap Result

Date: 2026-07-11

## Scope

Phase 12 replaced only the ChessOnline application payload under `/opt/chessonline/server` using the staged P4K archive. The deployment restarted only `chessonline.service`.

No nginx, UFW/firewall, TLS/443, x-ui/Xray, Outline, Albatronix Docker, Unreal, PostgreSQL containers, DNS, systemd unit, port configuration, or `/var/lib/chessonline` runtime state was changed.

## Pre-Mutation Gate

Immediately before the swap:

- `chessonline.service`: `active`
- backup existed: `/opt/chessonline/backups/server-before-p4k-20260711-184042.tar.gz`
- backup SHA-256: `65bccdbd74c3da2063c97b45ffb7626c75edbca877fa4ac3b3041d190e8dc043`
- staged archive: `/opt/chessonline/incoming/ChessOnlineServer-P4K-f33240e87cd3.tar.gz`
- staged archive SHA-256: `2868635c362da78bfa2cdd2796ab31efe7cbee610d277d7db2db192539ce8a1d`
- staged archive `server-build.json` contained expected commit `f33240e87cd39ed6d2cfb7b612a8504c28f85586`
- disk free: `61G` available on `/`
- loopback health before deploy: PASS
- public HTTP health before deploy: PASS

## Deploy Command

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Deploy-ChessOnlineServer-Hetzner.ps1 `
  -ArchivePath .tmp\deploy\ChessOnlineServer-P4K-f33240e87cd3.tar.gz `
  -ArchiveSha256 2868635C362DA78BFA2CDD2796AB31EFE7CBEE610D277D7DB2DB192539CE8A1D `
  -SshTarget root@178.105.220.117 `
  -SshKeyPath "$env:USERPROFILE\.ssh\id_ed25519_hetzner" `
  -ExpectedCommit f33240e87cd39ed6d2cfb7b612a8504c28f85586 `
  -SkipUpload `
  -RollbackOnFailure `
  -HealthTimeoutSeconds 60 `
  -NoSecretLog
```

`-SkipUpload` was used because Phase 11 had already staged and verified the archive.

## Result

- Deploy script result: PASS
- Rollback needed: no
- Previous server directory: `/opt/chessonline/server.prev.20260711-191019`
- Current service state: `active`
- Current main PID: `3457320`
- New `startedUtc`: `2026-07-11T19:10:22.0861339Z`

The deploy log included one transient loopback `curl` connection failure during the expected service restart window, then completed successfully after health became available.

## Active Payload After Swap

Current files:

| File | Size | Owner | Mode | Modified UTC |
| --- | ---: | --- | --- | --- |
| `/opt/chessonline/server/ChessOnlineServer.dll` | `122368` | `chessonline:chessonline` | `666` | `2026-07-11 18:17:24` |
| `/opt/chessonline/server/server-build.json` | `194` | `chessonline:chessonline` | `666` | `2026-07-11 18:18:52` |
| `/opt/chessonline/server/libChess3DEngine.so` | `341296` | `chessonline:chessonline` | `666` | `2026-06-21 13:31:30` |

## Immediate Capability Check

Loopback and public diagnostics now report:

- `serverCommit`: `f33240e87cd39ed6d2cfb7b612a8504c28f85586`
- `build.packageId`: `chessonline-linux-x64-f33240e87cd3`
- `profileCount`: `5`
- `authEnabled`: `true`
- `authorityIsSupported`: `true`
- `authorityPlatform`: `Linux`
- `requestLegalPreview`: `true`
- `realtimeResync`: `true`
- `actionLog`: `true`
- `matchmaking`: `true`
- `resumeMatch`: `true`
- `spectatorMode`: `true`
- `lobbySnapshot`: `true`

`supportedHubMethods` now includes:

- `RequestResumeMatch`
- `JoinSpectator`
- `RequestLobbySnapshot`
- `RequestLegalPreview`

## Next Gate

Phase 13 should perform a fuller post-deploy health gate:

- loopback health;
- nginx-local health;
- public HTTP health;
- journal check;
- neighboring port check;
- capability assertions.
