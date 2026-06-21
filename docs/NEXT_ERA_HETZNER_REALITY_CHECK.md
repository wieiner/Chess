# Next Era Hetzner Reality Check

Date: 2026-06-21

Host placeholder: `<HETZNER_HOST>`

This document summarizes a read-only SSH probe. It intentionally does not include private keys, known_hosts, tokens, passwords, certificates, raw SSH logs, runtime stores, or the real host address.

## Probe Scope

Read-only commands checked:

- Linux kernel and OS release;
- .NET SDK/runtime;
- clang, CMake, Ninja;
- Nginx availability;
- `chessonline.service` status;
- listening ports `80`, `443`, `5077`;
- temporary native build workspace;
- temporary Kestrel smoke workspace;
- production-like `/opt/chessonline` and `/var/lib/chessonline` paths;
- current Linux native `libChess3DEngine.so` artifact.

No package install, service change, file deletion, production path mutation, or deployment was performed.

## Operating System

- OS: Ubuntu 24.04.4 LTS (Noble Numbat)
- Kernel: `6.8.0-117-generic`
- Architecture: `x86_64`

## Installed Toolchain

| Tool | Status |
| --- | --- |
| .NET SDK | installed, `8.0.128` |
| .NET runtime | installed, `Microsoft.NETCore.App 8.0.28` |
| ASP.NET Core runtime | installed, `Microsoft.AspNetCore.App 8.0.28` |
| clang++ | installed, Ubuntu clang `18.1.3`, target `x86_64-pc-linux-gnu` |
| CMake | installed, `3.28.3` |
| Ninja | installed, `1.11.1` |
| `nm` | installed at `/usr/bin/nm` |
| Nginx | not installed or not found in `PATH` |

## Native Chess3D Engine Artifact

The temporary P4D1 native build output still exists:

```text
/tmp/chess3d-build/build-linux-clang/libChess3DEngine.so
```

Observed properties:

- Size: about `334K`
- Exported `Chess3D_` symbols: `154`
- This matches the previous P4D1 ABI parity result.

The artifact is still temporary. It is not committed and is not installed into a production server path.

## Server Process / Service State

| Item | Status |
| --- | --- |
| `chessonline.service` | not installed; `systemctl` reports unit not found |
| Kestrel smoke workspace `/tmp/chessonline-smoke` | absent |
| Production server path `/opt/chessonline` | absent |
| Production data/keyring path `/var/lib/chessonline` | absent |
| Port `5077` | no listener observed |
| Port `80` | no listener observed in the probe |
| Port `443` | occupied by an existing non-ChessOnline process named like `xray-linux-*` |

The existing port `443` listener is a deployment constraint. Do not overwrite or stop it without a separate operator decision.

## What Is Ready

- Native Linux build toolchain is present.
- The Linux `libChess3DEngine.so` from P4D1 is still available.
- The host can run `dotnet` and ASP.NET Core 8 applications.
- A Kestrel-only temporary smoke is safe to attempt next, using `/tmp/chessonline-smoke` and port `127.0.0.1:5077`.

## What Is Missing

- Linux server package is not currently staged on the host.
- No persistent ChessOnline system user or production layout exists yet.
- No `systemd` service exists.
- Nginx is not installed/configured for ChessOnline.
- External/public HTTP health is not proven.
- TLS/domain status is unknown.
- Remote SignalR/auth/matchmaking/Asgard smoke is not yet proven.

## Safe Next Step

Next phase: complete the local `linux-x64` server package using the tested temporary `libChess3DEngine.so`, then run a temporary Kestrel smoke on `<HETZNER_HOST>` without touching `/opt/chessonline`, `/var/lib/chessonline`, Nginx, TLS, or systemd.
