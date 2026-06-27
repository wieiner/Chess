# P4G2 Phase 13 - Hetzner Server Version Gap

Date: 2026-06-27

## Summary

The local repository on `main` contains the online legal-preview hub method, but the public Hetzner deployment does not expose it yet. This explains why the Phase 12 smoke tool receives `HubException: Method does not exist` when it tries to invoke `RequestLegalPreview`.

This is a deployment-version gap, not a Chess3D rules bug and not a client DTO bug.

## Local Repository State

Local code contains:

- `src/ChessOnlineServer/Chess3DRelayHub.cs`: `RequestLegalPreview(OnlineProtocolMessage message)`
- `src/ChessOnlineProtocol/OnlineRoomRegistry.cs`: authority-side legal preview routing
- `src/ChessOnlineProtocol/OnlineGameSession.cs`: native-engine legal preview adapter
- `src/ChessOnlineClient/ChessOnlineRelayClient.cs`: `RequestLegalPreviewAsync`
- `src/ChessOnlineApp/MainWindow.xaml.cs`: online board legal preview UI flow
- `tests/ChessOnlineContractTests/Program.cs`: legal preview contract coverage

Local server build:

```powershell
dotnet build src\ChessOnlineServer\ChessOnlineServer.csproj -c Release
```

Result: PASS.

## Public Hetzner State

Public HTTP checks:

- `http://178.105.220.117/healthz/live`: `Healthy`
- `http://178.105.220.117/healthz/ready`: ready JSON with `profileCount=5`
- `http://178.105.220.117/chess3d/diagnostics`: Linux native authority OK, but no explicit feature/capabilities fields yet

Diagnostics snapshot includes:

- `authorityRuntimeKind=LinuxNativeFuture`
- `authorityIsSupported=true`
- `authorityPlatform=Linux`
- `authorityNativeLibraryName=libChess3DEngine.so`
- `authorityNativeLibraryPath=/opt/chessonline/server/libChess3DEngine.so`
- `authEnabled=true`
- `persistenceProvider=json`
- `startedUtc=2026-06-21T14:53:35.8563945Z`

## Read-Only SSH Probe

Command shape:

```powershell
ssh -i "$env:USERPROFILE\.ssh\id_ed25519_hetzner" root@178.105.220.117 "systemctl status chessonline.service --no-pager; ls -lah /opt/chessonline/server | head -80; stat /opt/chessonline/server/ChessOnlineServer.dll /opt/chessonline/server/ChessOnlineProtocol.dll /opt/chessonline/server/libChess3DEngine.so"
```

Findings:

- `chessonline.service` is active and has been running since `2026-06-21 14:53:14 UTC`.
- Kestrel process: `/usr/bin/dotnet /opt/chessonline/server/ChessOnlineServer.dll`.
- `/opt/chessonline/server/ChessOnlineServer.dll` modify time: `2026-06-21 14:52:46 UTC`.
- `/opt/chessonline/server/ChessOnlineProtocol.dll` modify time: `2026-06-21 14:52:16 UTC`.
- `/opt/chessonline/server/libChess3DEngine.so` modify time: `2026-06-21 13:31:30 UTC`.

## Gap

The local repo contains P4G2 Phase 04+ legal-preview server support, but the deployed server binary set predates that code path. The server can still run health/auth/matchmaking/start/action smoke through known-safe actions, but cannot serve the current UI legal-preview hub call.

## Decision

Phase 14 should expose explicit server capabilities in diagnostics so clients and operators can see whether the deployed server supports:

- `requestLegalPreview`
- `realtimeResync`
- `actionLog`
- `matchmaking`

Phase 15+ should build and deploy a new ChessOnlineServer package only. This should not touch:

- Nginx config
- UFW/firewall
- TLS/443
- x-ui/Xray
- Outline
- Albatronix Docker
- Unreal SYServer

## Verification

Phase 13 verified:

- local server build passes;
- public health endpoints work;
- public diagnostics confirm Linux native authority;
- read-only SSH timestamps explain why `RequestLegalPreview` is absent on the deployed hub.
