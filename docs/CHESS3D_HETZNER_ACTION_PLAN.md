# Chess3D Hetzner Action Plan

Status: P4C Phase 12 planning document.

This is a safe action plan for a future Hetzner deployment. It intentionally uses placeholders and does not claim the current Windows-native package can run on Linux.

## Placeholders

Use placeholders in tracked docs:

- `<HETZNER_SERVER_IP>`
- `<HETZNER_HOSTNAME>`
- `<OPERATOR_USER>`
- `<LOCAL_PUBLIC_KEY_PATH>`

Do not commit the real IP, private key path, private key, known-hosts entry, production hostname, or populated config.

## Stage 0 - Preconditions

- P4D Linux-native rules authority exists.
- `ChessOnlineServer` can publish/run for `linux-x64`.
- State-hash parity is green for all five Chess3D RuleProfiles.
- Windows CI remains green.
- A Linux smoke test has passed in a local VM or controlled non-production host.

## Stage 1 - Read-Only Host Probe

Allowed commands for a future controlled probe:

```bash
ssh <OPERATOR_USER>@<HETZNER_SERVER_IP> 'uname -a && lsb_release -a || cat /etc/os-release'
ssh <OPERATOR_USER>@<HETZNER_SERVER_IP> 'id && whoami && pwd'
ssh <OPERATOR_USER>@<HETZNER_SERVER_IP> 'systemctl --version | head -1'
ssh <OPERATOR_USER>@<HETZNER_SERVER_IP> 'nginx -v 2>&1 || true'
ssh <OPERATOR_USER>@<HETZNER_SERVER_IP> 'dotnet --info || true'
```

Do not upload packages, edit services, install packages, or open firewall ports during a read-only probe.

## Stage 2 - Host Preparation

Only after P4D native Linux readiness:

- create dedicated `chessonline` service user;
- install ASP.NET Core runtime;
- install nginx;
- create `/opt/chessonline/server`;
- create `/var/lib/chessonline`;
- create `/var/lib/chessonline/keyring`;
- create `/var/backups/chessonline`.

Keep runtime state owned by the service user. Keep application binaries owned by root.

## Stage 3 - Package Upload

Upload only a Linux-compatible package:

```bash
rsync -a --delete ./ChessOnlineServer/ <OPERATOR_USER>@<HETZNER_SERVER_IP>:/tmp/chessonline-server/
```

Then promote with sudo on the host. Do not upload:

- local `Data`;
- local key rings;
- `appsettings.Production.json`;
- secrets;
- developer logs.

## Stage 4 - systemd And Nginx

Use the existing templates as a starting point:

- `deploy/linux/chessonline-server.service.template`
- `deploy/linux/nginx-chessonline.conf.template`
- `deploy/linux/nginx-chessonline-https-snippet.template`

Bind Kestrel to `127.0.0.1:5077` and let nginx proxy public traffic.

## Stage 5 - Health And Smoke

Run:

```bash
curl -f http://127.0.0.1:5077/healthz/live
curl -f http://127.0.0.1:5077/healthz/ready
curl -f http://<HETZNER_HOSTNAME>/healthz/ready
```

Then run SignalR smoke from a trusted client. Public exposure should wait until health, logs, and backup policy are reviewed.

## Stage 6 - Backup And Rollback

Back up before every update:

- `/var/lib/chessonline/chess3d-online-store.json`;
- `/var/lib/chessonline/keyring`;
- `/etc/chessonline/chessonline.env`.

Rollback must restore a matching package and compatible runtime store/keyring.

## Stage 7 - Out Of Scope For P4C

- Redis/Azure SignalR/backplane.
- Kubernetes.
- Docker production image.
- public ranked matchmaking.
- automated TLS issuance in repository scripts.
- storing secrets in Git.

