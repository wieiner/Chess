# Testing

## Run Everything

Use the main verification script:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

It builds `Release|x64`, creates portable output, runs native contract tests, and runs `Chess2DBenchmark --quick`.

The same baseline is now covered by the green GitHub Actions `Windows Build` workflow. CI runs from a clean checkout, builds `Release|x64`, creates production packages, runs the contract tests, runs `Chess2DBenchmark --quick`, and validates the no-CUDA baseline. The default branch is `main`; the workflow also listens to `master` for compatibility.

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
- P2C Chess3D profile tests validate `occupancyProfile`, `fusionProfile`, `corePhysicsProfile`, and disabled Volume-Surface 216 metadata.
- P2D Chess3D runtime tests load all four RuleProfile JSON files through `Chess3D_LoadRuleProfileJson`, verify profile summary ABI getters, confirm invalid-profile rollback, validate CoreCube and target slots for sides 1..6, and check simple centerAssembly anchors/victory in projection mode.
- P2E Chess3D runtime tests validate CoreCell stack enablement, stack push/read/clear/remove ABI, `SetPiece` compatibility, reset clearing, projected/top-piece behavior, stack-aware anchors, profile isolation, and basic moves entering, leaving, and crossing the core.
- P2F Chess3D runtime tests validate fusion disabled isolation, fusion descriptor ABI, `single`, `friendlyPair`, `friendlyStack`, `royalPair`, `contested`, side fusion/contested counts, move integration, anchor/fusion interaction, implosion progress, and Rubik deferred stack rotation stability.
- P2G Chess3D runtime tests validate classic capture isolation, Asgard/Rubik reserve and knockback enablement, home-slot return, reserve fallback, own-piece rejection, outside-to-core no-knockback stack entry, core-to-outside capture routing, reserve clearing, reset clearing, and Rubik profile safety.
- P2H Chess3D runtime tests validate Rubik profile layer-turn enablement, Asgard/classic/single-side disabled isolation, projected board rotation for Z/Y/X engine conventions, four-turn identity, CoreCell stack relocation, fusion recompute, fixed-world target-slot anchor recompute, reserve invariance, and last-result telemetry.
- P2I Chess3D runtime tests validate action-history reset behavior, failed-action isolation, move/capture/layer-turn records, deterministic notation, reserve restore, auto-restore, reserve count mutation, profile isolation, and string ABI safety.
- P2J Chess3D runtime tests validate Hodge Projection Duel profile metadata, projection isolation for older profiles, macro-player grouping, side-to-side face-frame transforms, all-or-nothing projected composite moves, capture recording, and deterministic `HPD` notation.
- P2K Chess3D tests validate scenario smoke descriptor JSON for classic, Asgard, Rubik, and Hodge, plus non-Hodge projected-move clean failure/no mutation.
- P2L Chess3D tests validate exactly five real RuleProfiles, legal action preview non-mutation, invalid-action reasons, profile capability masks, turn summaries, mode isolation, and playthrough scenario JSON parsing.
- P2M Chess3D tests validate the canonical visual piece-set manifest, readable black-piece fallback metadata, required OBJ/MTL files for all standard piece types, and default board tile OBJ assets.
- `scripts/verify.ps1` checks that representative Asgard, Rubik convergence, and Hodge Projection Duel profiles plus all P2K smoke and P2L playthrough scenario descriptors are copied into `Chess3DApp` output and `ProductionOutput/Chess3D`.
- `scripts/verify.ps1` also checks that `Assets/Models/piece_sets.json` and representative OBJ/MTL assets are copied into Chess2D and Chess3D development output and portable `ProductionOutput`.
- Rubik size, state, rotation, scramble, reverse-history solve, and manual-state ABI calls still work.
- GPU backend CPU/Auto paths work without CUDA, and Direct3D/CUDA absence is handled as non-fatal where appropriate.

## What They Do Not Guarantee

