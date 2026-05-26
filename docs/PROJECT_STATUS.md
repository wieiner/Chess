# Project Status

Last audited locally for P2M visual asset pipeline, lighting, and Chess3D interaction reliability.

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
- `assets/rules/profiles`: profile JSON assets for classic six-side, single-side, Asgard/Meru convergence, Rubik convergence, and Hodge Projection Duel modes.
- `docs/CHESS3D_RULE_PROFILE_ARCHITECTURE.md`: configurable rule-profile architecture.
- `docs/CHESS3D_ASGARD_CONVERGENCE.md`: Asgard/Meru centerAssembly design.
- `docs/CHESS3D_RUBIK_LAYER_TURNS.md`: Rubik layer-turn profile contract.
- `docs/CHESS3D_ASGARD_CORE_PHYSICS.md`: P2C two-zone core physics, occupancy, fusion, and Volume-Surface 216 specification.
- `docs/CHESS3D_ASGARD_CORE_PHYSICS_AUDIT.md`: audit of current single-occupancy board storage and staged refactor path.
- `docs/CHESS3D_P2D_RUNTIME_PROFILE_AUDIT.md`: runtime profile boundary and asset-pipeline audit.
- `docs/CHESS3D_P2D_RUNTIME_PROFILE_SELECTION.md`: strict RuleProfile loader and profile summary ABI notes.
- `docs/CHESS3D_TARGET_SLOTS.md`: six-side typed target-slot projection for the current core.
- `docs/CHESS3D_SIMPLE_ANCHOR_PROJECTION.md`: temporary single-occupancy centerAssembly anchor model.
- `docs/CHESS3D_P2E_CORECELL_STACK_AUDIT.md`: audit of board storage before adding core stacks.
- `docs/CHESS3D_CORECELL_STACK_MODEL.md`: runtime model for Forbidden Core stacks.
- `docs/CHESS3D_P2E_CORECELL_STACK_RUNTIME.md`: implementation behavior for stack-enabled profiles.
- `docs/CHESS3D_CORE_STACK_ABI.md`: append-only stack ABI contract.
- `docs/CHESS3D_CORE_STACK_MOVE_SEMANTICS.md`: move behavior for entering, leaving, and moving within the core.
- `docs/CHESS3D_P2F_FUSION_AUDIT.md`: audit of the boundary between core stacks and fusion descriptors.
- `docs/CHESS3D_FUSION_MODEL.md`: P2F non-destructive fusion descriptor model.
- `docs/CHESS3D_P2F_FUSION_RUNTIME.md`: runtime recomputation and profile-isolation notes for fusion.
- `docs/CHESS3D_FUSION_ABI.md`: append-only fusion ABI contract.
- `docs/CHESS3D_IMPLOSION_PROGRESS.md`: progress-state implosion notes.
- `docs/CHESS3D_FUSION_AND_ANCHORS.md`: relationship between target-slot anchors and fusion descriptors.
- `docs/CHESS3D_P2G_KNOCKBACK_RESERVE_AUDIT.md`: capture/stack/fusion boundary audit before P2G.
- `docs/CHESS3D_KNOCKBACK_RESERVE_MODEL.md`: home-or-reserve runtime model.
- `docs/CHESS3D_KNOCKBACK_RESERVE_ABI.md`: append-only reserve/knockback ABI contract.
- `docs/CHESS3D_CAPTURE_SEMANTICS.md`: classic vs Asgard/Rubik capture behavior.
- `docs/CHESS3D_P2G_KNOCKBACK_RESERVE_RUNTIME.md`: P2G implementation notes.
- `docs/CHESS3D_P2H_RUBIK_LAYER_TURN_AUDIT.md`: audit of the previous rotate-layer boundary.
- `docs/CHESS3D_RUBIK_LAYER_TURN_SEMANTICS.md`: formal layer-turn coordinate transforms.
- `docs/CHESS3D_P2H_RUBIK_LAYER_TURN_RUNTIME.md`: runtime behavior for ritual layer turns.
- `docs/CHESS3D_RUBIK_LAYER_TURN_ABI.md`: append-only layer-turn ABI contract.
- `docs/CHESS3D_RUBIK_LAYER_TURN_COORDINATES.md`: axis-code and invariant notes.
- `docs/CHESS3D_RUBIK_STACK_ROTATION.md`: whole-stack relocation model.
- `docs/CHESS3D_P2I_ACTION_SYSTEM_AUDIT.md`: audit of move/capture/stack/layer-turn action boundaries.
- `docs/CHESS3D_ACTION_SYSTEM.md`: unified Chess3D action record model.
- `docs/CHESS3D_ACTION_HISTORY_AND_NOTATION.md`: notation v0.1 and replay foundation.
- `docs/CHESS3D_RESERVE_RESTORE_MODEL.md`: legal reserve restore behavior.
- `docs/CHESS3D_P2I_ACTION_RUNTIME.md`: runtime integration notes.
- `docs/CHESS3D_ACTION_ABI.md`: append-only action/history/restore ABI.
- `docs/CHESS3D_P2J_HODGE_PROJECTION_AUDIT.md`: audit of side/face/action boundaries before Hodge projection.
- `docs/CHESS3D_HODGE_PROJECTION_DUEL.md`: game design specification for the separate Hodge Projection Duel mode.
- `docs/CHESS3D_FACE_COORDINATE_FRAMES.md`: canonical cube-face local frames.
- `docs/CHESS3D_HODGE_PROJECTION_TRANSFORMS.md`: transform semantics for mirror moves.
- `docs/CHESS3D_HODGE_PROJECTION_RUNTIME.md`: runtime behavior for projected composite moves.
- `docs/CHESS3D_HODGE_PROJECTION_ABI.md`: append-only Hodge projection ABI.
- `docs/CHESS3D_HODGE_PROJECTION_NOTATION.md`: notation v0.1 for `HPD` composite turns.
- `docs/CHESS3D_P2L_PLAYABILITY_AUDIT.md`: audit of profile count, UI visibility, and playability gaps.
- `docs/CHESS3D_RULE_PROFILE_PLAYABILITY_MATRIX.md`: status matrix for the five real Chess3D profiles.
- `docs/CHESS3D_LEGAL_ACTION_PREVIEW.md`: append-only preview ABI.
- `docs/CHESS3D_TURN_CONTROLLER.md`: mode-aware turn/capability ABI.
- `docs/CHESS3D_UI_PLAYABILITY_GUIDE.md`: player-facing control-center guide.
- `docs/CHESS3D_SCENARIO_PLAYTHROUGHS.md`: P2L playthrough descriptors.
- `assets/models/chess/pieces/piece_sets.json`: canonical visual model catalog for Chess2D and Chess3D.
- `assets/models/chess/pieces/default`: canonical local OBJ/MTL chess piece set.
- `docs/CHESS_VISUAL_ASSET_AUDIT.md`: P2M audit of OBJ/MTL, lighting, hit-test, and interaction boundaries.
- `docs/CHESS_MODEL_ASSET_PIPELINE.md`: canonical model asset layout and packaging contract.
- `docs/CHESS_OBJ_MTL_TEXTURE_SUPPORT.md`: supported OBJ/MTL/texture subset and fallbacks.
- `docs/CHESS_VISUAL_THEME_AND_LIGHTING.md`: readable fallback materials, background, and lighting.
- `docs/CHESS3D_INTERACTION_AUDIT.md`: preview-to-action mismatch audit and fix.
- `docs/CHESS3D_CLICK_TO_MOVE_FLOW.md`: player/runtime click-to-move flow.
- `docs/CHESS3D_PLAYABILITY_KNOWN_ISSUES.md`: honest remaining UI/visual limitations.

