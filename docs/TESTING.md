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
- `tests\ChessOnlineContractTests`: exercises the managed P3E online protocol/domain layer against `Chess3DEngine.dll`.
- `tests\ChessOnlineSignalRContractTests`: starts the P3F hosted server in-process and exercises the SignalR transport against the P3E authority registry.

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
- P4C phase 10 extends visual asset tests with a disabled generated-piece manifest smoke: it must parse, declare source/license/size policy, avoid absolute paths, avoid private/temp file markers, and remain lightweight.
- `scripts/verify.ps1` checks that representative Asgard, Rubik convergence, and Hodge Projection Duel profiles plus all P2K smoke and P2L playthrough scenario descriptors are copied into `Chess3DApp` output and `ProductionOutput/Chess3D`.
- `scripts/verify.ps1` also checks that `Assets/Models/piece_sets.json`, representative OBJ/MTL assets, and the generated-piece example manifest are copied into Chess2D and Chess3D development output and portable `ProductionOutput`.
- P4C phase 13 extends `scripts/verify.ps1` with source-level checks for generated asset pipeline docs, product presentation docs, deployment decision docs, the online authority adapter doc, matchmaking durability docs, and Asgard deepening docs.
- P4C phase 14 extends the same source checks to cover the P4D Linux-native authority plan, Clang/Linux toolchain plan, Hetzner build probe plan, and draft CMake toolchain file.
- `scripts/verify.ps1` bounds the direct MSBuild pass with `CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT` (default `4`) to reduce local resource contention without reducing test coverage.
- Rubik size, state, rotation, scramble, reverse-history solve, and manual-state ABI calls still work.
- GPU backend CPU/Auto paths work without CUDA, and Direct3D/CUDA absence is handled as non-fatal where appropriate.
- P3E online authority tests validate protocol roundtrip/rejection, exact five-profile catalog, room/table/seat flows, server-side action validation, stale-hash resync, snapshots, action-log chunks, action-log replay hash equality, and online fixture parsing.
- P3F SignalR tests validate local hosted startup/health, hub protocol rejection, room/table/seat fanout, duplicate seat races, accepted action broadcasts, wrong actor rejection, stale hash resync, reconnect snapshots, Rubik/Hodge profile actions, malformed/oversized message handling, diagnostics without secrets, and SignalR fixture parsing.

## What They Do Not Guarantee

- They are not a full chess engine correctness suite.
- They do not prove search strength or GPU performance.
- They do not validate final six-sided 3D chess laws; P2A validates only the single-side local rule core.
- They now prove the P3A Classic/Single-Side king-safety kernel through focused check/checkmate/stalemate fixtures, but they are not a deep exhaustive endgame tablebase.
- They prove P2E CoreCell stack storage, P2F non-destructive fusion descriptors, P2G home-or-reserve capture routing, P2H runtimePartial Rubik layer turns for projected board plus whole CoreCell stacks, P2I action-history/reserve-restore contracts, P2J Hodge projected composite move contracts, and P3D profile-aware AI candidate/search/apply smoke contracts. They do not prove destructive implosion behavior, contested anchor scoring, or deep AI/search strength.
- They do not implement or prove color/permutation, destructive transformation, final Volume-Surface 216 mechanics, online serialization, GPU stack snapshots, final Hodge mathematical formalism, or tournament-strength AI.
- They do not automate WPF UI behavior yet.
- They do not require or validate `rude-resource/`.

`rude-resource/` is a local ignored resource archive and is absent on CI. Verification checks the ignore rule through the probe path `rude-resource/.verify-ignore-probe` without creating or requiring the archive.

## CUDA

CUDA is optional. Contract tests must pass without `ChessCudaBackend.dll`. If CUDA is built and placed next to `ChessGpuBackend.dll`, the GPU backend may use it, but absence of CUDA is not a test failure.

The next recommended testing milestone is P2N: save/load/replay/export/import tests over P2I/P2J/P2K/P2L action history, playthrough descriptors, and P2M visual asset metadata.

## P3E Online Authority Tests

`ChessOnlineContractTests` covers:

