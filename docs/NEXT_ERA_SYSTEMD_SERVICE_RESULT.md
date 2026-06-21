# Next Era systemd Service Result

Date: 2026-06-21

Host placeholder: `<HETZNER_HOST>`

This phase installed and started a real `systemd` unit for `ChessOnlineServer` on Hetzner. It did not install or configure Nginx, TLS, Redis, a SignalR backplane, or Kubernetes.

## Unit Installed

Installed unit path:

```text
/etc/systemd/system/chessonline.service
```

The tracked template is:

```text
deploy/linux/chessonline-server.service.template
```

Important unit properties:

```text
User=chessonline
Group=chessonline
WorkingDirectory=/opt/chessonline/server
ExecStart=/usr/bin/dotnet /opt/chessonline/server/ChessOnlineServer.dll
Restart=always
RestartSec=10
ASPNETCORE_URLS=http://127.0.0.1:5077
CHESS3D_ONLINE_HostedOnline__Persistence__StorePath=/var/lib/chessonline/data/store.json
CHESS3D_ONLINE_HostedOnline__DataProtection__KeyRingPath=/var/lib/chessonline/keyring
```

The service remains loopback-only. Public HTTP exposure belongs to the Nginx phase.

## Commands Run

```bash
systemctl daemon-reload
systemctl enable chessonline.service
systemctl restart chessonline.service
systemctl status chessonline.service --no-pager
```

## Service Status

The service is installed and enabled:

```text
Loaded: loaded (/etc/systemd/system/chessonline.service; enabled)
Active: active (running)
Main PID: dotnet /opt/chessonline/server/ChessOnlineServer.dll
```

## Health Results

Loopback probes from the VPS passed:

| Probe | Result |
| --- | --- |
| `GET http://127.0.0.1:5077/healthz/live` | `Healthy` |
| `GET http://127.0.0.1:5077/healthz/ready` | JSON ready response |
| `GET http://127.0.0.1:5077/chess3d/diagnostics` | JSON diagnostics response |

Important diagnostics:

- `profileCount`: `5`
- `authEnabled`: `true`
- `persistenceProvider`: `json`
- `authorityPlatform`: `Linux`
- `authorityNativeLibraryName`: `libChess3DEngine.so`
- `authorityNativeLibraryPath`: `/opt/chessonline/server/libChess3DEngine.so`

## Journal

`journalctl -u chessonline.service -n 80 --no-pager` showed normal Kestrel startup:

```text
Now listening on: http://127.0.0.1:5077
Hosting environment: Production
Content root path: /opt/chessonline/server
```

No secret values were copied into the repository. Runtime store/keyring/logs stay on the VPS under `/var`.

## Still Deferred

- Nginx reverse proxy.
- External/public HTTP health.
- TLS/domain.
- HTTPS token enforcement for public use.
- Backup/restore automation.
- Log rotation.
- Redis/Azure SignalR/backplane.