## Build-Verified

- `Release|x64` solution build works without requiring CUDA Toolkit MSBuild integration.
- The default solution configuration intentionally skips `ChessCudaBackend`; CUDA remains an optional backend built separately.
- Packaging creates `ProductionOutput` folders for all user-facing products.
- `scripts/verify.ps1` runs release packaging plus contract tests.
- Contract tests cover `ChessEngine.dll`, `Chess3DEngine.dll`, `RubikEngine.dll`, and `ChessGpuBackend.dll`.
- GitHub Actions `Windows Build` is green. The default branch is `main`; the workflow also listens to `master` for compatibility with older references.
- CI verifies a clean checkout, Release x64 build, production packaging, contract tests, `Chess2DBenchmark --quick`, and the baseline without CUDA.
- Chess3D contract tests now include P2A single-side setup, movement, capture, promotion, and JSON metadata smoke checks.
- Chess3D contract tests now validate the P2B profile JSON files and schema-level profile fields.
- Chess3D contract tests now validate P2C occupancy/fusion/corePhysics profile fields as data contracts.
- Chess3D contract tests now load P2D RuleProfiles at runtime, check profile summary ABI getters, target slots, simple anchor progress, profile isolation, and centerAssembly victory projection.
- Chess3D contract tests now cover P2E CoreCell stacks, stack ABI, stack projection, stack-aware anchors, and basic entering/core-to-core/leaving-core moves.
- Chess3D contract tests now cover P2F fusion descriptors: disabled profile isolation, single/friendly/royal/contested states, fusion recompute, move integration, implosion progress, and Rubik deferred layer-turn stability.
- Chess3D contract tests now cover P2G knockback/reserve: classic isolation, Asgard home-slot return, reserve fallback, own-piece rejection, outside-to-core no-knockback behavior, core-to-outside capture routing, reset clearing, and Rubik profile loading.
- Chess3D contract tests now cover P2H Rubik layer turns: profile gating, projected board rotation for X/Y/Z conventions, CoreCell stack relocation, fusion recompute, anchor/victory recompute, reserve invariance, four-turn identity, and last-result telemetry.
- Chess3D contract tests now cover P2I action history, deterministic notation, move/capture/layer-turn records, reserve restore, auto-restore, failure no-mutation behavior, and string ABI safety.
- Chess3D contract tests now cover P2J Hodge Projection Duel: JSON/profile validation, profile isolation, macro-player group coverage, face-frame transforms, projected composite moves, all-or-nothing rejection, and classic capture recording.
- Chess3D contract tests now cover P2L playability closure: exactly five real RuleProfiles, non-mutating legal action preview, invalid-action reasons, capability masks, turn summaries, mode isolation, and scenario playthrough JSON parsing.
- Chess3D contract tests now cover the P2M visual model manifest and required OBJ/MTL asset references.
- `scripts/verify.ps1` checks that representative RuleProfile JSON files are copied into Chess3D development output and `ProductionOutput`.
- `scripts/verify.ps1` checks that the canonical model manifest and representative OBJ/MTL files are copied into Chess2D and Chess3D development output and `ProductionOutput`.

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
- Rule profile assets define classic six-side, single-side sandbox, Asgard/Meru convergence, and Rubik convergence modes as data contracts.
- Rule profile assets also define Hodge Projection Duel as a separate two-player triune-projection mode; it is explicitly not Asgard and defaults to exclusive occupancy, classic capture, no fusion, no core physics, and no Rubik layer turns.
- Asgard/Rubik convergence profiles define `coreStack`, `stackFusion`, and `asgardCorePhysics`; P2E implements stack storage and P2F implements runtimePartial fusion descriptors.
- `Chess3DEngine.dll` can load strict RuleProfile JSON through an append-only ABI.
- `Chess3DApp.exe` can load profile-shaped JSON files and shows a compact profile/anchor summary in the status area.
- CenterAssembly victory can be detected through the P2D target-slot model and P2E stack-aware anchor scan.
- Forbidden Core cells can store multiple stack entries for Asgard/Rubik convergence profiles while old APIs still see a projected/top piece.
- CenterAssembly anchors are stack-aware for stack-enabled profiles.
- Fusion descriptors report `single`, `friendlyPair`, `friendlyStack`, `royalPair`, and `contested` state over core stacks without destroying stack entries.
- Implosion progress is exposed as progress state for Asgard/Rubik profiles and remains non-destructive.
- Asgard/Rubik convergence profiles route ordinary outer-field captures through knockback/home-or-reserve semantics.
- Reserve is stored as side/type counts without unique piece ids.
- Chess3DApp status text exposes reserve enabled, knockback enabled, current-side reserve total, and last capture destination.
- Rubik convergence profile enables runtime `ritualTurn` layer actions.
- `Chess3D_RotateLayer` can rotate the projected board and whole CoreCell stacks for Rubik convergence, then recompute fusion, anchors, implosion progress, and compatible victory.
- Asgard/classic/single-side profiles keep ritual layer turns disabled and fail cleanly without mutating board/stack/fusion/reserve state.
- Reserve counts are unaffected by layer turns.
- Chess3DApp status text exposes layer-turn enabled state and last layer-turn result.
- Chess3D maintains an action history for successful moves, Rubik layer turns, and reserve restores.
- Chess3D exposes deterministic notation v0.1 through append-only ABI and C# status wrappers.
- Reserve restore is implemented for reserve-enabled profiles: a piece may return from side/type reserve to a matching free home slot.
- Auto-restore finds the first matching free home slot and fails cleanly if none exists.
- Hodge Projection Duel runtime can transform one primary face move to two mirror projections and apply all three moves as one all-or-nothing composite action.
- Hodge projected composite moves append `HPD` notation and expose projection profile/status through append-only ABI and C# status text.
- `Chess3DApp.exe` now has a playable control center: RuleProfile selector, capability summary, mode-aware Asgard/Rubik/Hodge panels, and visible action log controls.
- `Chess3DApp.exe` now lists legal actions for the selected cell, highlights preview targets, exposes invalid-action reasons, and collapses special panels when the active profile does not enable them.
- `Chess3DApp.exe` now uses preview-aware click dispatch: target clicks must match legal preview entries and Hodge projection clicks use the projection action path instead of a blind normal move.
- Chess2D and Chess3D share the canonical OBJ model catalog and a WPF material resolver with MTL/texture best-effort support.
- Black 3D piece fallback material is readable medium slate/charcoal instead of pure black.
- Chess3D uses a neutral preview background and stronger ambient/key/rim lighting for piece readability.
- There are exactly five real Chess3D RuleProfiles at P2L; scenario smoke/playthrough files are descriptors, not additional modes.
- Chess3D action logs can be copied or saved as `.ch3dlog` text with a `rulesetId` header.
- Chess3D scenario smoke descriptors exist for classic, Asgard, Rubik, and Hodge profiles and are copied to development output and `ProductionOutput`.
- Chess3D scenario playthrough descriptors exist for all five RuleProfiles and are copied to development output and `ProductionOutput`.
- GitHub Actions uploads the generated `ProductionOutput` folder as `Chess-ProductionOutput-windows-x64` after successful verification.

