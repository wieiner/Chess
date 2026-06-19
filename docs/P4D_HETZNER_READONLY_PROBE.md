# P4D Hetzner Read-Only Probe

P4D phase 05 performed a read-only SSH probe against the configured Hetzner VPS target. No package install, deploy, service change, production path write, database write, or secret material collection was performed.

## Probe safety

- SSH used the configured private key path, but no private key content was printed or copied.
- A temporary known-hosts file under the local temp directory was used for the probe.
- This document intentionally omits the raw SSH command target, IP address, host key, and full raw log.
- No repository-tracked secret, token, certificate, store, keyring, or runtime database was created.

## Environment summary

| Item | Result |
| --- | --- |
| OS | Ubuntu 24.04.4 LTS (Noble Numbat) |
| Kernel | Linux 6.8.0-117-generic |
| CPU architecture | x86_64 |
| RAM | 3.7 GiB total, about 3.0 GiB available during probe |
| Swap | none configured |
| Root disk | about 75 GiB total, about 65 GiB available during probe |
| Docker/overlay presence | overlay mounts were present in `df -h`; no Docker actions were taken |

## Tool availability

| Tool | Probe result |
| --- | --- |
| `dotnet` | missing |
| `clang++` | missing |
| `cmake` | missing |
| `ninja` | missing |
| `nginx` | missing |

## Can native build be attempted now?

No. The VPS is reachable and has enough free disk/RAM for a small native build probe, but the required build/runtime tools are not installed yet.

A Linux-native `libChess3DEngine.so` build should wait until a separate build environment step installs or otherwise provides:

- .NET SDK/runtime appropriate for the server target;
- `clang`/`clang++` or `build-essential`;
- `cmake`;
- `ninja-build`;
- `git` if source clone is used;
- optionally `nginx` only for later reverse-proxy/TLS deployment phases, not for the first Kestrel smoke.

## P4D status after probe

- Local Windows-hosted Linux cross-build remains blocked by missing local sysroot.
- Hetzner native build is also blocked until the build environment is prepared.
- Next safe phase is a documented build environment plan/template, not an automatic install.
