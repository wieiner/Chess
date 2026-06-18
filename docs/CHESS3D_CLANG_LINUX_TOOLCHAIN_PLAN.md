# Chess3D Clang Linux Toolchain Plan

Status: P4C Phase 14 draft.

## Local Audit

The local custom LLVM root requested for the future P4D spike is:

`C:\ll\local\bin`

Detected tools:

| Tool | Status |
| --- | --- |
| `clang.exe` | present |
| `clang++.exe` | present |
| `ld.lld.exe` | present |
| `llvm-ar.exe` | present |
| `llvm-ranlib.exe` | present |

Detected version summary:

`clang version 23.0.0git`, installed under `C:\ll\local\bin`, default target `x86_64-pc-windows-msvc`.

Linux sysroot status:

| Candidate | Status |
| --- | --- |
| `C:\ll\sysroot` | missing |
| `C:\ll\linux-sysroot` | missing |
| `C:\ll\local\sysroot` | missing |
| `SYSROOT` env var | unset |
| `LINUX_SYSROOT` env var | unset |

## Draft Toolchain File

The repository now contains:

`cmake/toolchains/linux-x64-clang-from-windows.cmake`

It sets:

- `CMAKE_SYSTEM_NAME=Linux`;
- `CMAKE_SYSTEM_PROCESSOR=x86_64`;
- local Clang/Clang++ paths under `C:/ll/local`;
- target triple `x86_64-linux-gnu`;
- LLVM `ar`, `ranlib`, and `ld.lld`;
- optional sysroot from `LINUX_SYSROOT`, `SYSROOT`, or `CHESS_LINUX_SYSROOT`;
- `CMAKE_TRY_COMPILE_TARGET_TYPE=STATIC_LIBRARY`.

Without a sysroot the file emits a warning and remains a planning artifact. It should not be treated as proof that Linux linking works.

## Why Sysroot Matters

Clang can accept a Linux target triple from Windows, but the Linux target still needs headers, startup objects, C/C++ runtime libraries, and system libraries for the target environment. The Clang cross-compilation guide recommends using `--sysroot=<path>` so the compiler can find target `bin`, `lib`, and `include` content.

## P4D Acceptance Criteria

P4D can claim Linux-native authority readiness only after:

- a Linux sysroot or native Linux build environment exists;
- `Chess3DEngine` emits a Linux shared library;
- managed loading resolves the Linux native artifact;
- all five RuleProfiles pass state-hash parity tests;
- `scripts/verify.ps1` or a dedicated P4D CI path validates the Linux authority smoke.

## Deferred

- Editing or relying on `C:\ll-fw`.
- Real Hetzner deployment.
- Docker/Kubernetes packaging.
- Managed reimplementation of the native rules authority.

## Primary References

- CMake toolchains manual: https://cmake.org/cmake/help/latest/manual/cmake-toolchains.7.html
- Clang cross-compilation guide: https://clang.llvm.org/docs/CrossCompilation.html
