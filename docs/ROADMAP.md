# Roadmap

## Current Next Era Priorities

Current source of truth after Next Era Phase 12:

1. Confirm domain/DNS and configure TLS/HTTPS for ChessOnlineServer.
2. Enforce HTTPS-safe auth/token behavior before real public accounts.
3. Add deployment rollback, backup/restore, and log rotation runbooks.
4. Reconcile older historical docs that still say Linux-native authority or Classic king safety are blocked/draft.
5. Build Chess2D PGN/SAN and UCI adapter foundations before live portal play.
6. Prove public SignalR over HTTPS and reconnect/resume semantics.
7. Add visual QA automation and later AI/search strength work.

Historical phase sections below are retained for context. When an older "Next" line conflicts with this section or `docs/NEXT_ERA_PROJECT_MAP.md`, the Next Era map wins.

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
- Deferred at P2H: king-safety hardening after rotations, notation/replay, online serialization, UI animation, and GPU stack snapshots. P3D later added profile-aware AI/search candidates.

### P2I - Action History, Notation, And Reserve Restore

- Completed: unified action history for successful moves, Rubik layer turns, and reserve restores.
- Completed: deterministic notation v0.1 for `MOVE`, `LAYER`, and `RESTORE` actions.
- Completed: append-only action-history ABI and C# wrapper/status visibility.
- Completed: legal reserve restore action to matching free home slots for reserve-enabled profiles.
- Completed: auto-restore helper and contract tests for failure/no-mutation cases.
- Completed: GitHub Actions now uploads `ProductionOutput` as a short-retention workflow artifact after successful verification.
- Deferred: full replay/export/import, undo, online serialization, drag/drop reserve inventory, and notation standardization.

### P2J - Hodge Projection Duel

- Completed: separate `hodge-projection-duel-3d-8x8x8-v0.1` RuleProfile.
- Completed: `projectionProfile` schema/data contract.
- Completed: side/face local-frame documentation and Hodge transform ABI.
- Completed: runtime all-or-nothing projected composite move for two macro-players with three projections each.
- Completed: action history/notation for `HPD` composite moves.
- Completed: C# status wrapper for projection mode and last projection error.
- Deferred at P2J: projection-specific UI controls, replay/import/export, online serialization, and full 3D checkmate hardening. P3D later added profile-aware AI/search integration.

### P2K - Playable Control Center

- Completed: Chess3D RuleProfile selector from runtime `Assets/Rules3D/Profiles`.
- Completed: capability summary that keeps Classic, Single-side, Asgard, Rubik, and Hodge visibly separate.
- Completed: mode-aware Common, Asgard/Core, Rubik Layer Turn, and Hodge Projection panels.
- Completed: action-log UI with refresh, copy, and `.ch3dlog` save.
- Completed: scenario smoke descriptors for classic, Asgard, Rubik, and Hodge profiles.
- Deferred: animated layer turns, stack-entry visualization in the 3D board, rich reserve inventory, and replay import.

### P2L - Playability Closure And Legal Action Preview

- Completed: append-only legal action preview ABI and invalid-action reason ABI.
- Completed: current turn kind, current side/macro-player, allowed action mask, and turn summary ABI.
- Completed: UI legal action list, target highlighting from preview entries, invalid-reason text, and mode-specific panel visibility.
- Completed: playability audit and matrix for all five real Chess3D profiles.
- Completed: explicit documentation that no sixth Chess3D RuleProfile exists at P2L.
- Completed: playthrough scenario descriptors for Classic, Single-Side, Asgard, Rubik, and Hodge.

### P2M - Visual Assets, Materials, Lighting, And Click Reliability

- Completed: canonical OBJ/MTL piece asset catalog under `assets/models/chess/pieces`.
- Completed: shared WPF material resolver with readable fallback materials and best-effort MTL/texture support.
- Completed: lighter black-piece fallback material, neutral Chess3D background, and stronger scene lighting.
- Completed: preview-aware click-to-move dispatch and clearer invalid target reasons in Chess3D.
- Completed: packaging/verify checks for model manifests and representative OBJ/MTL assets.

### P2N - Save / Load / Replay / Export / Import

- Build replay/export/import over the P2I action record.
- Decide savegame format for profiles, board, stacks, fusion descriptors, reserve counts, visual asset set id, and history.
- Add notation file export/import once syntax stabilizes beyond v0.1.

