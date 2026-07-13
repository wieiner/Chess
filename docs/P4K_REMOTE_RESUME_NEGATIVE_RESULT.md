# P4K Remote Resume Authority Boundary Result

Date: 2026-07-13

## Result

The public Hetzner Asgard resume authority-boundary smoke passed through the bounded C# watchdog.

- run ID: `p4k-phase17-asgard-negative-r2-20260713`;
- profile: `asgard-convergence-3d-8x8x8-v0.1`;
- active match after one accepted server-preview action: `match-4-asgard` / `table-4`;
- authoritative state hash: `e8443902f01a9450`;
- server sequence: `1`;
- scenario result: `SMOKE PASS`;
- smoke duration: approximately 1.85 seconds;
- watchdog process duration: approximately 3.15 seconds.

## Negative probes

The deployed server returned explicit, safe protocol results:

| Probe | Result | Expected authority behavior |
| --- | --- | --- |
| seated player B requests player A seat | `playerNotInTable` | rejected |
| unknown room | `tableNotFound` | rejected |
| unknown table | `tableNotFound` | rejected |
| wrong expected ruleset | `rulesetMismatch` | rejected |
| authenticated but unseated temporary user | `playerNotInTable` | rejected |

Failure text was non-empty and contained no stack trace shape. Tokens, passwords, authorization headers, and connection tokens were not printed.

## Stale-state behavior

The current runtime treats resume as authoritative reconciliation:

- a stale `LastKnownStateHash` does not produce `staleState`; the server returns success with the current authoritative snapshot/hash;
- an old `LastKnownServerSeq=0` returns success plus the complete available action-log tail;
- this behavior is safe and useful for reconnect catch-up, but it is now documented explicitly rather than described as a rejection.

The `OnlineResumeFailureReasons.StaleState` constant exists in the protocol, but the current in-memory `RequestResumeMatch` path does not emit it.

## No-mutation proof

After all failed and reconciliation probes:

- state hash remained `e8443902f01a9450`;
- snapshot action count remained unchanged;
- public diagnostics `acceptedActionCount` was `4` before and after the probes;
- the server remained healthy and responsive.

The first diagnostic run stopped early because the smoke tool's stack-trace detector treated the safe English phrase `seated at this table` as a stack frame. The detector was corrected to match newline/`System.`/method-frame shapes, the tool rebuilt with zero warnings/errors, and the full remote run then passed. This was a tooling false positive, not a server failure.

Raw logs remain below ignored `.tmp/remote-ux-smoke/` and are not committed.
