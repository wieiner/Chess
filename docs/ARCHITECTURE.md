# Architecture

The repository is split into separate native DLLs and separate user-facing applications. The split is intentional: ordinary chess, cube chess, Rubik, and online integrations have different lifecycles and should be installable independently.

## Native Layers

- `ChessEngine.dll`: ordinary 8x8 chess rules, legal move generation, FEN, draw rules, search, evaluation, and search telemetry.
- `Chess3DEngine.dll`: 8x8x8 cube board state, P2A single-side movement/capture contracts, P2B/P2C profile data contracts, P2D runtime RuleProfile loading, P2E Forbidden Core stack overlay, P2F fusion/implosion descriptors, P2G knockback/reserve capture routing, P2H Rubik layer turns, P2I action history/notation/reserve restore, P2J Hodge projection composite turns, stack-aware centerAssembly anchors, draft six-side setup, and Rubik-style layer rotations for cube chess.
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
- `assets/rules/profiles` contains machine-readable profile contracts. `Chess3DApp` copies them to `Assets/Rules3D/Profiles`, and `ProductionOutput/Chess3D` carries the same runtime profile assets.
- `assets/rules/scenarios/chess3d` contains P2K smoke descriptors. `Chess3DApp` copies them to `Assets/Rules3D/Scenarios`, and `ProductionOutput/Chess3D` carries them for manual QA.
- `assets/models/chess/pieces` contains the canonical P2M OBJ/MTL model catalog for Chess2D 3D-model mode and Chess3D. Both WPF apps copy it to `Assets/Models`; production packaging carries the same catalog into `ProductionOutput/Chess2D` and `ProductionOutput/Chess3D`.
- `ProductionOutput/` is generated portable output and is ignored.

### Visual Rendering Boundary

P2M keeps rendering inside WPF `Media3D`. `ObjModelLibrary` owns OBJ mesh loading, best-effort MTL/texture material resolution, readable fallback materials, and model diagnostics. Chess2D and Chess3D can share the same model catalog without moving game rules into the visual layer.

The visual pipeline intentionally does not implement full PBR. Diffuse `map_Kd` textures are used when they exist locally; missing external textures, normal maps, roughness maps, and PBR metadata fall back safely to the readable palette.

### 3D Rules Boundary

P2A defines one local 3D chess ruleset, `single-side-3d-chess-8x8x8-v0.1`, for one army on the `z=0` home face with forward direction `+Z`. That rule core is documented in `docs/CHESS3D_SINGLE_SIDE_RULES_SPEC.md` and tested through `Chess3DEngineContractTests`.

P2B adds a data-first `RuleSet` architecture for goal, capture, central core, victory, turn, randomization, and layer-turn profiles. The Asgard/Meru convergence idea is split into `mythProfile` for narrative and `goalProfile`/`coreProfile`/`victoryProfile` for gameplay.

P2C adds core physics profile contracts: `occupancyProfile`, `fusionProfile`, and `corePhysicsProfile`. These describe future Asgard behavior where the outer field remains one-piece-per-cell, but the Forbidden Core can allow stacks and fusion states. Runtime board storage is still `512` integer cells, so stack/fusion behavior is currently data/spec only.

P2D adds the first runtime bridge from profile JSON to the engine. `Chess3D_LoadRuleProfileJson` stores the active ruleset id/version/display name, goal/capture/occupancy/fusion/core-physics/layer/victory profile types, core cube bounds, anchor mode, required anchor count, and last profile-load error. It also derives typed target slots for sides 1..6 and computes a simple centerAssembly anchor projection over the existing single-occupancy board.

P2E adds CoreCell stacks inside the Forbidden Core while preserving the old 512-int board as a projection. Old APIs see the top stack entry; new stack ABI functions expose stack count and entries. Anchors now search stack entries instead of only the projected piece.

P2F adds non-destructive `CoreFusionState` descriptors over those stacks. The engine can report friendly pair, friendly stack, royal pair, and contested core state, plus side-level fusion counts and implosion progress. Stack entries remain the source of truth.

P2G adds profile-gated capture routing. Classic profiles still remove captured pieces. Asgard/Rubik convergence profiles route ordinary outer-field captures through `knockbackCapture`: captured pieces first try to return to a matching free home slot and otherwise enter side/type reserve counts. Forbidden Core entries are not knocked back on core entry; they coexist as stacks and fusion descriptors.

P2H adds profile-gated Rubik layer turns for `rubik_convergence_3d_v0_1`. The projected board rotates, whole CoreCell stacks move with their cells, fusion and anchors are recomputed, and reserve counts are preserved. Asgard/classic/single-side profiles keep ritual layer turns disabled.

