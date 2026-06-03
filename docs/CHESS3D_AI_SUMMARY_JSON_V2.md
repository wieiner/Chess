# Chess3D AI Summary JSON V2

P3D.1 extends the AI summary JSON while keeping the existing `Chess3D_GetLastAiSearchSummaryJson` ABI.

The search summary uses:

- `format`: `chess3d-ai-search-summary`;
- `version`: `p3d1-search-summary-v0.1`;
- `rulesetId`;
- `requestedDepth`;
- `effectiveDepth`;
- `completedDepth`;
- `nodeLimit`;
- `timeLimitMs`;
- `elapsedMs`;
- `nodes`;
- `qnodes`;
- `cutoffs`;
- `ttHits`;
- `candidateCount`;
- `orderedCandidateCount`;
- `bestScore`;
- `bestAction`;
- `stoppedReason`;
- `error`.

Features not implemented report stable `0`, `false`, or `null` values rather than changing the schema shape where practical.

