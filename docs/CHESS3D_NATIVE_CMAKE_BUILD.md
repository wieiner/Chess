# Chess3D Native CMake Build

P4D phase 03 adds a CMake scaffold for building the Chess3DEngine native C ABI as a shared library outside the existing Visual Studio project.

## Scope

The existing Windows production build remains `src/Chess3DEngine/Chess3DEngine.vcxproj`. The CMake files are an additive portability scaffold for Linux-native authority work:

- `cmake/CMakeLists.txt` is the CMake entrypoint.
- `cmake/Chess3DEngine.CMakeLists.txt` defines the `Chess3DEngine` shared library target.
- `cmake/toolchains/linux-x64-clang-from-windows.cmake` documents the Windows-hosted Clang path for a future cross-build.

No Chess3D gameplay rules, native DTO layouts, or existing ABI names are changed by this scaffold.

## Export macro

`src/Chess3DEngine/Chess3DEngine.h` now keeps the existing Windows export behavior and adds a Linux visibility path:

- Windows build with `CHESS3DENGINE_EXPORTS`: `extern "C" __declspec(dllexport)`.
- Windows import: `extern "C" __declspec(dllimport)`.
- Linux/macOS-style native build: `extern "C" __attribute__((visibility("default")))`.

This is intentionally append-only from the ABI consumer point of view: existing exported function names and signatures are unchanged.

## Expected outputs

On Windows through the existing Visual Studio project:

- `Chess3DEngine.dll`

On Linux through CMake:

- `libChess3DEngine.so`

The CMake target uses the same `Chess3DEngine.cpp` and `Chess3DEngine.h` source files as the Visual Studio project.

## Windows host configure

If CMake and a suitable generator are available on Windows, a host configure can be attempted with:

```powershell
cmake -S cmake -B build\cmake-chess3d-host -A x64
cmake --build build\cmake-chess3d-host --config Release --target Chess3DEngine
```

This is a scaffold validation path only. The repository's authoritative Windows build remains `Chess.sln` through MSBuild and `scripts\verify.ps1`.

## Windows-hosted Linux cross-build

The draft toolchain file is:

```powershell
cmake -S cmake -B build\linux-x64-chess3d `
  -G Ninja `
  -DCMAKE_TOOLCHAIN_FILE=cmake\toolchains\linux-x64-clang-from-windows.cmake `
  -DCHESS_LINUX_SYSROOT=C:\path\to\linux-sysroot
cmake --build build\linux-x64-chess3d --target Chess3DEngine
```

P4D phase 02 found the local LLVM tools under `C:\ll\local\bin`, but no Linux sysroot in the configured candidate paths. Without Linux headers and libraries, a Windows-hosted Linux cross-link would be misleading and is expected to fail.

## Hetzner native build path

Because no local sysroot is present, the first trustworthy Linux `.so` validation should happen on the target Linux class of machine:

```bash
cmake -S cmake -B build-linux -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build-linux --target Chess3DEngine
file build-linux/libChess3DEngine.so
ldd build-linux/libChess3DEngine.so || true
nm -D build-linux/libChess3DEngine.so | grep Chess3D_
```

That native build attempt belongs to the later P4D Linux build phase after the read-only Hetzner probe and build environment plan.

## Current blocker

Local Windows-hosted Linux cross-compilation remains blocked by the missing sysroot. This phase only prepares the source/build metadata needed for a real native Linux build.
