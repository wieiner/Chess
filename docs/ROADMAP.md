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
- GitHub Actions `Windows Build` is green for clean checkout, Release x64 build, production packaging, contract tests, `Chess2DBenchmark --quick`, and the no-CUDA baseline. The workflow tracks `main` and keeps `master` for compatibility.
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
- Completed: staged plan for moving from single integer cells to CoreCell stacks without breaking old ABI.

### P2D - Runtime Profile Selection And Simple Anchors

- Completed: strict runtime RuleProfile loading through `Chess3D_LoadRuleProfileJson`.
- Completed: append-only profile summary ABI getters.
- Completed: profile asset copying into Chess3D build output and `ProductionOutput`.
- Completed: six-side typed target-slot derivation over CoreCube `2..5`.
- Completed: simple anchor projection over the current single-occupancy board.
- Completed: centerAssembly victory detection for `allPiecesAnchored` / `requiredPieceCount` style profiles.
- Deferred: CoreCell stack runtime, fusion, contested anchors, knockback/reserve, and Rubik layer turns as legal chess actions.

### P2E - CoreCell Stack Board Model

- Completed: stack-aware `CoreCell` overlay for Forbidden Core cells.
- Completed: old 512-int board ABI preserved as projected/top-piece board.
- Completed: append-only stack ABI for count, entry, push, clear, remove, and projected-piece reads.
- Completed: old `GetPiece`/`SetPiece` compatibility semantics over core stacks.
- Completed: stack-aware anchors and centerAssembly victory.
- Completed: basic move integration for entering core, core-to-core, and leaving core.
- Completed: minimal Chess3DApp status visibility for stack enabled/count/projection.
- Deferred: Rubik layer turns moving stacks.

### P2F - Fusion Mechanics And Victory

- Completed: non-destructive fusion descriptors over CoreCell stacks.
- Completed: friendly pair, friendly stack, royal pair, and contested core-cell state.
- Completed: append-only fusion ABI and fusion kind names.
- Completed: implosion progress state for Asgard/Rubik convergence profiles.
- Completed: contract tests for fusion disabled isolation, move integration, anchor interaction, and deferred Rubik stack rotation stability.
- Deferred: color/permutation state, destructive implosion events, requiredFusionCount victory, surfaceVolume216Completion, sixGateCoronation, and hybrid victory variants.

### P2G - Knockback And Reserve

- Completed: `knockbackCapture` runtime for Asgard/Rubik profiles.
- Completed: captured outer-field pieces return to first matching free home slot when possible.
- Completed: fallback reserve counts by side/type when home slots are blocked.
- Completed: append-only reserve/knockback ABI, C# status wrapper, and contract tests.
- Deferred: reserve restore action, reserve inventory UI, notation, and online serialization.

### P2H - Rubik Turns Moving Pieces And Stacks

- Completed: `layerTurnProfile.type = ritualTurn` runtime for Rubik convergence.
- Completed: projected board rotation for ABI axes `0=Z`, `1=Y`, `2=X`.
- Completed: whole CoreCell stack relocation during layer turns.
- Completed: fusion, anchors, implosion progress, and compatible victory recompute after turns.
- Completed: reserve counts remain unaffected by turns.
- Completed: append-only layer-turn ABI, C# wrapper/status, JSON/schema updates, and contract tests.
- Deferred: king-safety hardening after rotations, notation/replay, online serialization, AI/search generation, UI animation, and GPU stack snapshots.

### P2I - Action History, Notation, And Reserve Restore

- Completed: unified action history for successful moves, Rubik layer turns, and reserve restores.
- Completed: deterministic notation v0.1 for `MOVE`, `LAYER`, and `RESTORE` actions.
- Completed: append-only action-history ABI and C# wrapper/status visibility.
- Completed: legal reserve restore action to matching free home slots for reserve-enabled profiles.
- Completed: auto-restore helper and contract tests for failure/no-mutation cases.
- Completed: GitHub Actions now uploads `ProductionOutput` as a short-retention workflow artifact after successful verification.
- Deferred: full replay/export/import, undo, online serialization, drag/drop reserve inventory, and notation standardization.

### P2J - UI Visualization For Stacks, Fusion, Reserve, And Layer Turns

- Visualize multiple stack entries in one core cell.
- Display fusion/contested/royal/implosion state in the board view.
- Add layer-turn controls, animation, and optional notation/replay display.
- Add safe editor workflows for stack inspection without changing fusion descriptors manually.

### P2K - Replay / Export / Import / Savegame

- Build replay/export/import over the P2I action record.
- Decide savegame format for profiles, board, stacks, fusion descriptors, reserve counts, and history.
- Add notation file export/import once syntax stabilizes beyond v0.1.

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
