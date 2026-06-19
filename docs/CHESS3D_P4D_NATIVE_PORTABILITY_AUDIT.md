# Chess3D P4D Native Portability Audit

Status: P4D phase 01 audit-only. No runtime code was changed in this phase.

## Scope

This audit checks whether `Chess3DEngine` and the hosted online authority path can become Linux-native without changing gameplay rules, DTO layouts, action history, save/replay, AI/search, or the five existing Chess3D RuleProfiles.

Files inspected:

- `src/Chess3DEngine/Chess3DEngine.cpp`
- `src/Chess3DEngine/Chess3DEngine.h`
- `src/Chess3DEngine/Chess3DEngine.vcxproj`
- `src/ChessOnlineProtocol/OnlineRulesAuthority.cs`
- `src/ChessOnlineProtocol/OnlineGameSession.cs`
- `src/ChessOnlineProtocol/ChessOnlineProtocol.csproj`
- `src/ChessOnlinePersistence/ChessOnlinePersistence.csproj`
- `src/ChessOnlineServer/ChessOnlineServer.csproj`
- `src/ChessApp/NativeChess3DEngine.cs`

## Summary

`Chess3DEngine.cpp` itself is a promising portability candidate: the audited include set is standard C++ oriented and no direct `windows.h`, `LoadLibrary`, `GetProcAddress`, or Win32 API dependency was found in the engine source.

The current blockers are around the boundary:

- C ABI export macro is Windows-only (`__declspec(dllexport/dllimport)`).
- Native project is only a Visual Studio `.vcxproj` targeting Windows dynamic library output.
- Managed wrappers and server diagnostics hardcode `Chess3DEngine.dll`.
- Online protocol/server/persistence projects target `net8.0-windows`.
- Server package copies only the Windows DLL, not a Linux `.so`.

## Dependency Table

| File | Dependency | Windows-only? | Linux replacement | Risk | Required change |
| --- | --- | --- | --- | --- | --- |
| `src/Chess3DEngine/Chess3DEngine.h` | `__declspec(dllexport)` / `__declspec(dllimport)` in `CHESS3D_API` | yes | `__attribute__((visibility("default")))` for ELF builds, no import decoration on Linux | medium | Introduce cross-platform `CHESS3D_API` macro guarded by `_WIN32` and preserve `extern "C"` ABI names. |
| `src/Chess3DEngine/Chess3DEngine.h` | `#pragma pack(push, 4)` DTO packing | mostly portable but compiler-sensitive | Keep pragma for MSVC/Clang/GCC and add ABI size tests | medium | Do not change DTO layout; add native/managed size parity tests before Linux claim. |
| `src/Chess3DEngine/Chess3DEngine.cpp` | Standard C++ headers only in inspected include set | no direct Windows-only API found | Build with C++20 on Linux | medium | Compile under Clang/GCC to expose latent MSVC-only assumptions; do not infer success without build. |
| `src/Chess3DEngine/Chess3DEngine.vcxproj` | MSBuild VC++ project, `WindowsTargetPlatformVersion`, `PlatformToolset=v143`, `WIN32`, `_WINDOWS`, `_USRDLL`, Windows subsystem | yes | CMake shared-library target or native Linux build file | high | Add non-invasive CMake scaffold that builds `libChess3DEngine.so` while keeping `.vcxproj` as Windows path. |
| `src/ChessOnlineProtocol/OnlineRulesAuthority.cs` | `NativeLibraryName = "Chess3DEngine.dll"` and diagnostics report `WindowsNative` only on Windows | yes | Platform resolver: Windows `Chess3DEngine.dll`, Linux `libChess3DEngine.so`, macOS future `libChess3DEngine.dylib` | high | Add cross-platform native library name/path resolver and diagnostics without changing existing DTOs. |
| `src/ChessOnlineProtocol/OnlineGameSession.cs` | Owns `NativeChess3DEngine` directly | not Windows-only by itself, but tied to current wrapper | Same session interface backed by platform-aware wrapper | medium | Keep `IChessOnlineRulesAuthority`; swap underlying native resolver in a later phase. |
| `src/ChessApp/NativeChess3DEngine.cs` | Many `[DllImport("Chess3DEngine.dll", CallingConvention=Cdecl)]` declarations | yes for Linux loading name | `NativeLibrary.SetDllImportResolver` or a server-specific wrapper/resolver | high | Avoid WPF-wide rewrite; add resolver layer for server path first, keep desktop Windows apps stable. |
| `src/ChessOnlineProtocol/ChessOnlineProtocol.csproj` | `TargetFramework=net8.0-windows`, links `NativeChess3DEngine.cs` from `ChessApp` | yes for Linux publish | `net8.0` if server-side code is separated from WPF-only code | high | Split or conditionalize native wrapper dependency before Linux server publish. |
| `src/ChessOnlinePersistence/ChessOnlinePersistence.csproj` | `TargetFramework=net8.0-windows` despite portable-shaped persistence code | yes by TFM | `net8.0` after protocol dependency is portable | medium | Retarget only after protocol/server native boundary is safe. |
| `src/ChessOnlineServer/ChessOnlineServer.csproj` | `TargetFramework=net8.0-windows`; copies `bin\x64\Release\Chess3DEngine.dll` | yes | `net8.0` plus RID-specific native artifact copy: `libChess3DEngine.so` for Linux | high | Defer until Linux native engine artifact exists or is built in CI/Hetzner probe. |
| `tests/ChessOnlineContractTests/*.csproj` and `tests/ChessOnlineSignalRContractTests/*.csproj` | `net8.0-windows` and hardcoded Windows DLL copy assumptions | yes for Linux CI | platform-specific test path or Windows-only tests plus Linux-native tests | medium | Keep current Windows tests green; add Linux-specific smoke only after `.so` exists. |

## Current Move Generation / Authority Path Impact

The hosted online path calls the existing engine through `NativeChess3DEngine`, then submits profile-aware actions through:

- `NormalMove`
- `HodgeProjectedMove`
- `RubikLayerTurn`
- `ReserveRestore`

This phase does not alter these actions. Linux work must preserve:

- action acceptance/rejection semantics;
- state hash;
- save/load/replay JSON;
- action history and notation;
- profile isolation for Classic, Single-Side, Asgard, Rubik, and Hodge.

## Exact Assumptions To Preserve

- Existing C ABI function names must remain stable.
- Existing native DTO field order, field type, and packing must remain stable.
- Calling convention remains C-style; managed declarations currently use `CallingConvention.Cdecl`.
- RuleProfile count remains exactly five.
- `Chess3DEngine.dll` remains the Windows desktop/server artifact.
- Linux artifact should be `libChess3DEngine.so`.

## Recommended P4D Sequence

1. Phase 02: prove or block local Windows-hosted Linux cross-build by checking sysroot and doing only a minimal probe if sysroot exists.
2. Phase 03: add CMake shared-library scaffold for `Chess3DEngine` without disrupting `.vcxproj`.
3. Phase 04: add cross-platform native loading boundary and diagnostics.
4. Phase 05+: perform read-only Hetzner environment probe, then decide whether native build should happen on Hetzner.

## Non-Classic Mode Isolation

Linux portability must not change gameplay behavior. Asgard, Rubik, and Hodge should continue to flow through their existing profile-gated runtime paths. Any Linux parity test must compare state hashes and accepted/rejected action results against the Windows authority for all five profiles.

## Phase 01 Conclusion

The native engine appears feasible to attempt as a Linux shared library, but not yet proven. The biggest known blockers are build system scaffolding, export macro portability, managed loader naming, `net8.0-windows` project targeting, and missing Linux sysroot for local cross-compilation.
