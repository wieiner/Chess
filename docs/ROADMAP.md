# Roadmap

## P0 - Repo, Build, Package Stabilization

- Keep GitHub import reproducible.
- Ensure normal `Release|x64` builds without CUDA Toolkit MSBuild integration.
- Keep `rude-resource/` ignored and document asset boundaries.
- Keep release packaging centralized and repeatable.

## P1 - Tests and Rule Contracts

- Native contract smoke tests now cover ChessEngine, Chess3DEngine, RubikEngine, and ChessGpuBackend.
- `tests/run-tests.ps1` builds and runs the contract suite plus `Chess2DBenchmark --quick`.
- `scripts/verify.ps1` now includes contract tests after packaging.
- GitHub Actions `Windows Build` is green on `master` for clean checkout, Release x64 build, production packaging, contract tests, `Chess2DBenchmark --quick`, and the no-CUDA baseline.
- `rude-resource/` remains a local ignored resource archive and is absent on CI; verification checks it through `rude-resource/.verify-ignore-probe`.
- CUDA backend remains optional.
- Next P1 work: expand assertions into deeper rule-contract suites and add UI smoke automation.

## P2 - Formal 3D Chess Rules

### P2A - Single-Side 3D Rules

- Completed: factual audit in `docs/CHESS3D_SINGLE_SIDE_AUDIT.md`.
- Completed: formal ruleset spec `single-side-3d-chess-8x8x8-v0.1` in `docs/CHESS3D_SINGLE_SIDE_RULES_SPEC.md`.
- Completed: machine-readable rules asset `src/ChessApp/Assets/Rules3D/single_side_3d_chess_8x8x8_v0_1.json`.
- Completed: starting 4x4 setup, movement/capture contract, pawn promotion smoke, and JSON metadata tests in `Chess3DEngineContractTests`.

### P2B - Configurable Rule Profiles

- Completed: configurable rule profile architecture in `docs/CHESS3D_RULE_PROFILE_ARCHITECTURE.md`.
- Completed: Asgard/Meru convergence profile as data/spec.
- Completed: Rubik convergence profile as data/spec.
- Completed: classic six-side and single-side profile JSON contracts.
- Completed: profile JSON validation through `Chess3DEngineContractTests`.
- Next six-side implementation work: map single-side local rules to six sides and six home faces.

### P2C - Center Assembly Mechanics

- Implement centerAssembly mechanics.
- Add anchor state and target-slot derivation.
- Add victory detection for allPiecesAnchored, requiredPieceCount, kingOnly, and percentageThreshold variants.

### P2D - Knockback And Reserve

- Implement knockbackCapture.
- Return captured pieces to home slot when possible.
- Add reserve state and reserve restore action.

### P2E - Rubik Turns As Chess Actions

- Implement `layerTurnProfile.type = ritualTurn`.
- Enforce axes, layers, quarter turns, action cost, and turn order.
- Define interaction with king safety, anchors, notation, and replay.

### P3 - Full Six-Side Gameplay And Hybrid Victory

- Later: harden king safety, check, mate, and stalemate for full 3D multiplayer.
- Harden six-side full gameplay and hybrid checkmate/centerAssembly victory.

## P4 - Online Relay Server for `chess3d.relay.v1`

- Build the hosted room/relay service for 3D chess.
- Support six clients per table and bridge groups between six-player tables.
- Formalize sync, move, rotate, chat, reconnect, and authority messages.

## P5 - Asset Pipeline

- Keep OBJ loading now.
- Add future glTF/GLB support.
- Add manifests, validation, scale/origin checks, and missing-asset reports.

## P6 - GPU Benchmark, Parity, Frontier Evaluation

- Expand CPU/Direct3D/CUDA benchmarks.
- Add correctness parity checks across backends.
- Identify where GPU batching really wins and where CPU remains better.

## P7 - Release Packaging and GitHub Actions

- Keep GitHub Actions Windows build verification green.
- Produce zipped portable artifacts.
- Optionally add installer packaging later.
