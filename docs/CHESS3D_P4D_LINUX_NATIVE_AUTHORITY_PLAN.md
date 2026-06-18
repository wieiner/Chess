# Chess3D P4D Linux Native Authority Plan

Status: P4C Phase 14 preparation. This is a feasibility plan, not a completed Linux server port.

## Goal

P4D should produce a Linux-native rules authority path for the hosted Chess3D server without changing gameplay rules or adding a sixth RuleProfile. The current working hosted path remains Windows-native: `ChessOnlineServer` runs against the Windows `Chess3DEngine.dll` authority.

The P4D target is narrower:

- build or otherwise provide a Linux-compatible Chess3D native authority artifact;
- load that artifact from the server on `linux-x64`;
- prove state-hash parity against the Windows authority for all five existing RuleProfiles;
- only then perform a controlled Hetzner runtime probe.

## Current Local Toolchain Finding

Local LLVM tools are present under `C:\ll\local\bin`:

- `clang.exe`
- `clang++.exe`
- `ld.lld.exe`
- `llvm-ar.exe`
- `llvm-ranlib.exe`

The detected Clang target is `x86_64-pc-windows-msvc`. No Linux sysroot was found in:

- `C:\ll\sysroot`
- `C:\ll\linux-sysroot`
- `C:\ll\local\sysroot`
- `SYSROOT`
- `LINUX_SYSROOT`

Therefore a Windows-hosted Linux cross-compile is currently blocked at the sysroot/toolchain-completeness layer. This repository must not claim Linux-native authority readiness until that blocker is removed.

## Proposed Steps

1. Keep `IChessOnlineRulesAuthority` and `IChessOnlineGameSessionFactory` as the authority boundary.
2. Create a Linux build target for the native Chess3D engine that emits a shared library suitable for `linux-x64`.
3. Add platform-aware managed loading that can choose the Windows DLL or Linux shared object without changing DTOs or gameplay semantics.
4. Add state-hash parity tests for Classic, Single-Side, Asgard, Rubik, and Hodge.
5. Add a Linux package smoke only after the native artifact exists.
6. Run a controlled Hetzner probe only after source checks, local verify, and GitHub Actions are green.

## Non-Goals

- No new RuleProfile.
- No gameplay-rule changes.
- No Redis/Azure SignalR/backplane.
- No Kubernetes or Docker orchestration.
- No public ranked matchmaking.
- No committed IP addresses, private keys, certificates, stores, keyrings, or production secrets.

## Primary References

- CMake toolchains manual: https://cmake.org/cmake/help/latest/manual/cmake-toolchains.7.html
- Clang cross-compilation guide: https://clang.llvm.org/docs/CrossCompilation.html
- Microsoft .NET RID catalog: https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
- Microsoft .NET native library loading: https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading
