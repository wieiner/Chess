# P4D1 Phase 01 - Hetzner Build Environment Install

## Scope

Phase 01 removed the first real Linux blocker by installing native build/runtime tools on the Hetzner Ubuntu host. The operation was performed after a fresh read-only check and did not touch production paths such as `/opt/chessonline` or `/var/lib/chessonline`.

Tracked documentation uses `<HETZNER_HOST>` placeholders. No private keys, known_hosts, tokens, passwords, certificates, runtime stores, or raw SSH logs are committed.

## Pre-install read-only check

- OS: Ubuntu 24.04.4 LTS, x86_64.
- Kernel: `6.8.0-117-generic` at probe time.
- Disk: root filesystem about 75G total, about 65G free before install.
- Memory: about 3.7Gi total, about 3.0Gi available.
- Missing before install:
  - `dotnet`;
  - `clang++`;
  - `cmake`;
  - `ninja`;
  - `nginx`.

## Install command

Executed on `<HETZNER_HOST>`:

```bash
apt update
apt install -y dotnet-sdk-8.0 clang cmake ninja-build git build-essential rsync curl ca-certificates
```

This installed the .NET 8 SDK/runtime, clang/LLVM 18, CMake, Ninja, GNU build tools, and transfer/debug helpers. It did not install Nginx; that remains deferred to the explicit Nginx phase after Kestrel/native authority smoke.

## Post-install tool versions

- .NET SDK: `8.0.128`.
- .NET host/runtime: `8.0.28`.
- ASP.NET Core runtime: `8.0.28`.
- `clang++`: Ubuntu clang `18.1.3` targeting `x86_64-pc-linux-gnu`.
- `cmake`: `3.28.3`.
- `ninja`: `1.11.1`.
- `git`: `2.43.0`.
- `rsync`: `3.2.7`.
- `curl`: `8.5.0`.

## Post-install capacity

- Root filesystem: about 75G total, about 63G available after install.
- Memory: about 3.7Gi total, about 3.0Gi available.
- Swap: none.

## Warnings and follow-up

- Ubuntu reported a newer kernel is available: running `6.8.0-117-generic`, expected `6.8.0-124-generic`.
- No reboot was performed in this phase.
- Some service restarts were deferred by the system package tooling; no ChessOnline service exists yet.
- `nginx` remains intentionally missing and should be installed/configured in the Nginx reverse-proxy phase, not during native build bootstrap.

## Phase result

Build/runtime environment is ready for a native Linux `libChess3DEngine.so` build attempt with CMake and Ninja.