### P2O - Product Playability And Rules Correctness Gate

- Completed: formal rule contract for the five existing RuleProfiles.
- Completed: game phase/outcome/turn summary/allowed-action diagnostics.
- Completed: shallow action perft/divide and regression playthrough fixtures.

### P3A - Full 3D King Safety / Check / Mate / Stalemate

- Completed: Classic Six-Side runtime king safety, check, checkmate, and stalemate.
- Completed: Single-Side uses the same legal filter when a king is present.
- Completed: legal action preview, `TryMakeMove`, side legal-action counts, and action perft/divide use king-safe legal actions for Classic/Single-Side.
- Completed: Asgard/Rubik/Hodge outcome isolation remains intact.

### P3B - Visual Playability Sprint Final

- Completed: stack/fusion/contested/anchor overlays.

### P3C - Visual RC / Interaction Polish

- Completed: runtime-only visual options and diagnostics for camera, theme, overlays, and high-contrast pieces.

### P3D - Profile-Aware AI / Search

- Completed: profile-aware candidate generation for Classic, Single-Side, Asgard, Rubik, and Hodge.
- Completed: native search/apply ABI that routes through existing action semantics.
- Completed: P3D.1 iterative deepening, alpha-beta hardening, deterministic ordering, bounded quiescence-lite, and summary JSON v2.

### P3E - Online Serialization And Multiplayer Authority Contract

- Completed: managed protocol DTOs and JSON envelope for `chess3d.relay.v1` / `0.1`.
- Completed: in-process authoritative room/table/seat registry.
- Completed: server-side validation through the existing Chess3D engine action paths.
- Completed: snapshot/resync, action-log chunks, state-hash checks, diagnostics, and online regression fixtures.
- Completed: minimal ChessOnlineApp local authority panel and production packaging checks.
- Deferred: production auth, public matchmaking, durable persistence, anti-cheat completeness, binary protocol, and online-native ABI.

### P3F - Hosted SignalR Transport Prototype

- Completed: local ASP.NET Core `ChessOnlineServer`.
- Completed: SignalR hub at `/chess3d/relay` using the P3E protocol DTOs.
- Completed: hub methods/events for hello, rooms, tables, seats, ready/start, submit action, snapshot, action log, ping, and diagnostics.
- Completed: local reconnect session-token smoke behavior.
- Completed: health endpoints and diagnostics without session-token exposure.
- Completed: in-process SignalR contract tests for startup, protocol rejection, authority flow, reconnect, concurrency, malformed/oversized messages, and fixtures.
- Completed: ChessOnlineApp hosted transport panel and production packaging checks.
- Deferred: production identity, durable sessions, public matchmaking, DB persistence, Redis/Azure SignalR, complete anti-cheat, online replay UX, and spectator UX.

### Next

- P4A: completed production-oriented local identity/session/persistence baseline.
- P4B: reconnect/spectator UX and persisted room/table restore policy.
- P4C: production hosting/backplane/matchmaking decision.
- Completed: Rubik layer-turn pre-animation and input lock.
- Completed: Hodge primary/mirror arrow hints.
- Completed: move/replay action flash and visual diagnostics.

### P3C - Visual Release Candidate Polish

- Completed: explicit visual state machine and mode-specific visual language.
- Completed: camera/readability toggles, high-contrast pieces, and visual diagnostics polish.

### P3D - AI/Search Integration Per Profile

- Completed: append-only profile-aware AI action/candidate/search/apply ABI.
- Completed: search candidates for normal moves, reserve restore, Rubik layer turns, and Hodge projected composite actions.
- Completed: shallow deterministic search summary JSON, C# wrappers, UI panel, and regression fixtures.
- Completed in P3D.1: iterative deepening, alpha-beta hardening, deterministic move ordering, bounded quiescence-lite, summary JSON v2, async WPF search/apply calls, and expanded no-mutation regression fixtures.
- Deferred: tournament-strength evaluation, transposition-table storage, AI/search UI timeline, GPU search, and online AI authority.

### P3 - Full Six-Side Gameplay And Hybrid Victory

- Later: harden king safety, check, mate, and stalemate for full 3D multiplayer.
- Harden six-side full gameplay and hybrid checkmate/centerAssembly victory.
- Synchronize profiles, stacks, anchors, fusion, and layer turns in online play.

