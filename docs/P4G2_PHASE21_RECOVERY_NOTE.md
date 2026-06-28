# P4G2 Phase 21 Recovery Note

Date: 2026-06-28

## Recovery Context

Codex stopped during Phase 21 before the five-profile online coverage work was committed. The working tree was intentionally preserved; no `git reset`, stash, or cleanup was applied before auditing the state.

## Dirty Tree

Recovered intended Phase 21 changes:

- `docs/NEXT_ERA_MICRO_RESEARCH_LOG.md`
- `tools/HetznerSignalRSmoke/Program.cs`
- `docs/P4G2_FIVE_PROFILE_ONLINE_COVERAGE_MATRIX.md`

No unrelated tracked code changes were present during recovery. Runtime logs and manual-smoke files remain ignored under `.tmp` and were not added to source control.

## Recovered Work

The smoke tool change handles `single-side-3d-8x8x8-v0.1` as a one-player matchmaking profile. That profile can return `MatchFound` on the first `JoinMatchmaking` call instead of first returning `MatchmakingJoined`, which is the correct one-player flow for the training profile.

The coverage matrix records the exact five real Chess3D profiles:

- `classic-six-side-3d-8x8x8-v0.1`
- `single-side-3d-8x8x8-v0.1`
- `asgard-convergence-3d-8x8x8-v0.1`
- `rubik-convergence-3d-8x8x8-v0.1`
- `hodge-projection-duel-3d-8x8x8-v0.1`

Scenario, playthrough, and regression JSON files are not counted as modes.

## Smoke Results Recovered From Phase 21

- Classic remote smoke passed with `action-source=server-preview`.
- Asgard remote smoke passed with `action-source=server-preview`.
- Single-side startup/snapshot smoke passed after the one-player matchmaking fix.
- Rubik startup/snapshot smoke passed with action submit intentionally skipped.
- Hodge startup/snapshot smoke passed with action submit intentionally skipped.

## Verification Needed Before Commit

- Build `tools/HetznerSignalRSmoke`.
- Re-run or confirm sequential remote smoke for the five profiles, avoiding parallel executions that share the same `.tmp` stdout/stderr files.
- Run targeted online contract tests.
- Run `git diff --check` and `tests/run-tests.ps1 -List`.