- protocol envelope roundtrip and future-field tolerance;
- wrong protocol, unknown message type, malformed/oversized message rejection;
- room creation, room join, table creation, seat claiming, duplicate-seat rejection, ready/start;
- wrong actor rejection and stale hash `ResyncRequired`;
- accepted Classic normal move;
- Rubik layer-turn acceptance only in the Rubik profile;
- Hodge projected composite acceptance through engine candidates;
- snapshot savegame hash roundtrip;
- authoritative online action-log replay to the same final hash;
- online fixture JSON under `assets\rules\scenarios\chess3d\online`.

`scripts/verify.ps1` also checks that representative online protocol/profile/scenario assets are copied to `ChessOnlineApp` development output and `ProductionOutput\ChessOnlineIntegrations`.

## P3F Hosted SignalR Tests

`ChessOnlineSignalRContractTests` covers:

- in-process Kestrel startup and clean shutdown;
- `/healthz/live`, `/healthz/ready`, and `/chess3d/diagnostics`;
- SignalR `Hello` and reconnect session-token smoke behavior;
- room/table/seat authority through the hub;
- accepted Classic action broadcast to the table group;
- wrong actor rejection and stale hash resync;
- Rubik layer turn and Hodge composite action acceptance through existing registry/engine paths;
- duplicate seat and parallel submit concurrency checks;
- malformed/oversized message rejection without leaking exception details;
- SignalR fixture JSON under `assets\rules\scenarios\chess3d\signalr`.

The suite has no UI dependency, no internet dependency, no CUDA dependency, and leaves no hosted server process running after completion.

## P4A Identity / Persistence Tests

`ChessOnlineSignalRContractTests` also starts an auth-required hosted server with temporary JSON store and Data Protection key-ring paths. It verifies:

- register issues protected access and refresh tokens;
- diagnostics do not expose tokens or password hashes;
- anonymous mutating commands are rejected when auth is required;
- authenticated envelopes cannot spoof another `playerId`;
- authenticated room/table/start/action flow still reaches the P3E authority registry;
- refresh token works, logout revokes it, and rejected refresh is stable;
- the JSON store persists account, session, and accepted action-log event records.

Identity and persistence fixture descriptors live under `assets\rules\scenarios\chess3d\identity` and `assets\rules\scenarios\chess3d\persistence`. They are not new game modes.

`scripts\verify.ps1` checks representative identity/persistence descriptors in development and portable output, and rejects generated runtime stores, key files, certificates, token files, and other secret-like artifacts in `ProductionOutput`.

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

## P3B Visual Playability Checks

P3B remains mostly UI/manual because it is WPF visual feedback rather than native rule logic. Automated coverage comes from:

- Release build of `ChessApp` and `Chess3DApp`;
- existing Rubik four-turn roundtrip fixture;
- existing Hodge blocked-mirror rollback fixture;
- existing invalid-click no-mutation fixture;
- contract tests proving layer turns, projection moves, stacks, fusion, replay, and save/load remain stable.

Manual visual QA is listed in `CHESS3D_MANUAL_VISUAL_SMOKE_CHECKLIST.md`.

## P3C Visual RC Testing

P3C keeps CI headless and avoids fragile pixel tests. Automated confidence comes from:

- Release build of `ChessApp` and `Chess3DApp`;
- existing legal-preview, save/load, replay, state-hash, Rubik, Hodge, stack/fusion, and king-safety contract tests;
- regression fixtures for invalid click no-mutation, Rubik four-turn roundtrip, Hodge blocked mirror rollback, Asgard stack/fusion/anchor, and Classic king-safety.

Manual QA is required for:

- background and piece contrast;
- camera presets and readability;
- whether CoreCube/fusion/stack overlays obscure pieces;
- Rubik layer pre-highlight timing;
- Hodge arrow readability;
- replay-step flash clarity.

Use `docs\CHESS3D_VISUAL_RC_MANUAL_QA.md` for the release-candidate checklist.

## P3D AI/Search Testing

P3D remains headless in CI. Automated coverage includes:

- AI candidate generation for all five real Chess3D RuleProfiles;
- no-mutation checks for candidate build and search using state hash/action count;
- Classic/Single-Side search over king-safe legal moves;
- Asgard reserve-restore candidate visibility;
- Rubik layer-turn candidate visibility;
- Hodge projected composite candidate/search/apply behavior;
- runnable regression fixtures under `assets\rules\scenarios\chess3d\regression`.

The tests intentionally verify integration and legality boundaries, not playing strength.