- They are not a full chess engine correctness suite.
- They do not prove search strength or GPU performance.
- They do not validate final six-sided 3D chess laws; P2A validates only the single-side local rule core.
- They now prove the P3A Classic/Single-Side king-safety kernel through focused check/checkmate/stalemate fixtures, but they are not a deep exhaustive endgame tablebase.
- They prove P2E CoreCell stack storage, P2F non-destructive fusion descriptors, P2G home-or-reserve capture routing, P2H runtimePartial Rubik layer turns for projected board plus whole CoreCell stacks, P2I action-history/reserve-restore contracts, and P2J Hodge projected composite move contracts. They do not prove destructive implosion behavior, contested anchor scoring, or AI/search strength.
- They do not implement or prove color/permutation, destructive transformation, final Volume-Surface 216 mechanics, full replay/import/export, online serialization, AI/search generation for layer turns or Hodge composite turns, GPU stack snapshots, or final Hodge mathematical formalism.
- They do not automate WPF UI behavior yet.
- They do not require or validate `rude-resource/`.

`rude-resource/` is a local ignored resource archive and is absent on CI. Verification checks the ignore rule through the probe path `rude-resource/.verify-ignore-probe` without creating or requiring the archive.

## CUDA

CUDA is optional. Contract tests must pass without `ChessCudaBackend.dll`. If CUDA is built and placed next to `ChessGpuBackend.dll`, the GPU backend may use it, but absence of CUDA is not a test failure.

The next recommended testing milestone is P2N: save/load/replay/export/import tests over P2I/P2J/P2K/P2L action history, playthrough descriptors, and P2M visual asset metadata.

## UI Smoke Tests

UI smoke tests are currently manual. P2M improves click dispatch and visual diagnostics in C# and keeps both WPF apps compiling against the shared OBJ/material loader, but there is still no automated WPF click-through test. The next useful layer is a small launcher/screenshot check for `ChessApp.exe`, `Chess3DApp.exe`, `RubikApp.exe`, and `ChessOnlineApp.exe`.
## P2N Save / Replay Tests

`Chess3DEngineContractTests` now covers:

- valid savegame export;
- transactional invalid save load;
- save/load hash roundtrips for Classic, Single-Side, Asgard, Rubik, and Hodge;
- replay of normal move, Rubik layer turn, Hodge projected move, and reserve restore;
- invalid replay load error handling;
- all five `*_playthrough_v0_1.json` scenario files as runnable headless scripts.

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

## P2O Rules / Perft / Regression Tests

`Chess3DEngineContractTests` now also covers:

- exactly five real Chess3D RuleProfile JSON files, with scenario/playthrough files excluded from mode count;
- game phase, game outcome, mode rule summary, allowed action mask, and current-turn summary ABI;
- draft Classic check status summary and side legal-action counts;
- profile isolation for Classic, Asgard, Rubik, and Hodge action masks;
- `Chess3D_PerftActions` depth 0/1 and `Chess3D_DivideActionsJson` depth 1;
- state-hash no-mutation guarantees for perft/divide;
- regression fixtures under `assets\rules\scenarios\chess3d\regression`.

P2O diagnostics are deliberately small-depth CI checks. P3A extends them so Classic/Single-Side perft/divide count legal king-safe actions rather than pseudo-actions.

## P3A King-Safety Tests

`Chess3DEngineContractTests` now covers:

- side king lookup through runtime check status;
- pawn, rook, bishop/officer, queen, knight, and king attack-map smoke positions;
- self-check rejection and no-mutation state hash behavior;
- king move into attacked cell rejection;
- legal capture of a checking piece;
- legal blocking of a sliding rook check;
- Classic checkmate and stalemate micro positions;
- Single-Side king-safety smoke behavior;
- non-classic outcome isolation for Asgard, Rubik, and Hodge;
- legal action preview and `TryMakeMove` consistency;
- `Chess3D_PerftActions` and `Chess3D_DivideActionsJson` no-mutation checks over legal actions.

The P3A regression fixtures are copied into development output and `ProductionOutput`, then executed by the headless playthrough runner.
