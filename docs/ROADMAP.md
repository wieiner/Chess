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

### P2C - Asgard Core Physics

- Completed: Asgard core physics specification.
- Completed: `occupancyProfile`, `fusionProfile`, and `corePhysicsProfile` data contracts.
- Completed: profile JSON/schema/tests for exclusive, coreStack, stackFusion, and Volume-Surface 216 future metadata.
- Completed: staged plan for moving from single integer cells to future CoreCell stacks without breaking old ABI.

### P2D - Runtime Profile Selection And Simple Anchors

- Completed: strict runtime RuleProfile loading through `Chess3D_LoadRuleProfileJson`.
- Completed: append-only profile summary ABI getters.
- Completed: profile asset copying into Chess3D build output and `ProductionOutput`.
- Completed: six-side typed target-slot derivation over CoreCube `2..5`.
- Completed: simple anchor projection over the current single-occupancy board.
- Completed: centerAssembly victory detection for `allPiecesAnchored` / `requiredPieceCount` style profiles.
- Deferred: final CoreCell stacks, fusion, contested anchors, knockback/reserve, and Rubik layer turns as legal chess actions.

### P2E - CoreCell Stack Board Model

- Add stack-aware `CoreCell` model for Forbidden Core cells.
- Preserve old 512-int board ABI as a projection.
- Add stack-aware UI and network-safe state export.
- Resolve the six-side 96-pieces-vs-64-core-cells pressure through real co-occupancy instead of P2D projection.

### P2F - Fusion Mechanics And Victory

- Implement fusion entity descriptors.
- Implement stackFusion, pairFusion, color/permutation state, and future implosion hooks.
- Implement requiredFusionCount, requiredCoreStacks, kingQueenFusion, surfaceVolume216Completion, sixGateCoronation, and hybrid victory variants.

### P2G - Knockback And Reserve

- Implement knockbackCapture.
- Return captured pieces to home slot when possible.
- Add reserve state and reserve restore action.

### P2H - Rubik Turns Moving Pieces And Stacks

- Implement `layerTurnProfile.type = ritualTurn`.
- Enforce axes, layers, quarter turns, action cost, and turn order.
- Define interaction with king safety, anchors, CoreCell stacks, notation, and replay.

### P3 - Full Six-Side Gameplay And Hybrid Victory

- Later: harden king safety, check, mate, and stalemate for full 3D multiplayer.
- Harden six-side full gameplay and hybrid checkmate/centerAssembly victory.
- Synchronize profiles, stacks, anchors, fusion, and layer turns in online play.

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