## P4 - Production Online Hardening for `chess3d.relay.v1`

- Completed P4A: replace dev-only session smoke with optional authenticated local identity/session baseline.
- Completed P4A: persistent player accounts, durable sessions, Data Protection protected access/refresh tokens, server-derived SignalR player identity, JSON persistence provider, and identity/persistence fixtures.
- Completed P4B: single-server authenticated matchmaking MVP and deploy scaffolding.
- Completed P4C phase 00-14: baseline reports, SignalR CI stabilization, Linux portability decision, online rules authority adapter boundary, Windows server package hardening, Hetzner runbook/planning docs, matchmaking durability policy, five-mode feature matrix, Asgard/Rubik/Hodge/Classic product refresh docs, generated asset pipeline policy, product presentation packet, deployment decision package, consolidated verify checks, and Linux-native authority spike preparation.
- Still deferred: Linux-native authority, Redis/Azure SignalR/backplane, public ranked matchmaking, Kubernetes/Docker orchestration, and production anti-cheat completeness.

### P4D - Linux Native Authority Spike

- Prepared: audited `C:\ll\local\bin` Clang/LLVM and found no Linux sysroot.
- Prepared: drafted a Windows-to-Linux CMake toolchain file without claiming cross-compile success.
- Next: plan Linux `Chess3DEngine` shared-library output, native loading, and state-hash parity tests.
- Next: only run Hetzner probes after source, local verify, and CI are green.

## P5 - Asset Pipeline

- Keep P2M OBJ/MTL catalog validation healthy.
- Add future glTF/GLB support.
- Add deeper scale/origin checks, texture QA, and missing-asset reports.

## P6 - GPU Benchmark, Parity, Frontier Evaluation

- Expand CPU/Direct3D/CUDA benchmarks.
- Add correctness parity checks across backends.
- Identify where GPU batching really wins and where CPU remains better.

## P7 - Release Packaging and GitHub Actions

- Keep GitHub Actions Windows build verification green.
- Produce zipped portable artifacts.
- Optionally add installer packaging later.
## P2N - Save / Load / Replay / Playthrough Runner

P2N is completed in the runtime plan:

- `.ch3dsave` JSON snapshot export/import.
- `.ch3dreplay` JSON action replay import/export.
- Engine-level replay cursor and replay errors.
- Deterministic state hash for diagnostics.
- Runnable headless playthrough JSON for all five Chess3D RuleProfiles.
- Minimal Chess3D UI panel for save/load/replay/hash.

Next:

- P2O: richer visualization and animation.
- P3A: completed Classic/Single-Side king safety/check/mate/stalemate.
- P3B: visual playability sprint final.
- P3C: visual release-candidate polish.
- P3D: AI/search per profile.
- P3D.1: search correctness and strength gate.
- P3E: online serialization.

## P2O - Product Playability And Rules Correctness Gate

P2O is completed in the runtime plan:

- Formal rule contract for the five existing Chess3D RuleProfiles.
- Game phase, game outcome, current turn, allowed-action, and rule-summary ABI.
- P2O originally exposed draft Classic king/check status; P3A now turns that layer into runtime Classic/Single-Side king safety.
- Profile-aware action perft/divide diagnostics with no-mutation state-hash checks.
- UI status hardening for phase, outcome, allowed actions, and invalid/legality reasons.
- Regression playthrough fixtures built on the P2N headless runner.

Next:

- P3B: visual playability sprint final for stacks, fusion, layer turns, Hodge mirrors, and replay/action animation.
- P3C: visual release-candidate polish.
- P3D: AI/search integration per profile.
- P3D.1: search correctness and strength gate.
- P3E: online serialization and multiplayer replay authority.
- P3F: packaging/release polish and manual visual QA automation.

## P3C - Visual Release Candidate Polish

P3C is completed as the visual RC polish stage:

- explicit UI visual state machine;
- mode-specific visual language for Classic, Single, Asgard, Rubik, and Hodge;
- camera/readability toggles, high-contrast pieces, and visual diagnostics polish;
- short animation-controller contract for action/replay/Rubik/Hodge flashes;
- manual visual RC QA checklist.

Next:

