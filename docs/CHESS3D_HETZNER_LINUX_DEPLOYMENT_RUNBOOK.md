# Chess3D Hetzner Linux Deployment Runbook

Status: P4C honest-mode runbook. This document is a deployment plan, not a claim that the current package is Linux-runnable.

## Current Runtime Blocker

The current authoritative online server path still depends on the Windows-native `Chess3DEngine.dll` rules authority. The P4C adapter boundary makes this dependency visible, but does not create a Linux `Chess3DEngine.so`.

Until P4D provides and verifies a Linux-native rules authority, Hetzner Linux can host only a future compatible package or a gateway-style shell. The working executable path today remains Windows local/Windows Server packaging.

## Assumptions

- Hetzner Cloud VPS.
- Ubuntu LTS.
- SSH access as an administrator.
- Optional domain name pointing to the VPS.
- No real SSH deploy is performed by this repository task.
- No secrets, IP addresses, SSH keys, certificates, or passwords are committed.

## Public Ports

Hetzner firewall / server firewall should expose only:

- `22/tcp` for SSH, ideally restricted to operator IPs.
- `80/tcp` for HTTP and ACME challenge traffic.
- `443/tcp` for HTTPS and WebSocket traffic.

The application should listen locally on:

```text
127.0.0.1:5077
```

Nginx terminates public HTTP/HTTPS and proxies to the local Kestrel endpoint.

## Directory Layout

```text
/opt/chessonline/server              # application binaries/assets
/etc/chessonline                     # optional environment/config files
/var/lib/chessonline                 # runtime data root
/var/lib/chessonline/keyring         # ASP.NET Core Data Protection keys
/var/backups/chessonline             # operator backups
```

Runtime data must be owned by the service user and must not live in the Git repository.

## Linux User

Template:

```bash
sudo useradd --system --create-home --home-dir /var/lib/chessonline --shell /usr/sbin/nologin chessonline
sudo mkdir -p /opt/chessonline/server /var/lib/chessonline/keyring /var/backups/chessonline
sudo chown -R chessonline:chessonline /var/lib/chessonline
sudo chown -R root:root /opt/chessonline
```

## Install ASP.NET Core Runtime

Template for Ubuntu:

```bash
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0 nginx
```

If the target Ubuntu feed does not provide the required runtime, follow Microsoft package-feed guidance for that Ubuntu version. Do not mix package feeds casually.

## Copy Package

Future Linux-compatible package template:

```bash
sudo rsync -a --delete ./ChessOnlineServer/ /opt/chessonline/server/
sudo chown -R root:root /opt/chessonline/server
sudo chmod -R a+rX /opt/chessonline/server
```

Do not copy runtime `Data`, local stores, key rings, certificates, or secrets into source control.

## Environment Configuration

Example environment file:

```bash
sudo tee /etc/chessonline/chessonline.env >/dev/null <<'EOF'
ASPNETCORE_ENVIRONMENT=Production
CHESS3D_ONLINE_HostedOnline__HostUrls=http://127.0.0.1:5077
CHESS3D_ONLINE_HostedOnline__Persistence__StorePath=/var/lib/chessonline/chess3d-online-store.json
CHESS3D_ONLINE_HostedOnline__DataProtection__KeyRingPath=/var/lib/chessonline/keyring
EOF
sudo chmod 640 /etc/chessonline/chessonline.env
sudo chown root:chessonline /etc/chessonline/chessonline.env
```

Use placeholders for future secrets. Never commit populated production config.

## systemd Service

Template:

```ini
[Unit]
Description=Chess3D Online Server
After=network.target

[Service]
WorkingDirectory=/opt/chessonline/server
ExecStart=/usr/bin/dotnet /opt/chessonline/server/ChessOnlineServer.dll
Restart=always
RestartSec=5
SyslogIdentifier=chessonline
User=chessonline
EnvironmentFile=/etc/chessonline/chessonline.env

[Install]
WantedBy=multi-user.target
```

Install:

```bash
sudo cp chessonline-server.service /etc/systemd/system/chessonline-server.service
sudo systemctl daemon-reload
sudo systemctl enable chessonline-server
sudo systemctl start chessonline-server
sudo systemctl status chessonline-server
```

Current blocker reminder: this will not work with the Windows-native package. It is the desired shape for the future Linux-compatible build.

## Nginx Reverse Proxy

Template:

```nginx
server {
    listen 80;
    server_name example.com;

    location / {
        proxy_pass http://127.0.0.1:5077;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $connection_upgrade;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

If using `$connection_upgrade`, define it in the `http` block:

```nginx
map $http_upgrade $connection_upgrade {
    default upgrade;
    '' close;
}
```

Validate and reload:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

## TLS Later

Use a standard ACME flow only after HTTP health checks work. Keep private keys and certificates out of the repository.

## Health Checks

Local:

```bash
curl -f http://127.0.0.1:5077/healthz/live
curl -f http://127.0.0.1:5077/healthz/ready
```

Through Nginx:

```bash
curl -f http://example.com/healthz/live
curl -f http://example.com/healthz/ready
```

WebSocket/SignalR smoke should be run only after the HTTP checks pass.

## Logs

```bash
sudo journalctl -u chessonline-server -n 200 --no-pager
sudo journalctl -u nginx -n 200 --no-pager
```

## Backup

Minimum backup set:

```text
/var/lib/chessonline/chess3d-online-store.json
/var/lib/chessonline/keyring
/etc/chessonline/chessonline.env
```

Template:

```bash
sudo mkdir -p /var/backups/chessonline/$(date -u +%Y%m%dT%H%M%SZ)
sudo cp -a /var/lib/chessonline/chess3d-online-store.json /var/backups/chessonline/$(date -u +%Y%m%dT%H%M%SZ)/ 2>/dev/null || true
sudo cp -a /var/lib/chessonline/keyring /var/backups/chessonline/$(date -u +%Y%m%dT%H%M%SZ)/
```

Use an operator-reviewed backup script in real deployment so the timestamp is computed once and retention is explicit.

## Update

1. Confirm current health.
2. Stop the service.
3. Back up store and keyring.
4. Replace `/opt/chessonline/server` with the new package.
5. Start the service.
6. Check local health.
7. Check Nginx health.
8. Run online smoke tests.

```bash
sudo systemctl stop chessonline-server
sudo rsync -a --delete ./ChessOnlineServer/ /opt/chessonline/server/
sudo systemctl start chessonline-server
curl -f http://127.0.0.1:5077/healthz/ready
```

## Rollback

1. Stop the service.
2. Restore the previous `/opt/chessonline/server`.
3. Restore matching `/var/lib/chessonline` data if a store format changed.
4. Start the service.
5. Re-run health checks.

## Troubleshooting

- `systemctl status chessonline-server`: service state and last error.
- `journalctl -u chessonline-server`: application logs.
- `nginx -t`: reverse proxy syntax.
- `curl http://127.0.0.1:5077/healthz/live`: local Kestrel reachability.
- `curl http://example.com/healthz/live`: public proxy reachability.
- Missing authority library on Linux: expected until P4D creates `Chess3DEngine.so` and platform-specific loader tests.

## P4D Prerequisites

Before this runbook can become an execution runbook:

- Build a Linux-native rules authority artifact.
- Add platform-specific native library loading.
- Add Linux CI for server/protocol/persistence.
- Add state-hash parity tests between Windows and Linux authority.
- Produce a Linux package that passes health and online authority smoke tests.
