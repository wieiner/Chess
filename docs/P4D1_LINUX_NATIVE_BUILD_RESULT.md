# P4D1 Phase 02 - Linux Native Chess3DEngine Build

## Scope

Phase 02 attempted and completed a real Linux-native build of `libChess3DEngine.so` on the Hetzner Ubuntu host. The build used a temporary workspace only:

- Source upload: `/tmp/chess-p4d1-src.tar`.
- Build workspace: `/tmp/chess3d-build`.
- Production paths were not touched.
- No secrets or runtime stores were copied.

The source snapshot was produced with `git archive HEAD` locally and copied with `scp` because direct `git clone https://github.com/wieiner/Chess.git` from the host could not authenticate non-interactively.

## Build environment

- OS: Ubuntu 24.04.4 LTS, x86_64.
- Compiler: `clang++ 18.1.3`.
- CMake: `3.28.3`.
- Ninja: `1.11.1`.
- .NET SDK available on the host after Phase 01: `8.0.128`.

## Build command

Executed inside `/tmp/chess3d-build`:

```bash
cmake -S cmake -B build-linux-clang -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_CXX_COMPILER=clang++
cmake --build build-linux-clang --target Chess3DEngine -v
```

## Result

- Output: `/tmp/chess3d-build/build-linux-clang/libChess3DEngine.so`.
- Size: about 334K.
- ELF: `ELF64`, x86-64, shared object.
- Dynamic dependencies observed with `ldd`:
  - `libstdc++.so.6`;
  - `libm.so.6`;
  - `libgcc_s.so.1`;
  - `libc.so.6`;
  - `/lib64/ld-linux-x86-64.so.2`.
- Exported `Chess3D_` symbols counted with `nm -D`: `154`.

## Warnings

The clang build completed with warnings only:

- unused fusion/preview constants;
- unused helper functions already present in the Windows-compatible source.

No Linux-only compile blocker was found in the native engine source during this phase.

## Notes

- A first CMake build without `-DCMAKE_CXX_COMPILER=clang++` successfully used GCC through `/usr/bin/c++`; it was discarded as the primary result because P4D1 requested clang.
- The `file` utility was not installed on the host, so ELF details were captured with `readelf` instead of installing an extra package.
- The resulting `.so` remains a temporary Hetzner build artifact and is not committed to the repository.

## Phase result

`libChess3DEngine.so` is now proven buildable on native Linux with clang/cmake/ninja. Next step: compare Linux exports against the expected/Windows Chess3D ABI and then unblock linux-x64 server publish.
