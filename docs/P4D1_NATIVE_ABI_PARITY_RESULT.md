# P4D1 Phase 03 - Native ABI Parity Result

## Scope

Phase 03 verified that the Linux-native `libChess3DEngine.so` built in Phase 02 exports the same required Chess3D C ABI as the Windows `Chess3DEngine.dll`.

No source code change was required: the existing `tools/abi/Compare-Chess3DEngineExports.ps1` already supports `-LinuxLibraryPath`.

## Inputs

- Expected export manifest source: `src/Chess3DEngine/Chess3DEngine.h`.
- Windows library: `bin/x64/Release/Chess3DEngine.dll` from local Release build/verify output.
- Linux library: temporary copy of `/tmp/chess3d-build/build-linux-clang/libChess3DEngine.so` copied from Hetzner into `%TEMP%`.

The Linux `.so` remained an untracked temporary artifact and was not committed.

## Command

```powershell
$so = Join-Path $env:TEMP 'libChess3DEngine-p4d1.so'
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\abi\Compare-Chess3DEngineExports.ps1 -LinuxLibraryPath $so
```

## Result

```text
Expected Chess3D exports: 154
Windows Chess3DEngine.dll: OK (154 Chess3D exports found; 154 required exports present).
Linux libChess3DEngine.so: OK (154 Chess3D exports found; 154 required exports present).
```

## Interpretation

- Required ABI parity is satisfied by name.
- Export order is intentionally not required to match.
- Extra exports would be acceptable only if documented; none were observed by the tool in this run.
- Native DTO layout was not changed.

## Phase result

The Linux native authority boundary now has a real compiled shared library and verified C ABI export parity. The next blocker is the server project target framework/publish graph, not the native Chess3D engine exports.
