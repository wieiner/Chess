# Next Era Nginx Public HTTP Result

Date: 2026-06-21

Host placeholder: `<HETZNER_HOST>`

This phase installed Nginx on Hetzner and exposed the existing `chessonline.service` loopback Kestrel server through public HTTP port 80.

It did not configure TLS, a domain name, Redis, Azure SignalR, a backplane, Kubernetes, Docker orchestration, or public ranked matchmaking.

## Application Change

`ChessOnlineServer` now enables ASP.NET Core forwarded headers for trusted loopback proxies:

```text
X-Forwarded-For
X-Forwarded-Proto
```

The trusted proxy list is limited to loopback addresses. This keeps the Nginx-on-same-host path working without trusting arbitrary external forwarded headers.

## Nginx Configuration

Tracked template:

```text
deploy/linux/nginx-chessonline.conf.template
```

Installed runtime file:

```text
/etc/nginx/sites-available/chessonline
/etc/nginx/sites-enabled/chessonline
```

The default site was removed from `sites-enabled`.

Important proxy settings:

```text
proxy_pass http://127.0.0.1:5077;
proxy_http_version 1.1;
proxy_set_header Upgrade $http_upgrade;
proxy_set_header Connection "upgrade";
proxy_set_header Host $host;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;
proxy_cache_bypass $http_upgrade;
proxy_read_timeout 3600;
```

## Commands Run

Nginx was not installed before this phase, so the phase installed it:

```bash
apt-get update
DEBIAN_FRONTEND=noninteractive apt-get install -y nginx
nginx -t
systemctl enable nginx
systemctl restart nginx
```

The server package was republished first so the forwarded-header app change was deployed under:

```text
/opt/chessonline/server
```

## Port Status

After install:

```text
0.0.0.0:80       nginx
127.0.0.1:5077   dotnet ChessOnlineServer
```

Port 443 remains out of scope for this phase.

## Local VPS HTTP Probes

These passed on the VPS:

```text
curl http://127.0.0.1/healthz/live
curl http://127.0.0.1/healthz/ready
curl http://127.0.0.1/chess3d/diagnostics
```

Results:

- live: `Healthy`
- ready: JSON ready response with `profileCount=5`
- diagnostics: Linux-native authority diagnostics with `libChess3DEngine.so`

## External HTTP Probes

These passed from the workstation:

```text
curl http://<HETZNER_HOST>/healthz/live
curl http://<HETZNER_HOST>/healthz/ready
curl http://<HETZNER_HOST>/chess3d/diagnostics
```

Important external diagnostics:

- `protocolId`: `chess3d.relay.v1`
- `profileCount`: `5`
- `authEnabled`: `true`
- `authorityPlatform`: `Linux`
- `authorityNativeLibraryName`: `libChess3DEngine.so`
- `authorityNativeLibraryPath`: `/opt/chessonline/server/libChess3DEngine.so`

## Security Boundary

Public HTTP is diagnostic/dev-only. Do not use real user accounts or long-lived tokens over public HTTP.

Token issuance and public authenticated SignalR should be considered production-blocked until:

- a real domain is configured;
- TLS is issued and enforced;
- `RequireHttpsForTokens` remains true for non-loopback public traffic;
- operator runbooks cover log rotation, backups, and rollback.

## Still Deferred

- TLS/domain.
- HTTPS redirect.
- Public authenticated SignalR smoke.
- Production secrets/key management.
- Log rotation.
- Backup/restore automation.
- Redis/Azure SignalR/backplane.