- P3D: AI/search integration per profile completed;
- P3D.1: search correctness and strength gate completed;
- P3E: online serialization and multiplayer authority;
- P3F: release packaging polish and optional automated visual smoke capture.

## P3D - AI/Search Integration Per Profile

P3D is completed as the first profile-aware Chess3D search layer:

- AI candidates are generated from existing legal profile actions.
- Classic/Single-Side candidates are king-safe.
- Asgard candidates include legal reserve restore where available.
- Rubik candidates include legal layer turns.
- Hodge candidates include projected composite moves as one all-or-nothing action.
- Search is shallow, deterministic, bounded, and non-mutating until an explicit apply/make call.

## P3D.1 - Search Correctness And Strength Gate

P3D.1 is completed as a hardening pass over the existing search layer:

- iterative deepening tracks requested, effective, and completed depth;
- alpha-beta copy-state search is used beyond the fast depth-1 root path;
- deterministic move ordering keeps candidate summaries and regression output stable;
- bounded quiescence-lite extends tactical capture/restore leaves only when budgets allow;
- summary JSON v2 reports nodes, qnodes, cutoffs, stopped reason, best score, and compact best-action text;
- WPF Search Best and Make AI Move are asynchronous and leave rules in the native engine;
- all five real Chess3D RuleProfiles remain isolated, and no sixth mode is added.

## P3E / P3F / P4A / P4B - Online Authority Path

Completed:

- P3E: online authority protocol and replay-safe server action contract.
- P3F: hosted/local SignalR transport prototype.
- P4A: identity, authenticated sessions, JSON persistence, and no-secret packaging checks.
- P4B: single-server matchmaking MVP, deployment templates, Asgard online playability gate, and packaging checks.

Superseded next steps:

- P4C/P4D Linux portability and remote smoke work are now complete through the Next Era dry-run.
- Current work is public deployment hardening: TLS/domain, HTTPS auth enforcement, rollback, backups, log rotation, public SignalR over HTTPS, and rate limits.

## P4D1.4

P4D1.4 stabilizes the server publish and test pipeline after the Linux-native authority spike:

- controlled MSBuild parallelism instead of bare `/m`;
- decomposed native/managed/online test suites;
- per-test executable timeouts and logs;
- `ChessOnlineServer`, `ChessOnlineProtocol`, and `ChessOnlinePersistence` as `net8.0` server-side projects;
- WPF clients remain `net8.0-windows`.

## Next Era - Linux Native Authority Dry-Run

Completed:

- Linux-native `libChess3DEngine.so` build and ABI parity check.
- `linux-x64` ChessOnlineServer package with canonical `libChess3DEngine.so`.
- Hetzner Kestrel-only loopback smoke.
- Remote authenticated SignalR/Asgard matchmaking/action smoke through SSH local-forward.
- Production-like `/opt/chessonline` and `/var/lib/chessonline` layout with service-user loopback smoke.
- `systemd` unit installed/enabled for loopback Kestrel service.
- Nginx public HTTP reverse proxy with external health/diagnostics smoke.
- TLS/domain status documented as blocked on missing confirmed DNS/domain; public HTTP remains diagnostic-only.

Next:

- TLS/domain hardening;
- remote operator runbook and rollback/backup notes.

## Next Era - Chess2D Portal Integration Path

Audited path:

- Phase A: FEN/PGN export/import hardening for ordinary 2D chess.
- Phase B: UCI-compatible engine adapter as a separate console/process boundary.
- Phase C: Lichess connector hardening with safe token storage and explicit Board API vs Bot API policy.
- Phase D: keep Chess3D custom profiles on ChessOnlineServer; do not force 8x8x8 Asgard/Rubik/Hodge actions into public orthodox chess portals.

Deferred:

- live portal move submission;
- account-token persistence;
- Chess.com interactive play, because the public Published Data API is read-only;
- any public-portal path for Chess3D custom modes.

## Next Era - Stalled Areas Priority

Current priority order after the stalled-area audit:

1. TLS/domain + HTTPS auth enforcement.
2. Deployment rollback, backup, and log rotation.
3. Documentation consistency pass for stale `draft`/`blocked` notes.
4. Chess2D PGN/SAN and UCI adapter.
5. Reconnect/resume and public SignalR smoke over HTTPS.
6. Visual QA automation and screenshot checklist execution.
7. AI/search quality work and anti-cheat policy.
