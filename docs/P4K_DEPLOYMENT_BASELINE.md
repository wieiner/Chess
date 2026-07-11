# P4K Deployment Baseline

Date: 2026-07-11

## Scope

P4K starts from the P4J repository/local online UX work and prepares a safe server-package deployment to the existing Hetzner diagnostic HTTP 80 environment.

This baseline is read-only. It does not change nginx, UFW/firewall, TLS/443, x-ui/Xray, Outline, Albatronix Docker, Unreal SYServer, systemd, runtime stores, or keyrings.

## Local Git Baseline

- branch: `main`
- local HEAD: `4ab3ae2ded37078b531727a429e9b5f608f90d43`
- origin/main: `4ab3ae2ded37078b531727a429e9b5f608f90d43`
- commit: `P4J phase 25: finalize online match UX guide`
- working tree before Phase 00 docs: clean

Latest observed GitHub Actions:

- `28500726627` - `P4J phase 25: finalize online match UX guide` - success
- previous P4J phase runs also success through Phase 16+

## Public HTTP Baseline

Commands:

```powershell
curl.exe -fsS http://178.105.220.117/healthz/live
curl.exe -fsS http://178.105.220.117/healthz/ready
curl.exe -fsS http://178.105.220.117/chess3d/diagnostics
```

Observed:

- live: `Healthy`
- ready: `profileCount=5`, `authEnabled=true`, `persistenceProvider=json`
- diagnostics:
  - `requestLegalPreview=true`
  - `realtimeResync=true`
  - `actionLog=true`
  - `matchmaking=true`
  - `authorityRuntimeKind=LinuxNativeFuture`
  - `authorityIsSupported=true`
  - `authorityPlatform=Linux`
  - `authorityNativeLibraryName=libChess3DEngine.so`
  - `authorityNativeLibraryPath=/opt/chessonline/server/libChess3DEngine.so`

Current deployed `supportedHubMethods`:

- `Hello`
- `JoinMatchmaking`
- `CancelMatchmaking`
- `GetMatchmakingStatus`
- `Ready`
- `StartGame`
- `SubmitAction`
- `RequestSnapshot`
- `RequestActionLog`
- `RequestLegalPreview`
- `RequestDiagnostics`
- `Ping`

Missing from current public deployment:

- `RequestResumeMatch`
- `JoinSpectator`
- `RequestLobbySnapshot`

Deployment gap: the repository has P4J resume/spectator/lobby server/protocol/client/UI code, but the public Hetzner package still runs the older hub surface.

## Hetzner Read-Only Inventory

Command class used:

```powershell
ssh -i "$env:USERPROFILE\.ssh\id_ed25519_hetzner" root@178.105.220.117 "<read-only inventory>"
```

Service:

- `chessonline.service`
- loaded from `/etc/systemd/system/chessonline.service`
- active/running
- main process: `/usr/bin/dotnet /opt/chessonline/server/ChessOnlineServer.dll`
- observed main PID: `831`
- active since: `2026-07-03 11:26:50 UTC`

Ports/processes observed:

- `127.0.0.1:5077` - `dotnet` ChessOnline/Kestrel
- `0.0.0.0:80` - `nginx`
- `*:443` - `xray-linux-amd6`
- `*:22527` - `outline-ss-serv`
- `0.0.0.0:3000` / `[::]:3000` - `docker-proxy`

ChessOnline payload metadata:

- `/opt/chessonline/server/ChessOnlineServer.dll`
  - modify time: `2026-06-27 20:59:34 UTC`
  - owner/group: `chessonline:chessonline`
- `/opt/chessonline/server/ChessOnlineProtocol.dll`
  - modify time: `2026-06-27 20:59:28 UTC`
  - owner/group: `chessonline:chessonline`
- `/opt/chessonline/server/libChess3DEngine.so`
  - modify time: `2026-06-21 13:31:30 UTC`
  - owner/group: `chessonline:chessonline`

Runtime directory metadata only:

- `/var/lib/chessonline`
- permissions: `0750`
- owner/group: `chessonline:chessonline`

No runtime store/keyring contents were listed or copied.

## Do-Not-Touch Services

P4K must not touch:

- port `443`
- x-ui/Xray
- Outline
- Albatronix Docker
- Unreal SYServer
- nginx configuration
- UFW/firewall
- DNS/domain
- TLS/certbot
- `/var/lib/chessonline` runtime data contents

## Phase 00 Result

Status: baseline confirmed.

Next safe step: audit package/runtime boundaries before building or deploying any updated server payload.
