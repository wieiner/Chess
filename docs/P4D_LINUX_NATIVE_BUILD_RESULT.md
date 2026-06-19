# P4D Linux Native Build Result

P4D phase 07 was evaluated after the local Clang/sysroot probe and the Hetzner read-only probe.

## Result

`libChess3DEngine.so` was not built in this phase.

This is an intentional blocked result, not a hidden build failure. The phase contract allows a Hetzner native build attempt only when the build environment is ready or the user explicitly authorizes installing missing packages. The read-only probe showed that the required tools are missing, and no package installation was authorized in this phase.

## Why the build was not attempted

The target VPS currently lacks:

- `dotnet`;
- `clang++`;
- `cmake`;
- `ninja`;
- `nginx` for later reverse-proxy phases.

`nginx` is not required for the native build itself, but the missing compiler/CMake/Ninja tools are hard blockers.

The local Windows-hosted cross-build path is also blocked because no Linux sysroot was found under the configured candidate paths.

## Prepared command once unblocked

After the build dependencies are installed or otherwise made available on the Linux host, the native build probe should use a temporary workspace, not production paths:

```bash
rm -rf /tmp/chess3d-build
mkdir -p /tmp/chess3d-build
cd /tmp/chess3d-build
git clone https://github.com/wieiner/Chess.git
cd Chess
cmake -S cmake -B build-linux -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build-linux --target Chess3DEngine
file build-linux/libChess3DEngine.so
ldd build-linux/libChess3DEngine.so || true
nm -D build-linux/libChess3DEngine.so | grep Chess3D_
```

If the CMake output path differs, locate the file with:

```bash
find build-linux -name 'libChess3DEngine.so' -print
```

## Classification

| Area | Status |
| --- | --- |
| CMake scaffold | ready from phase 03 |
| Export macro | ready from phase 03 |
| Windows build | green after phase 06 CI |
| Local Windows-to-Linux cross-build | blocked by missing sysroot |
| Hetzner native build | blocked by missing build tools |
| Production deploy | not attempted |

## Next unblock action

Run the build environment plan/template from P4D phase 06 only after explicit approval. Then repeat phase 07 as an actual native build attempt.
