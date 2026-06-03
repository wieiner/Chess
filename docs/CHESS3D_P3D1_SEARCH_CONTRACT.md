# Chess3D P3D.1 Search Contract

P3D.1 hardens the P3D profile-aware AI/search layer. It does not add a new game mode and does not change the rules of the five existing Chess3D RuleProfiles.

## Function Contract

- `BuildCandidates` is non-mutating except for AI telemetry/candidate cache.
- `SearchBest` is non-mutating except for AI telemetry.
- `MakeBestProfileAction` mutates only after a selected action is applied through the existing legal apply path.
- `ApplyAiAction` mutates only if the supplied action is legal and successfully applied.
- Failed apply, failed search, timeout before a completed result, and node-limit failure must not mutate game state.
- Timeout or node-limit search returns the last completed iterative-deepening result when one exists. If no depth completed, it fails cleanly with summary JSON and error text.

## State Invariants

Candidate/search calls must leave these unchanged:

- projected 512-cell board;
- CoreCell stacks;
- fusion descriptors;
- reserve counts;
- anchors and victory state;
- action history count;
- replay cursor;
- deterministic state hash.

Allowed telemetry mutations:

- `aiCandidates`;
- `lastAiSearchSummaryJson`;
- `lastAiSearchError`.

## Profile Invariants

- Classic and Single-Side use king-safe legal actions only.
- Asgard can score anchors/fusion/reserve, but search does not perform destructive fusion or implosion.
- Rubik layer turns appear only when `layerTurnProfile.type = ritualTurn`.
- Hodge projected composite moves are all-or-nothing.
- Special actions do not leak into profiles that do not enable them.

## Limit Invariants

- Requested depth is clamped to a documented safe maximum.
- Node and time limits are respected at safe poll points.
- Results are deterministic for the same state/depth/node/time settings when no timeout occurs.
- Summary JSON reports whether search stopped because it completed, hit a node limit, hit a time limit, had no candidates, or encountered an error.

## Deferred

P3D.1 does not implement tournament-strength play, opening books, external engines, GPU search, online authority, or a persistent transposition table.