## P3D.1 Search Hardening Testing

P3D.1 adds regression coverage for the strengthened search loop:

- iterative depth-2 Classic search with no state-hash or action-history mutation;
- clean node-limit stop reporting with the previous state preserved;
- deterministic candidate ordering for Single-Side and profile smoke paths;
- summary JSON v2 fields including completed depth, nodes, qnodes, cutoffs, stopped reason, and compact best-action text;
- budget-gated quiescence-lite smoke around tactical capture/recapture-style positions;
- Asgard anchor/fusion/reserve evaluation smoke without stack mutation;
- Rubik layer-turn ordering and four-turn state consistency;
- Hodge macro-player search and all-or-nothing timeout/no-partial-apply behavior.

The P3D.1 tests still do not claim tournament strength, opening-book quality, GPU search, or online AI authority.

## P4B Matchmaking And Deployment Testing

`ChessOnlineSignalRContractTests` now also covers:

- anonymous matchmaking rejection on an auth-required server;
- authenticated matchmaking join/status/cancel-shaped responses;
- duplicate queue ticket rejection;
- exact-profile match-found creation;
- Asgard matchmaking room/table start and legal action acceptance;
- parsing of matchmaking, Asgard-online, and deployment scenario descriptors.

`scripts\verify.ps1` additionally checks that OnlineServer development output and `ProductionOutput\ChessOnlineServer` include the P4B scenarios, production sample config, and deploy templates.

## P4C Consolidation Checks

P4C keeps the CI gate conservative:

- no new RuleProfile count is introduced;
- Windows server packaging remains verified;
- Linux/Hetzner docs remain planning-only until a Linux-native authority exists;
- generated 3D assets use descriptor-first validation, not tracked heavy meshes;
- presentation and deployment docs are checked as source artifacts;
- `ProductionOutput` is scanned for database, token, key, certificate, and runtime secret-like files.

## P4D1.4 Test Runner and Server TFM Notes

P4D1.4 decomposes `tests/run-tests.ps1` into selectable suites with controlled MSBuild parallelism and per-test executable timeouts. The old `-SkipBenchmark` command remains compatible, but it now reports selected tests, build/run timing, timeout values, and log paths under `.tmp/test-logs`.

Useful examples:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Native -SkipBenchmark
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Online -SkipBenchmark -OnlineTestTimeoutSeconds 180
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only SignalR -SkipBenchmark -OnlineTestTimeoutSeconds 60
```

Server-side managed projects now target `net8.0`; WPF applications remain `net8.0-windows`. See `docs/P4D1_SERVER_TFM_CLEANUP.md`, `docs/P4D1_TEST_RUNNER_DECOMPOSITION.md`, and `docs/P4D1_LOCAL_MSBUILD_STABILITY.md`.

## P4D1.4 Timeout Hotfix

The test runner now uses file-backed stdout/stderr redirection for test executables and has a global timeout cap. This prevents online/SignalR test hangs from trapping PowerShell inside pipe reads after a killed process.

## P4D1.4 Reliable Test Watchdog

`tests/run-tests.ps1` now runs test executables through the C# watchdog at `tools/TestProcessWatchdog`. Use `-Only SignalR` or `-Suite Online` for bounded online diagnostics. Logs are written under `.tmp/test-logs`.

Examples:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List -SkipBenchmark
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only SignalR -SkipSolutionBuild -SkipTestBuild -SkipBenchmark -OnlineTestTimeoutSeconds 60 -GlobalTimeoutSeconds 120
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Online -SkipBenchmark -OnlineTestTimeoutSeconds 180 -GlobalTimeoutSeconds 420
```

Do not use the old PowerShell process wrapper as the authoritative timeout mechanism.

## Next Era Runner Operations

For local work, prefer `pwsh`:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Native -SkipBenchmark -MSBuildMaxCpuCount 1
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Online -SkipBenchmark -OnlineTestTimeoutSeconds 180 -MSBuildMaxCpuCount 1
```

Use `docs/NEXT_ERA_TEST_RUNNER_OPERATIONS.md` for the current operator playbook. Stale-process cleanup is inspect-first: run `scripts\diagnostics\Find-StaleBuildProcesses.ps1`, then use `Stop-StaleBuildProcesses.ps1.template -ConfirmStop` only after reviewing the candidate list.
