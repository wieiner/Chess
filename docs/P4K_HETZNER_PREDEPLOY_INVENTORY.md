# P4K Hetzner Predeploy Inventory

Date: 2026-07-11

## Scope

Phase 07 captured a read-only pre-deploy inventory for the Hetzner host before any package replacement. No files were copied, no services were restarted, no firewall/nginx/TLS/443/x-ui/Xray/Outline/Albatronix/Unreal/PostgreSQL configuration was changed, and no runtime store or keyring content was read.

## Git Baseline

- Local branch: `main`
- Phase baseline commit: `6c7bc23cc`
- Previous CI: `29163519836`, success

## ChessOnline Service

Read-only command group:

```bash
systemctl is-active chessonline.service
systemctl show chessonline.service \
  -p User \
  -p Group \
  -p ExecStart \
  -p WorkingDirectory \
  -p EnvironmentFiles \
  -p MainPID \
  -p ActiveEnterTimestampUTC \
  -p FragmentPath
```

Observed:

- service state: `active`
- main PID: `831`
- executable: `/usr/bin/dotnet /opt/chessonline/server/ChessOnlineServer.dll`
- working directory: `/opt/chessonline/server`
- user/group: `chessonline` / `chessonline`
- unit path: `/etc/systemd/system/chessonline.service`
- process start from `ExecStart`: `Fri 2026-07-03 11:26:50 UTC`

`systemctl cat` was intentionally not used because unit files may contain inline secrets.

## Ports and Neighbor Services

Read-only port inventory:

- `127.0.0.1:5077`: `dotnet`, PID `831`, ChessOnline Kestrel
- `0.0.0.0:80`: `nginx`
- `*:443`: `xray-linux-amd6`
- `*:22527`: `outline-ss-serv`
- `0.0.0.0:3000` and `[::]:3000`: Docker proxy for Albatronix
- `0.0.0.0:22` and `[::]:22`: SSH

Docker inventory was read-only:

- `albatronix-sse-server`: `0.0.0.0:3000->3000/tcp`
- `albatronix-postgres`: `5432/tcp`
- `watchtower`: `8080/tcp`
- `shadowbox`: Outline container

These neighboring services are explicitly out of scope for the P4K server package deployment.

## Current Server Payload Metadata

Current files under `/opt/chessonline/server`:

| File | Size | Owner | Mode | Modified UTC | SHA-256 |
| --- | ---: | --- | --- | --- | --- |
| `ChessOnlineServer.dll` | `114688` | `chessonline:chessonline` | `666` | `2026-06-27 20:59:34` | `df77825499cbaf701f14182716ccc47ad6f566a0159cbdd1be7aa8bdcd55d3b9` |
| `ChessOnlineProtocol.dll` | `216064` | `chessonline:chessonline` | `666` | `2026-06-27 20:59:28` | `30fb9dcb056fefe6d035de026e26a934126eefaeeba8316aeb5d9ef0ce7f38d8` |
| `libChess3DEngine.so` | `341296` | `chessonline:chessonline` | `666` | `2026-06-21 13:31:30` | `a5b5e0b707d09b199d49fe62ca5b5f00895f28b1a78e4d082584776c9913694d` |

The remote native library hash matches the locally packaged `libChess3DEngine.so` hash from Phase 05.

## Runtime Directory Metadata

Only metadata was read:

| Path | Type | Owner | Mode | Modified UTC |
| --- | --- | --- | --- | --- |
| `/var/lib/chessonline` | directory | `chessonline:chessonline` | `750` | `2026-06-21 14:37:46` |
| `/var/lib/chessonline/keyring` | directory | `chessonline:chessonline` | `700` | `2026-06-21 14:38:34` |

No runtime store, account data, refresh sessions, Data Protection keys, or keyring file contents were read or copied.

## Host Capacity

Disk:

- root filesystem: `75G`
- used: `12G`
- available: `61G`
- use: `16%`

Memory:

- total: `3.7Gi`
- used: `811Mi`
- free: `1.5Gi`
- available: `2.9Gi`
- swap: `0B`

## Health and Diagnostics

Local Kestrel on the server:

- `http://127.0.0.1:5077/healthz/live`: `Healthy`
- `http://127.0.0.1:5077/healthz/ready`: ready JSON with `profileCount=5`, `authEnabled=true`, `persistenceProvider=json`
- `http://127.0.0.1:5077/chess3d/diagnostics`: Linux native authority OK

Public HTTP through nginx:

- `http://178.105.220.117/healthz/live`: `Healthy`
- `http://178.105.220.117/healthz/ready`: ready JSON with `profileCount=5`, `authEnabled=true`, `persistenceProvider=json`
- `http://178.105.220.117/chess3d/diagnostics`: Linux native authority OK

Current public diagnostics still show the pre-P4K capability set:

- `requestLegalPreview=true`
- `realtimeResync=true`
- `actionLog=true`
- `matchmaking=true`
- `resumeMatch`: absent
- `spectatorMode`: absent
- `lobbySnapshot`: absent
- `supportedHubMethods` does not include `RequestResumeMatch`, `JoinSpectator`, or `RequestLobbySnapshot`

This confirms the expected deployment gap: the host is healthy, but the running package is older than the local P4J/P4K server build.

## Deployment Readiness

The pre-deploy inventory is safe to proceed to the backup phase:

- ChessOnline service is active.
- Public HTTP is healthy.
- Neighboring services and ports are identified and must remain untouched.
- Runtime directories exist with restrictive metadata.
- Disk and memory are sufficient for package backup/replacement.
- Current payload hashes are recorded for rollback comparison.
