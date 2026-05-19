# Project Status

Last audited locally for P2A single-side 3D chess rule contracts.

## Exists

- `Chess.sln` with separate native engine, GPU backend, optional CUDA backend, WPF apps, online hub, Rubik app, and benchmark projects.
- `src/ChessEngine`: native 2D chess rules/search DLL.
- `src/ChessGpuBackend`: native GPU ABI and routing DLL with CPU/Direct3D fallback and optional CUDA dynamic loading.
- `src/ChessCudaBackend`: optional CUDA backend project. It is present in the solution but not built by default.
- `src/Chess3DEngine`: native 8x8x8 cube chess engine DLL.
- `src/RubikEngine`: native Rubik-layer engine DLL.
- `src/ChessApp`: 2D chess WPF app.
- `src/Chess3DApp`: separate 3D chess WPF app.
- `src/RubikApp`: separate Rubik WPF app.
- `src/ChessOnlineApp`: separate online integrations hub.
- `src/Chess2DBenchmark`: 2D chess benchmark console executable.
- `tools/release/Build-Production.ps1`: central production packaging script.
- `tests/run-tests.ps1`: contract-test runner for native engine and GPU ABI smoke checks.
- `rude-resource/`: local ignored read-only resource archive.
- `docs/CHESS3D_SINGLE_SIDE_AUDIT.md`: factual audit of the current 3D engine before six-side generalization.
- `docs/CHESS3D_SINGLE_SIDE_RULES_SPEC.md`: formal P2A single-side 8x8x8 ruleset specification.
- `src/ChessApp/Assets/Rules3D/single_side_3d_chess_8x8x8_v0_1.json`: machine-readable P2A rules asset.

## Build-Verified

- `Release|x64` solution build works without requiring CUDA Toolkit MSBuild integration.
- The default solution configuration intentionally skips `ChessCudaBackend`; CUDA remains an optional backend built separately.
- Packaging creates `ProductionOutput` folders for all user-facing products.
- `scripts/verify.ps1` runs release packaging plus contract tests.
- Contract tests cover `ChessEngine.dll`, `Chess3DEngine.dll`, `RubikEngine.dll`, and `ChessGpuBackend.dll`.
- GitHub Actions `Windows Build` is green on `master`.
- CI verifies a clean checkout, Release x64 build, production packaging, contract tests, `Chess2DBenchmark --quick`, and the baseline without CUDA.
- Chess3D contract tests now include P2A single-side setup, movement, capture, promotion, and JSON metadata smoke checks.

## User Executables

Development build outputs:

- `src/ChessApp/bin/x64/Release/net8.0-windows/ChessApp.exe`
- `src/Chess3DApp/bin/x64/Release/net8.0-windows/Chess3DApp.exe`
- `src/RubikApp/bin/x64/Release/net8.0-windows/RubikApp.exe`
- `src/ChessOnlineApp/bin/x64/Release/net8.0-windows/ChessOnlineApp.exe`
- `bin/x64/Release/Chess2DBenchmark.exe`

Portable outputs:

- `ProductionOutput/Chess2D/ChessApp.exe`
- `ProductionOutput/Chess3D/Chess3DApp.exe`
- `ProductionOutput/Rubik/RubikApp.exe`
- `ProductionOutput/ChessOnlineIntegrations/ChessOnlineApp.exe`
- `ProductionOutput/Chess2DBenchmark/Chess2DBenchmark.exe`

## Works

- Native 2D chess DLL builds and is copied to the 2D app output.
- Native GPU routing DLL builds and is copied to 2D/3D app and benchmark output.
- Native 3D chess and Rubik DLLs build and are copied to their apps.
- Root launch scripts target `ProductionOutput` and trigger packaging when needed.
- `rude-resource/` is ignored by Git.
- `rude-resource/` is a local ignored resource archive and is absent on CI.
- `scripts\verify.ps1` checks the ignore rule through the probe path `rude-resource/.verify-ignore-probe`, so CI does not require the archive directory to exist.
- Native contract tests run without UI, CUDA, or `rude-resource/`.
- `Chess2DBenchmark --quick` is part of the contract-test runner when the benchmark executable exists.
- Single-side 3D ruleset `single-side-3d-chess-8x8x8-v0.1` is documented and covered by ABI-level contract tests.

## Draft

- Six-sided 3D chess laws are still draft and JSON-driven.
- 3D king safety, check, mate, and stalemate remain draft after P2A.
- Rubik layer turns as legal chess actions remain future work.
- 3D relay/web-platform contract is a documented client-side foundation, not a hosted production service.
- Rubik arbitrary-state solving beyond trusted move history remains future work.
- UI smoke tests are still manual.

## Optional

- `ChessCudaBackend.dll` is optional. If built separately and placed next to `ChessGpuBackend.dll`, it can be loaded dynamically.
- CUDA runtime DLL is copied into portable output only when the CUDA backend DLL exists and a local `cudart64*.dll` is discoverable.

## Known Risks

- Full UI automation tests are not present yet.
- GPU parity/performance needs more benchmark baselines on real target hardware.
- The project currently relies on local Visual Studio/MSBuild and vcpkg environment availability.
- Recommended next stage is P2B: generalize the single-side local rules to six sides and six home faces.
