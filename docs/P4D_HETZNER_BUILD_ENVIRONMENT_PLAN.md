# P4D Hetzner Build Environment Plan

P4D phase 06 prepares the Linux build environment plan after the read-only probe showed that the target Ubuntu VPS is reachable but missing the required build/runtime tools.

This phase does not install packages and does not deploy the ChessOnlineServer.

## Current probe result

The P4D read-only probe found:

- Ubuntu 24.04.4 LTS x86_64;
- enough disk/RAM for a small native build probe;
- no `dotnet` command;
- no `clang++` command;
- no `cmake` command;
- no `ninja` command;
- no `nginx` command.

Therefore P4D phase 07 cannot attempt a native `libChess3DEngine.so` build until the build environment is prepared.

## Proposed package set

For a minimal build/probe host:

```bash
apt update
apt install -y dotnet-sdk-8.0 clang cmake ninja-build git build-essential
```

For a later reverse-proxy deployment rehearsal:

```bash
apt install -y nginx
```

`nginx` is not required for the first native build or Kestrel-only smoke.

## Risks and controls

| Risk | Control |
| --- | --- |
| Running as root | Keep phase 06 as plan/template only. Later install should be explicit and auditable. |
| Package conflicts | Check existing package sources and installed packages before install. |
| Disk usage | Probe showed about 65 GiB free; still run `df -h` before install/build. |
| No swap | Native C++ build is small, but memory should be watched during build. |
| Firewall/service exposure | No public service should be started in build-env phase. |
| Service downtime | No production service exists in P4D; still avoid changing `/opt/chessonline` and `/var/lib/chessonline`. |
| Secrets | Do not copy appsettings secrets, certificates, keyrings, or runtime stores. |

## Recommended manual sequence

1. Re-run a read-only health probe.
2. Install only the build dependencies required for phase 07.
3. Verify versions:

```bash
dotnet --info
clang++ --version
cmake --version
ninja --version
git --version
```

4. Keep source/build work under `/tmp/chess3d-build` or `/root/chess3d-build`.
5. Do not write to `/opt/chessonline` or `/var/lib/chessonline` until a later deploy rehearsal phase.

## Phase gates

- Phase 07 may proceed only after the build tools are present.
- Phase 09 linux-x64 publish may proceed only after target framework/native loader blockers are resolved.
- Phase 10 Kestrel smoke may proceed only after a real Linux package and native authority are available.
