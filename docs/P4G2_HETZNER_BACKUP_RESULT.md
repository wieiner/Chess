# P4G2 Phase 16 - Hetzner Backup Result

Date: 2026-06-27

## Scope

This phase created a rollback backup before deploying the updated ChessOnlineServer build that contains `RequestLegalPreview` diagnostics and hub support.

No network stack was changed:

- Nginx was not modified.
- UFW/firewall was not modified.
- TLS/443 was not touched.
- x-ui/Xray, Outline, Albatronix Docker, and Unreal SYServer were not touched.

## Backup Command

The backup was created over SSH with a server-local timestamp:

```bash
mkdir -p /opt/chessonline/backups
backup=/opt/chessonline/backups/server-before-p4g2-$(date +%Y%m%d-%H%M%S).tar.gz
tar -czf $backup /opt/chessonline/server /etc/systemd/system/chessonline.service
```

## Backup Artifact

Created on Hetzner:

```text
/opt/chessonline/backups/server-before-p4g2-20260627-203832.tar.gz
```

Reported size:

```text
385K
```

The backup remains on the server and is not committed to Git.

## Pre-Deploy Service State

`chessonline.service` was active before deployment:

```text
Loaded: loaded (/etc/systemd/system/chessonline.service; enabled)
Active: active (running)
Main PID: dotnet /opt/chessonline/server/ChessOnlineServer.dll
```

Loopback readiness remained healthy:

```json
{"status":"ready","protocolId":"chess3d.relay.v1","protocolVersion":"0.1","profileCount":5,"authEnabled":true,"persistenceProvider":"json"}
```

Loopback diagnostics confirmed the currently deployed server was still the older build without the new capability flags:

```json
{
  "protocolId": "chess3d.relay.v1",
  "protocolVersion": "0.1",
  "profileCount": 5,
  "authorityRuntimeKind": "LinuxNativeFuture",
  "authorityIsSupported": true,
  "authorityPlatform": "Linux",
  "authorityNativeLibraryName": "libChess3DEngine.so",
  "authorityNativeLibraryPath": "/opt/chessonline/server/libChess3DEngine.so",
  "authEnabled": true
}
```

## Rollback Boundary

If the next deploy fails, rollback should restore only the ChessOnlineServer payload:

```bash
systemctl stop chessonline.service
mv /opt/chessonline/server /opt/chessonline/server.bad.$(date +%Y%m%d-%H%M%S)
tar -xzf /opt/chessonline/backups/server-before-p4g2-20260627-203832.tar.gz -C /
systemctl start chessonline.service
```

The rollback must not modify Nginx, TLS/443, UFW, x-ui/Xray, Outline, Albatronix, or Unreal services.
