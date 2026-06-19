# P4D Hetzner Kestrel Smoke

P4D phase 10 evaluated the first Kestrel-only smoke gate for `ChessOnlineServer` on Hetzner.

## Result

The Kestrel smoke was not run.

This is intentional. The phase requires a real linux-x64 server package and a working Linux native `libChess3DEngine.so` authority. Neither exists yet:

- the native Linux build is blocked by missing build tools on the VPS;
- the local cross-build is blocked by missing sysroot;
- the server publish is blocked by `net8.0-windows` project targets and the missing Linux native library.

Starting a server on Hetzner without a valid Linux native authority would produce a misleading smoke result.

## What was not done

- No package was copied to Hetzner.
- No Kestrel process was started.
- No `/opt/chessonline` or `/var/lib/chessonline` path was touched.
- No systemd or nginx configuration was changed.
- No test credentials, keyring, store, certificate, or runtime DB was created.

## Intended future smoke once unblocked

After a real linux-x64 package exists, use a temporary path only:

```bash
mkdir -p /tmp/chessonline-smoke/server
mkdir -p /tmp/chessonline-smoke/data
mkdir -p /tmp/chessonline-smoke/keyring
cd /tmp/chessonline-smoke/server
ASPNETCORE_URLS=http://127.0.0.1:5077 dotnet ChessOnlineServer.dll
```

Then from the same host/session:

```bash
curl http://127.0.0.1:5077/healthz/live
curl http://127.0.0.1:5077/healthz/ready
curl http://127.0.0.1:5077/chess3d/diagnostics
```

If auth smoke is run later, credentials must be ephemeral and never committed.

## Current gate

| Gate | Status |
| --- | --- |
| Hetzner reachable | yes, from phase 05 read-only probe |
| Build tools installed | no |
| `libChess3DEngine.so` built | no |
| linux-x64 server package | no |
| Kestrel smoke eligible | no |
