# P4K Phase 15 Recovery Note

Date: 2026-07-13

## Recovered state

The worktree was intentionally inspected without reset, checkout, clean, or stash. `HEAD` and `origin/main` both pointed to `20a897b27b9bb362824a9587fa3aae032860c0c1` (`P4K phase 14: verify deployment rollback readiness`).

Two Phase 15 files were present:

- modified `tools/HetznerSignalRSmoke/Program.cs`;
- untracked `scripts/deploy/Test-HetznerOnlineUx.ps1`.

No unrelated tracked changes and no untracked runtime/log artifacts were reported by Git. `.tmp/` remains ignored.

## Work preserved

The interrupted implementation already contained the beginning of scenario-based remote smoke support for `play`, `resume`, `spectator`, `lobby`, and `all`, plus a bounded wrapper using the existing C# process watchdog. That work was retained and completed in place.

The finished implementation adds:

- strict scenario, profile, URL, run-id, and timeout validation;
- capability gates for `RequestResumeMatch`, `JoinSpectator`, and `RequestLobbySnapshot`;
- resume state/hash/sequence/action-log assertions;
- lobby checks before matchmaking and after the table starts;
- spectator read-only rejection checks and a live action broadcast check;
- deterministic `all` ordering and final diagnostics;
- sanitized failure output without exception stack dumps;
- unique per-run/per-scenario stdout and stderr paths;
- redacted wrapper tails and sequential tool builds.

## Verification completed during recovery

- PowerShell parser accepted `Test-HetznerOnlineUx.ps1`.
- `HetznerSignalRSmoke` built in Release with zero warnings and zero errors.
- dry-run completed for all five scenarios without build or network access;
- an invalid C# `--scenario` was rejected with exit code 1 before network access;
- no reset or destructive cleanup was used.

Remote scenario execution is deliberately deferred to P4K Phases 16-19 after this tooling commit reaches green CI.
