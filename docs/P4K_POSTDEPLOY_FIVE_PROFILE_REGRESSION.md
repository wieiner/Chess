# P4K Post-deploy Five-profile Regression

Date: 2026-07-13

## Scope

All five and only the five tracked Chess3D rule profiles were exercised
sequentially against the Hetzner HTTP 80 diagnostic deployment. Each run used
temporary users, a unique run ID, bounded execution, and ignored/redacted logs.

## Results

| Profile | Match/start | Snapshot/action log | Submitted action | Result |
| --- | --- | --- | --- | --- |
| `asgard-convergence-3d-8x8x8-v0.1` | PASS | PASS, final hash `e8443902f01a9450` | Accepted `NormalMove` selected by `server-preview` | PASS |
| `classic-six-side-3d-8x8x8-v0.1` | PASS | PASS, final hash `679085fef5801b2a` | Accepted king move selected by `server-preview` | PASS |
| `single-side-3d-8x8x8-v0.1` | PASS, one-player queue | PASS, unchanged hash `e3a0ccbe33ae47df` | Intentionally skipped | PASS |
| `rubik-convergence-3d-8x8x8-v0.1` | PASS | PASS, unchanged hash `df5cecc8f5e0c331` | Intentionally skipped; no fabricated layer action | PASS |
| `hodge-projection-duel-3d-8x8x8-v0.1` | PASS | PASS, unchanged hash `668481062bf778bd` | Intentionally skipped; no fabricated projected action | PASS |

Classic preview inspected several occupied sources before finding a submit-ready
legal king move. This is expected source scanning, not a compatibility fallback.
Both submitted actions reported `action-source=server-preview`.

Single, Rubik, and Hodge used `-SkipActionSubmit`. This phase proves their remote
matchmaking/start/snapshot/action-log boundary without misrepresenting a
profile-specific special action as `NormalMove`.

## Evidence Quality

All five wrappers returned exit code 0. Every stdout log ended in `SMOKE PASS`,
and all five stderr logs were empty. Runs were sequential and used distinct log
paths under ignored `.tmp/remote-ux-smoke`.

Post-run public checks returned `Healthy` and ready status with `profileCount=5`.
Diagnostics continued to report supported Linux native authority and
`requestLegalPreview=true`; active connections returned to zero.

## Boundary

No sixth profile or rule change was introduced. The run did not change nginx,
systemd, UFW, port 443, TLS, x-ui/Xray, Outline, Albatronix, Unreal SYServer, or
other neighboring services. HTTP 80 remains a diagnostic/development channel.
