# Chess3D P3D AI / Search Audit

P3D starts from the P3C visual release-candidate baseline. The engine has exactly five real Chess3D RuleProfiles: Classic Six-Side, Single-Side Training, Asgard Convergence, Rubik Convergence, and Hodge Projection Duel. Scenario, playthrough, and regression JSON files are not modes.

## Existing Action Boundary

- Ordinary 3D piece moves enter through `Chess3D_TryMakeMove`.
- Hodge projected composite moves enter through `Chess3D_TryMakeProjectedMove`.
- Rubik layer turns enter through `Chess3D_RotateLayer`.
- Reserve restores enter through `Chess3D_RestoreReservePiece` / `Chess3D_AutoRestoreReservePiece`.
- Successful runtime actions append `ActionRecord` entries and deterministic notation.

## Legal Action Generation

The engine already has pseudo-legal piece generation and a king-safe `generateLegalMoves` layer for Classic and Single-Side. P2O/P3A added profile-aware diagnostic enumeration through `enumerateDiagnosticActions`, which combines:

- Classic/Single: king-safe moves and captures.
- Asgard: moves plus reserve-restore candidates when reserve is enabled.
- Rubik: Asgard-style actions plus legal layer turns.
- Hodge: all-or-nothing projected composite candidates for the active macro-player.

This diagnostic layer is the safest base for P3D because it already has no-mutation perft/divide coverage.

## State And Mutation Risks

Search must not mutate:

- projected board;
- CoreCell stacks;
- fusion descriptors;
- reserve counts;
- action history;
- replay cursor;
- state hash.

The existing perft implementation copies `Game` before applying diagnostic actions. P3D should use the same copy-and-apply pattern for candidate scoring and shallow search. The live game should mutate only when an explicit `ApplyAiAction` or `MakeBestProfileAction` call succeeds.

## Existing Search

`Chess3D_MakeBestMove` exists, but it is move-only. It uses normal generated moves and material evaluation over the projected board. It is compatible with old ABI and should remain unchanged in signature and basic purpose. P3D adds a separate profile-aware action-search ABI instead of changing this older function into a different semantic surface.

## Profile Isolation

- Classic and Single-Side use king-safe legal actions and can evaluate checkmate/stalemate outcomes.
- Asgard and Rubik keep center/core/fusion/reserve semantics and do not inherit Classic checkmate as their victory condition.
- Rubik action search may include layer turns only when `layerTurnProfile` enables them.
- Hodge action search treats one projected composite move as one action and keeps all-or-nothing rollback behavior.

## Safe P3D Additions

- Append-only AI action DTO and ABI.
- Candidate list generated from existing profile-aware diagnostic actions.
- Shallow deterministic search with node/time limits.
- Static evaluation that combines material, mobility, reserve, anchors, fusion, and profile outcome signals.
- Minimal UI panel to search, apply, and copy the summary.
- Regression fixture descriptors and contract tests for no-mutation and profile isolation.

## Deferred

- Deep optimized AI/search.
- Opening books, transposition tables, quiescence, or neural evaluation.
- Stockfish/external engine integration.
- Online AI authority/synchronization.
- GPU/CUDA search.
- New game modes.
