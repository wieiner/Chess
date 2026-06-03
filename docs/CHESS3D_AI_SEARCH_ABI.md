# Chess3D AI Search ABI

P3D adds append-only native exports. Existing ABI remains unchanged. P3D.1 hardens the implementation behind those same exports without changing `Chess3DAiActionDto`.

## Candidate ABI

- `Chess3D_BuildAiActionCandidates(handle, sideOrMacroPlayer)`
- `Chess3D_GetAiActionCandidateCount(handle)`
- `Chess3D_GetAiActionCandidate(handle, index, Chess3DAiActionDto*)`

`sideOrMacroPlayer = 0` means current turn actor. For Hodge, values `1..2` may select a macro-player. For side-turn profiles, values `1..6` may select a side.

## Search / Apply ABI

- `Chess3D_SearchBestAiAction(handle, depth, nodeLimit, timeLimitMs, Chess3DAiActionDto*)`
- `Chess3D_ApplyAiAction(handle, const Chess3DAiActionDto*)`
- `Chess3D_MakeBestProfileAction(handle, depth, nodeLimit, timeLimitMs, Chess3DAiActionDto*)`

Search is non-mutating. Apply commits one action through the existing runtime path. `MakeBestProfileAction` searches and then applies the chosen action.

## Telemetry ABI

- `Chess3D_GetLastAiSearchSummaryJson(handle, buffer, capacity)`
- `Chess3D_GetLastAiSearchError(handle, buffer, capacity)`

The P3D.1 summary JSON reports ruleset id, requested/effective/completed depth, node/time limits, elapsed time, nodes, qnodes, cutoffs, `ttHits`, candidate count, ordered candidate count, stopped reason, error text, best score, and compact best-action details.

## Limits

Depth is clamped to a small v0.1 range. Searches use iterative deepening and return the last completed depth when a deeper search hits a node/time limit. This is an integration and correctness layer, not a tournament-strength engine.
