# Next Era Production-Like Linux Layout Result

Date: 2026-06-21

Host placeholder: `<HETZNER_HOST>`

This phase moved the tested Linux package from `/tmp` into a production-like Linux filesystem layout. It did not install systemd, Nginx, TLS, Redis, a SignalR backplane, or any public service.

## Created Linux Identity

The VPS now has a dedicated system account:

```text
chessonline
```

Shape:

```text
useradd --system --create-home --home-dir /var/lib/chessonline --shell /usr/sbin/nologin chessonline
```

This account owns mutable runtime data. It does not own the application package.

## Directory Layout

Package files:

```text
/opt/chessonline/server
```

Mutable runtime paths:

```text
/var/lib/chessonline/data
/var/lib/chessonline/keyring
/var/log/chessonline
/var/backups/chessonline
```

Verified ownership and mode shape:

```text
/opt/chessonline                  root:root        0755
/opt/chessonline/server           root:root        0755
/var/lib/chessonline              chessonline      0750
/var/lib/chessonline/data         chessonline      0700
/var/lib/chessonline/keyring      chessonline      0700
/var/log/chessonline              chessonline      0750
/var/backups/chessonline          chessonline      0750
```

The initial tar extraction produced overly broad modes. They were corrected before smoke:

```text
directories under /opt/chessonline/server -> 0755
files under /opt/chessonline/server       -> 0644
```

## Package Verification

Verified package files:

```text
/opt/chessonline/server/ChessOnlineServer.dll
/opt/chessonline/server/libChess3DEngine.so
```

The package remains root-owned and readable/executable by the service user.

## Service-User Smoke

The app was started as `chessonline`, not root:

```bash
runuser -u chessonline -- sh -c 'cd /opt/chessonline/server; env ... dotnet /opt/chessonline/server/ChessOnlineServer.dll > /var/log/chessonline/layout-smoke.log 2>&1 &'
```

Runtime configuration:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5077
CHESS3D_ONLINE_HostedOnline__Auth__EnableAuthentication=true
CHESS3D_ONLINE_HostedOnline__Auth__AllowDevAnonymousSessions=false
CHESS3D_ONLINE_HostedOnline__Persistence__StorePath=/var/lib/chessonline/data/store.json
CHESS3D_ONLINE_HostedOnline__DataProtection__KeyRingPath=/var/lib/chessonline/keyring
```

Smoke probes passed:

| Probe | Result |
| --- | --- |
| `GET /healthz/live` | `Healthy` |
| `GET /healthz/ready` | JSON ready response with `profileCount=5` |
| `GET /chess3d/diagnostics` | Linux-native authority diagnostics |

Important diagnostics:

- `authorityPlatform`: `Linux`
- `authorityNativeLibraryName`: `libChess3DEngine.so`
- `authorityNativeLibraryPath`: `/opt/chessonline/server/libChess3DEngine.so`
- `authEnabled`: `true`
- `persistenceProvider`: `json`

The server wrote:

```text
/var/lib/chessonline/data/store.json
/var/lib/chessonline/keyring/key-*.xml
/var/log/chessonline/layout-smoke.log
```

These are runtime artifacts and must not be committed.

## Cleanup Status

The temporary process was stopped after the smoke.

Post-stop checks:

- no listener on `:5077`;
- no persistent `chessonline.service` installed yet;
- no Nginx config installed yet;
- no TLS/domain work performed.

## Known Notes

- The Data Protection keyring warning about no XML encryptor remains expected for this dry-run. The key directory is now restricted to the service account, but certificate/encryptor hardening is still future work.
- PowerShell-to-SSH inline scripts should avoid CRLF stdin surprises; use one-line SSH commands or normalize script text before piping to `bash -s`.

## Still Deferred

- systemd service.
- Nginx reverse proxy.
- public HTTP health.
- TLS/domain.
- backup/restore automation.
- log rotation.
- production secrets management.
- Redis/Azure SignalR/backplane.
