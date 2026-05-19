# Testing

## Run Everything

Use the main verification script:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

It builds `Release|x64`, creates portable output, runs native contract tests, and runs `Chess2DBenchmark --quick`.

The same baseline is now covered by the green GitHub Actions `Windows Build` workflow on `master`. CI runs from a clean checkout, builds `Release|x64`, creates production packages, runs the contract tests, runs `Chess2DBenchmark --quick`, and validates the no-CUDA baseline.

To run only the contract-test layer:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```

## Contract Tests

- `tests\ChessEngineContractTests`: calls `ChessEngine.dll` through its public C ABI.
- `tests\Chess3DEngineContractTests`: calls `Chess3DEngine.dll` through its public C ABI.
- `tests\RubikEngineContractTests`: calls `RubikEngine.dll` through its public C ABI.
- `tests\GpuBackendContractTests`: calls `ChessGpuBackend.dll` through its public C ABI.

Each test executable prints `PASS`/`FAIL` lines and returns exit code `0` only when all assertions pass. The tests are native console executables and do not require WPF or any UI session.

## What They Guarantee

- Exported C ABI functions are present, callable, and stable enough for the frontends.
- Basic 2D chess rules and state transitions still work.
- Draft 3D chess state, board, setup, move, rotation, and rules JSON ABI calls still work.
- P2A single-side 3D chess contracts cover `single-side-3d-chess-8x8x8-v0.1`, the 16-piece central 4x4 setup, movement vectors, blocking, captures, invalid moves, promotion smoke, and rules JSON metadata.
- P2B Chess3D profile tests validate the four profile JSON files under `assets/rules/profiles`, including Asgard/Meru convergence and Rubik convergence metadata.
- Rubik size, state, rotation, scramble, reverse-history solve, and manual-state ABI calls still work.
- GPU backend CPU/Auto paths work without CUDA, and Direct3D/CUDA absence is handled as non-fatal where appropriate.

## What They Do Not Guarantee

- They are not a full chess engine correctness suite.
- They do not prove search strength or GPU performance.
- They do not validate final six-sided 3D chess laws; P2A validates only the single-side local rule core.
- They do not prove full 3D king safety, checkmate, or stalemate yet.
- They do not implement or prove runtime centerAssembly, anchoring, knockback/reserve, or ritual Rubik layer-turn legality yet.
- They do not automate WPF UI behavior yet.
- They do not require or validate `rude-resource/`.

`rude-resource/` is a local ignored resource archive and is absent on CI. Verification checks the ignore rule through the probe path `rude-resource/.verify-ignore-probe` without creating or requiring the archive.

## CUDA

CUDA is optional. Contract tests must pass without `ChessCudaBackend.dll`. If CUDA is built and placed next to `ChessGpuBackend.dll`, the GPU backend may use it, but absence of CUDA is not a test failure.

The next recommended testing milestone is P2C: runtime centerAssembly mechanics, anchor state, target slots, and victory detection.

## UI Smoke Tests

UI smoke tests are currently manual. The next useful layer is a small launcher/screenshot check for `ChessApp.exe`, `Chess3DApp.exe`, `RubikApp.exe`, and `ChessOnlineApp.exe`.
