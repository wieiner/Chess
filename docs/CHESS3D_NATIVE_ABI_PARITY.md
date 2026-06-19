# Chess3D Native ABI Parity

P4D phase 08 adds an export parity tool for the native Chess3D C ABI.

The goal is simple: when `libChess3DEngine.so` exists, it must export the same required `Chess3D_` C ABI functions declared by `CHESS3D_API` in `src/Chess3DEngine/Chess3DEngine.h`.

## Tool

```powershell
tools\abi\Compare-Chess3DEngineExports.ps1
```

Default inputs:

- expected exports: `src\Chess3DEngine\Chess3DEngine.h`;
- Windows library: `bin\x64\Release\Chess3DEngine.dll`;
- Linux library: `build-linux\libChess3DEngine.so`.

## Expected-manifest mode

Use this when no Linux `.so` has been built yet:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\abi\Compare-Chess3DEngineExports.ps1 -ExpectedOnly
```

This mode prints the required exports parsed from the header. It does not claim that Linux ABI parity passed.

## Windows/Linux comparison

After building both artifacts:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\abi\Compare-Chess3DEngineExports.ps1 `
  -WindowsLibraryPath .\bin\x64\Release\Chess3DEngine.dll `
  -LinuxLibraryPath .\build-linux\libChess3DEngine.so
```

The script tries to use:

- `dumpbin`, `llvm-objdump`, or `llvm-nm` for Windows exports;
- `nm` or `llvm-nm` for Linux exports.

If a library is missing, the script reports that the real comparison was skipped for that side. Missing Linux `.so` is expected until the P4D native build blocker is resolved.

## Current P4D status

- Windows DLL exists in normal Release builds.
- Linux `.so` is not available yet.
- Local Windows-hosted Linux cross-build is blocked by the missing Linux sysroot.
- Hetzner native build is blocked by missing build tools until the build environment plan is explicitly executed.

## ABI constraints

- Do not remove existing `Chess3D_` exports.
- Do not change function signatures or DTO layout for existing exports.
- New native ABI must remain append-only.
- Export order does not matter; required symbol presence does.
