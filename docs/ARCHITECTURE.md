# Architecture

The repository is split into separate native DLLs and separate user-facing applications. The split is intentional: ordinary chess, cube chess, Rubik, and online integrations have different lifecycles and should be installable independently.

## Native Layers

- `ChessEngine.dll`: ordinary 8x8 chess rules, legal move generation, FEN, draw rules, search, evaluation, and search telemetry.
- `Chess3DEngine.dll`: 8x8x8 cube board state, P2A single-side movement/capture contracts, P2B/P2C profile data contracts, draft six-side setup, and Rubik-style layer rotations for cube chess.
- `RubikEngine.dll`: N x N x N Rubik state, layer rotations, scramble/history, and trusted reverse playback.
- `ChessGpuBackend.dll`: stable GPU ABI boundary. It routes work to CUDA when available, otherwise Direct3D/CPU fallback paths.
- `ChessCudaBackend.dll`: optional CUDA backend built from `.cu` kernels. It is dynamically loaded and is not required for the default solution build.

## Applications

- `ChessApp.exe`: 2D chess WPF frontend and 2D/3D board view for ordinary chess.
- `Chess3DApp.exe`: separate cube chess WPF frontend for the 8x8x8 game.
- `RubikApp.exe`: separate Rubik product frontend.
- `ChessOnlineApp.exe`: online integrations hub for external chess portals and future relay/web-platform integration.
- `Chess2DBenchmark.exe`: native benchmark tool for ordinary 2D chess hot paths and GPU backend comparison.

## Boundaries

### P/Invoke Boundary

C# apps call native DLLs through narrow P/Invoke wrappers. Native state and rule logic remain inside the C++ DLLs; WPF owns presentation, input, and workflow.

### GPU ABI Boundary

`ChessGpuBackend.dll` is the stable ABI seen by engines and benchmarks. CUDA-specific implementation remains behind optional dynamic loading of `ChessCudaBackend.dll`. This keeps normal builds and CPU fallback usable on machines without CUDA Toolkit integration.

### Asset Boundary

- `rude-resource/` is local, ignored, read-only historical material.
- `src/.../Assets` contains runtime assets used by apps and copied during build.
- `src/ChessApp/Assets/Rules3D` contains runtime 3D rules JSON assets, including the P2A `single_side_3d_chess_8x8x8_v0_1.json` ruleset.
- `assets/rules/profiles` contains P2B machine-readable profile contracts. These are validated by tests but are not yet full runtime behavior switches.
- `ProductionOutput/` is generated portable output and is ignored.

### 3D Rules Boundary

P2A defines one local 3D chess ruleset, `single-side-3d-chess-8x8x8-v0.1`, for one army on the `z=0` home face with forward direction `+Z`. That rule core is documented in `docs/CHESS3D_SINGLE_SIDE_RULES_SPEC.md` and tested through `Chess3DEngineContractTests`.

P2B adds a data-first `RuleSet` architecture for goal, capture, central core, victory, turn, randomization, and layer-turn profiles. The Asgard/Meru convergence idea is split into `mythProfile` for narrative and `goalProfile`/`coreProfile`/`victoryProfile` for gameplay.

P2C adds core physics profile contracts: `occupancyProfile`, `fusionProfile`, and `corePhysicsProfile`. These describe future Asgard behavior where the outer field remains one-piece-per-cell, but the Forbidden Core can allow stacks and fusion states. Runtime board storage is still `512` integer cells, so stack/fusion behavior is currently data/spec only.

The engine still reserves side ids `1..6`. Six-sided chess should be built by mapping this local rule core to each cube face through coordinate transforms, rather than inventing six unrelated movement systems. Rubik-style layer rotation remains a board transform until P2E implements `layerTurnProfile.type = ritualTurn` as a legal chess action.

### Online/Integration Boundary

`ChessOnlineApp.exe` owns accounts, portal profiles, read-only platform APIs, ICS-style connections, and future hosted relay integration. The ordinary board app stays focused on chess play/advice instead of account management.

## Why Separate Apps

- 2D chess can remain a stable advisor/game app.
- 3D chess has experimental laws, six-side networking, and cube-layer operations.
- Rubik has a different state model and interaction model.
- Online integrations involve credentials, platform policies, and relay concerns.

Keeping them separate reduces coupling, avoids accidental feature bleed, and allows independent packaging.
