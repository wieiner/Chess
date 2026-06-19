# Chess3D Clang Linux Toolchain Probe

Status: P4D phase 02 local probe.

## Goal

Determine whether the local Windows machine can use `C:\ll\local\bin` to cross-build a Linux shared library for the Chess3D native authority.

This phase does not change gameplay rules, does not add a RuleProfile, does not build a fake Linux artifact, and does not commit binaries.

## Tool Versions

Commands executed:

```powershell
& 'C:\ll\local\bin\clang.exe' --version
& 'C:\ll\local\bin\clang++.exe' --version
& 'C:\ll\local\bin\ld.lld.exe' --version
& 'C:\ll\local\bin\llvm-ar.exe' --version
& 'C:\ll\local\bin\llvm-ranlib.exe' --version
```

Observed:

- `clang.exe`: `clang version 23.0.0git`
- `clang++.exe`: `clang version 23.0.0git`
- default target: `x86_64-pc-windows-msvc`
- installed dir: `C:\ll\local\bin`
- build config: `+assertions`
- `ld.lld.exe`: `LLD 23.0.0`
- `llvm-ar.exe`: `LLVM version 23.0.0git`
- `llvm-ranlib.exe`: `LLVM version 23.0.0git`

## Sysroot Probe

Commands checked:

```powershell
Test-Path C:\ll\sysroot
Test-Path C:\ll\linux-sysroot
Test-Path C:\ll\local\sysroot
Test-Path C:\ll\local\x86_64-linux-gnu
$env:SYSROOT
$env:LINUX_SYSROOT
$env:CMAKE_SYSROOT
```

Observed:

| Candidate | Result |
| --- | --- |
| `C:\ll\sysroot` | missing |
| `C:\ll\linux-sysroot` | missing |
| `C:\ll\local\sysroot` | missing |
| `C:\ll\local\x86_64-linux-gnu` | missing |
| `SYSROOT` | unset |
| `LINUX_SYSROOT` | unset |
| `CMAKE_SYSROOT` | unset |

## Result

Local Windows-hosted Linux cross-build is blocked.

The LLVM tools are present, but there is no Linux sysroot containing the target headers, startup objects, C/C++ runtime libraries, and system libraries. A target triple alone is not enough to produce a trustworthy `libChess3DEngine.so`.

No hello shared-library cross-compile test was attempted because the required sysroot is absent. This avoids creating a misleading "success" around an incomplete target environment.

## Implication For P4D

P4D should continue with:

1. CMake/native shared-library scaffold that is ready for Linux.
2. Cross-platform native loader boundary in managed code.
3. Read-only Hetzner probe to learn whether the VPS can act as a native Linux build host.
4. Hetzner build-environment plan before any install or build attempt.

## What Was Not Done

- No local Linux `.so` was produced.
- No binaries were committed.
- No `C:\ll-fw` files were modified.
- No Hetzner SSH probe was performed in this phase.
- No repository secrets or host-specific runtime artifacts were created.

## Next Decision

Use Hetzner as the likely native Linux build probe path unless a valid local Linux sysroot is provided later.
