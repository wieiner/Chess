# Chess3D P3D.1 Search Hardening Audit

P3D.1 starts from commit `7c1dcb1`, where Chess3D received a first profile-aware AI/search layer. The goal of this audit is to describe the current implementation boundary before hardening it.

## Existing AI Action Kinds

The AI layer uses the existing action kind ids instead of inventing a new mode:

- `ActionMove` for normal legal piece moves and captures;
- `ActionLayerTurn` for Rubik ritual layer turns;
- `ActionReserveRestore` for Asgard/Rubik reserve restore;
- `ActionProjectionCompositeMove` for Hodge all-or-nothing projected moves.

`Chess3DAiActionDto` is already exported and stores kind, side/macro-player, move coordinates, restore coordinates, layer-turn fields, primary side, score, flags, and result code. P3D.1 must not change this DTO layout.

## Candidate Generation

Candidate generation is implemented in `buildAiCandidates` in `src/Chess3DEngine/Chess3DEngine.cpp`. It calls `enumerateDiagnosticActions`, converts each `DiagnosticAction` to `Chess3DAiActionDto`, applies the action to a copied `Game`, evaluates the copied state, then sorts candidates deterministically.

The underlying `enumerateDiagnosticActions` is also used by perft/divide. This is a good boundary because it keeps AI candidates aligned with legal profile actions.

## Current Search

The current search is a shallow root loop plus recursive score function:

- depth is clamped to `1..3`;
- root candidates come from `buildAiCandidates`;
- every child is searched on a copied `Game`;
- the recursive score uses a root-perspective maximizing/minimizing test;
- node/time limits are polled with a simple `AiSearchContext`;
- summary JSON is short v0.1 telemetry.

There is no iterative deepening, alpha-beta cutoff counter, quiescence, transposition table, principal variation, completed-depth fallback, or rich stopped reason yet.

## Apply Path

`Chess3D_ApplyAiAction` converts DTO back to `DiagnosticAction` and calls `applyDiagnosticAction`, which routes through existing legal runtime paths:

- `Chess3D_TryMakeMove`;
- `Chess3D_TryMakeProjectedMove`;
- `Chess3D_RotateLayer`;
- `Chess3D_RestoreReservePiece`.

`Chess3D_MakeBestProfileAction` searches first, then applies the selected DTO through the same path.

## No-Mutation Today

Candidate generation and search use copied `Game` values before applying actions. Existing tests assert stable state hash and action count for all five profiles after candidate/search.

Telemetry mutations are allowed: `aiCandidates`, `lastAiSearchSummaryJson`, and `lastAiSearchError` may change. Game-state mutations such as board, CoreCell stacks, fusion, reserve, action history, replay cursor, and state hash must not change.

## Mutation Risks

The main risks are:

- accidental use of `applyDiagnosticAction` on the live `Game` during search;
- a failed apply partially mutating the live game before rollback;
- summary/candidate generation accidentally altering side-to-move instead of a scoped copy;
- time/node limit stopping mid-depth without a coherent result;
- Hodge projected moves partially applying primary/mirrors if the all-or-nothing path is bypassed.

P3D.1 should keep copy-and-apply search rather than introduce live make/unmake.

## Weak Scoring Areas

The v0.1 evaluation is deterministic but shallow:

- material and center proximity are coarse;
- tactical capture/recapture situations are horizon-prone;
- layer turns are scored mostly by resulting static state;
- Asgard anchor/fusion progress is present but not deep;
- Hodge macro-player evaluation is basic;
- no terminal PV or ordered candidate explanation is emitted.

## Limit Handling Gaps

The current search has node/time limits, but it does not distinguish `completed`, `nodeLimit`, `timeLimit`, `noCandidates`, or `error` in a stable way. It also does not return the last completed depth on timeout.

## Safe P3D.1 Improvements

Safe append-only/internal improvements:

- introduce internal `SearchOptions`, `SearchContext`, and `SearchResult`;
- add iterative deepening with previous-depth fallback;
- add root-perspective alpha-beta counters;
- add deterministic action-order scoring without removing candidates;
- add quiescence-lite for bounded tactical normal moves;
- emit summary JSON v2 while preserving existing ABI;
- make the WPF AI panel async so longer searches do not freeze the UI;
- add regression fixtures for no-mutation, ordering, limits, and summary JSON.

## Deferred

The following should not be done in P3D.1:

- external Stockfish/UCI integration;
- GPU search;
- opening books;
- online authority/multiplayer;
- persistent or global transposition table;
- changing `Chess3DAiActionDto` layout;
- changing RuleProfile rules or adding a sixth profile;
- live make/unmake refactor.