## Draft

- Six-sided 3D chess laws are still draft and JSON-driven.
- 3D king safety, check, mate, and stalemate remain draft after P2L and are tracked for P3A.
- Final Asgard/Meru fusion physics are not implemented yet; P2F provides descriptor/progress state but not destructive transformation, visual effects, or full victory variants.
- Runtime board projection remains one integer piece per cell for compatibility, while stack data exists as a Forbidden Core overlay.
- Full reserve inventory UI, drag/drop restore, restore into core, and restore captures are not implemented yet.
- Rubik layer turns are implemented as runtimePartial ritual actions for Rubik convergence, and P2K exposes basic controls. Animation, replay/import/export, online serialization, AI/search generation, and GPU stack snapshots are still draft.
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
- Recommended next stage is P2N: save/load/replay/export/import over P2I/P2J/P2K/P2L/P2M action history and visual asset metadata, then P2O richer visualization/animation for stack/fusion/reserve/layer-turn/projection state.
## P2N Status

P2N adds reproducibility to the existing five Chess3D modes. No sixth mode was added.

Implemented:

- savegame JSON snapshot ABI and UI;
- replay JSON ABI and UI;
- replay cursor/error state;
- state hash;
- runnable playthrough JSON for Classic, Single-Side, Asgard, Rubik, and Hodge;
- contract coverage for save/load/replay/playthrough roundtrips.

Still deferred:

- online serialization;
- replay import/export standardization beyond v0.1;
- visual replay timeline and animation;
- full 3D check/mate/stalemate.

## P2O Status

P2O hardens the five existing Chess3D modes without adding a sixth RuleProfile.

Implemented:

- formal runtime rule contract documentation for Classic, Single-Side, Asgard, Rubik, and Hodge;
- append-only game phase, game outcome, allowed-action, rule-summary, and last-legality-reason ABI;
- draft Classic king-safety/check status reporting, explicitly not final checkmate enforcement;
- profile-aware action perft/divide diagnostics for legal action generation smoke checks;
- non-mutating perft/divide guarantees verified by state hash;
- Chess3D UI status now surfaces phase, outcome, mode rule summary, turn summary, and side legal-action count;
- regression playthrough fixtures for invalid-click rollback, Rubik four-turn roundtrip, Hodge blocked-mirror rollback, Asgard stack/fusion/anchor state, and Classic turn progression.

Still deferred:

- full 3D king safety/check/mate/stalemate;
- AI/search integration over profile-aware legal actions;
- richer visualization/animation of stacks, fusion, Rubik turns, and Hodge mirrors;
- online serialization and multiplayer authority rules.
