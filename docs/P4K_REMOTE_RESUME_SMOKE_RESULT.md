# P4K Remote Resume Smoke Result

Date: 2026-07-13

## Result

Public Hetzner `RequestResumeMatch` positive smoke passed for both Asgard and Classic over the existing diagnostic HTTP 80 deployment.

The active server remained:

- live: `Healthy`;
- ready: `ready`;
- profile count: `5`;
- Linux native authority: supported;
- `resumeMatch`: `true`;
- build commit: `f33240e87cd39ed6d2cfb7b612a8504c28f85586`;
- package ID: `chessonline-linux-x64-f33240e87cd3`.

No server files, service configuration, nginx, firewall, TLS/443, or neighboring services were changed.

## Asgard

Command scenario: `resume`

- run ID: `p4k-phase16-asgard-20260713`;
- profile: `asgard-convergence-3d-8x8x8-v0.1`;
- action source: `server-preview`;
- accepted action: normal move, one authoritative action-log event;
- active table: `match-1-asgard` / `table-1`;
- state before resume: `e8443902f01a9450`, server sequence `1`;
- resumed seat: `1`;
- state after resume: `e8443902f01a9450`, server sequence `1`;
- action count/history matched the pre-disconnect authoritative state;
- result: `SMOKE PASS`;
- smoke duration: approximately 4.85 seconds (watchdog process duration approximately 9.49 seconds including startup/cleanup).

## Classic

Command scenario: `resume`

- run ID: `p4k-phase16-classic-20260713`;
- profile: `classic-six-side-3d-8x8x8-v0.1`;
- action source: `server-preview`;
- accepted action: `#1 S1 MOVE K (4,4,0)->(3,5,1)`;
- active table: `match-2-classic` / `table-2`;
- state before resume: `679085fef5801b2a`, server sequence `1`;
- resumed seat: `1`;
- state after resume: `679085fef5801b2a`, server sequence `1`;
- action count/history matched the pre-disconnect authoritative state;
- result: `SMOKE PASS`;
- smoke duration: approximately 1.60 seconds (watchdog process duration approximately 3.11 seconds).

## Assertions proved

For both profiles:

- public health and advertised resume capability passed before match creation;
- matchmaking, ready/start, snapshot, server legal preview, accepted action, and action-log request passed;
- the primary authenticated relay disconnected and established a new transport connection;
- `RequestResumeMatch` returned success for the same room/table/ruleset and seat;
- snapshot hash, server sequence, and action count were unchanged by resume;
- the accepted action was present in the authoritative history returned from sequence zero;
- resume itself did not add an action or mutate the board.

Raw stdout/stderr remain under ignored `.tmp/remote-ux-smoke/` and are not committed. Temporary credentials and bearer tokens were not printed.

## Boundary

This is positive in-process match resume. It does not claim resume after a `chessonline.service` restart; restart rehydration remains a later audited gate.