P2I adds a unified action-history layer over successful moves, Rubik layer turns, and reserve restores. It also adds deterministic notation v0.1 and a legal reserve restore action to matching free home slots for reserve-enabled profiles. Reset/profile load/setup helpers remain outside turn history.

P2J adds a separate Hodge Projection Duel profile. This is a two-macro-player mode where each macro-player has three cube-face projections. A successful projected action validates one primary move plus two mirror moves through documented face-coordinate transforms, then records one `HPD` composite action. The profile defaults to exclusive occupancy, classic capture, no core physics, no fusion, no reserve/knockback, and no Rubik layer turns.

P2L adds a non-mutating legal action preview layer and a lightweight turn/capability controller. The preview ABI derives selectable move/capture/core/reserve/layer/projection entries from existing runtime state, but it does not replace old `GetPieceMoves`, `TryMakeMove`, layer-turn, reserve-restore, or Hodge projected-move APIs. The WPF app uses this as a control-center contract for highlighting and invalid-action explanations.

P2K adds the playable Chess3D control center in the WPF app. It does not move rule logic into UI; it exposes the existing native profile/action ABI through a profile selector, mode-aware status/action panels, action-log controls, and scenario smoke-pack listing.

P2M hardens the WPF side of that boundary. Chess3D target clicks now match exact legal-preview entries before dispatching an action. Normal moves still call `TryMakeMove`; Hodge projected target clicks call the projected-move ABI; Rubik layer turns and reserve restore remain panel-driven actions.

This is still not final Asgard fusion physics. Destructive implosion, color/permutation state, online serialization, AI/search generation for layer turns, UI animation, full replay/import/export, and GPU stack snapshots remain later stages.

The engine still reserves side ids `1..6`. Six-sided chess should be built by mapping this local rule core to each cube face through coordinate transforms, rather than inventing six unrelated movement systems. Hodge Projection Duel now uses those same face-frame transforms for triune mirror moves. Rubik-style ritual turns are runtimePartial board/stack transforms for Rubik convergence; P2I/P2J notation is a replay foundation, while full online sync and search integration remain separate boundaries.

### Online/Integration Boundary

`ChessOnlineApp.exe` owns accounts, portal profiles, read-only platform APIs, ICS-style connections, and future hosted relay integration. The ordinary board app stays focused on chess play/advice instead of account management.

## Why Separate Apps

- 2D chess can remain a stable advisor/game app.
- 3D chess has experimental laws, six-side networking, and cube-layer operations.
- Rubik has a different state model and interaction model.
- Online integrations involve credentials, platform policies, and relay concerns.

Keeping them separate reduces coupling, avoids accidental feature bleed, and allows independent packaging.
## Chess3D Save / Replay Layer

P2N adds a serialization layer above the existing Chess3D runtime:

- `Position::board` remains the projected 512-cell board.
- CoreCell stacks, reserve counts, and action history are serialized explicitly.
- Fusion, anchors, implosion progress, and victory overlays are recomputed after load.
- Replay executes through the same public move/layer/projection/restore entry points used by UI and tests.
- The state hash is diagnostic and deterministic, not a security feature.

The layer is append-only ABI and does not change Chess2D, RubikApp, OnlineApp, CUDA optional behavior, or existing Chess3D exports.

## Chess3D Rules Correctness Gate

P2O adds a profile-aware playability/rules diagnostic layer above the P2N replayable runtime:

- `GamePhase`, `GameOutcome`, current-turn summary, allowed action mask, and mode rule summary are exposed through append-only ABI.
- Classic Six-Side now uses P3A runtime king safety for legal preview, move execution, check, checkmate, and stalemate. Single-Side uses the same filter when a king is present.
- P3B visual overlays live in the WPF layer. They read existing ABI state and do not alter native rule semantics, save/replay formats, or action history.
- Asgard/Rubik centerAssembly outcomes remain separate from checkmate.
- Hodge uses macro-player/projection composite action semantics rather than side-only chess turns.
- `Chess3D_PerftActions` and `Chess3D_DivideActionsJson` enumerate legal actions by profile for shallow diagnostic counts and never mutate the source state.

This keeps rule diagnostics in the native engine while the WPF UI remains a consumer of summaries, previews, and action/replay APIs.

## Chess3D Visual RC Layer

P3C adds an explicit WPF-only visual state layer:

- `Chess3DVisualStateSnapshot` records selection, mode, turn, action, options, overlay count, and animation lock state.
- Runtime visual options change only rendering: background, high contrast, CoreCube overlay, Hodge arrows, and Rubik layer overlay.
- Camera presets and visual diagnostics live in `Chess3DWindow`.
- Native engine state remains authoritative for board, legal preview, stacks, fusion, reserve, action history, save/load/replay, and outcomes.

This layer improves playability without creating a new mode or moving rules into UI code.
