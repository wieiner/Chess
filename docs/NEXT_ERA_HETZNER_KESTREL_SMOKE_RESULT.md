# Next Era Hetzner Kestrel Smoke Result

Date: 2026-06-21

Host placeholder: `<HETZNER_HOST>`

This phase ran the first temporary Linux execution smoke for `ChessOnlineServer` with the Linux-native `libChess3DEngine.so`. It did not install systemd, Nginx, TLS, or production paths.

## Package Transfer

Local package archive:

```text
DeploymentOutput/linux-x64/chessonline-server-linux-x64.tar.gz
```

Remote temporary paths:

```text
/tmp/chessonline-server-linux-x64.tar.gz
/tmp/chessonline-smoke/server
/tmp/chessonline-smoke/data
/tmp/chessonline-smoke/keyring
/tmp/chessonline-smoke/logs
```

Verified on the host:

- `/tmp/chessonline-smoke/server/ChessOnlineServer.dll`
- `/tmp/chessonline-smoke/server/libChess3DEngine.so`

## Start Command Shape

The server was started in temp mode with:

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5077
HostedOnline__Auth__EnableAuthentication=true
HostedOnline__Auth__AllowDevAnonymousSessions=false
HostedOnline__Persistence__StorePath=/tmp/chessonline-smoke/data/store.json
HostedOnline__DataProtection__KeyRingPath=/tmp/chessonline-smoke/keyring
dotnet ChessOnlineServer.dll
```

The process listened only on loopback:

```text
http://127.0.0.1:5077
```

## Smoke Results

| Probe | Result |
| --- | --- |
| `GET /healthz/live` | `Healthy` |
| `GET /healthz/ready` | JSON ready response |
| `GET /chess3d/diagnostics` | JSON diagnostics response |

Important diagnostics values:

- `profileCount`: `5`
- `authEnabled`: `true`
- `persistenceProvider`: `json`
- `authorityRuntimeKind`: `LinuxNativeFuture`
- `authorityIsPortableRuntime`: `true`
- `authorityIsSupported`: `true`
- `authorityPlatform`: `Linux`
- `authorityProcessArchitecture`: `X64`
- `authorityNativeLibraryName`: `libChess3DEngine.so`
- `authorityNativeLibraryPath`: `/tmp/chessonline-smoke/server/libChess3DEngine.so`

This proves that the Linux server package can start on Hetzner and load the Linux-native Chess3D authority library in Kestrel temp mode.

## Warnings

The temporary run logged the expected Data Protection warning:

```text
No XML encryptor configured. Key ... may be persisted to storage in unencrypted form.
```

This is acceptable only for `/tmp` smoke. Production layout must use restricted ownership/permissions under `/var/lib/chessonline/keyring`, and TLS/auth hardening remains a later phase.

## Stop / Cleanup Status

The first stop command used a PowerShell double-quoted SSH command and local PowerShell expanded `$(cat ...)` before SSH. That did not stop the process. The command was re-run with single-quoted remote shell text and stopped the `dotnet ChessOnlineServer.dll` process correctly.

Post-stop check:

- no listener on `:5077`;
- no persistent ChessOnline service was installed;
- `/opt/chessonline` and `/var/lib/chessonline` were not touched.

## Still Deferred

- Remote auth/register/login smoke.
- SignalR matchmaking and Asgard action smoke.
- Production-like `/opt/chessonline` and `/var/lib/chessonline` layout.
- `systemd` service.
- Nginx reverse proxy.
- Public HTTP health.
- TLS/domain handling.
