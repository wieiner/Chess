# P4K WPF Resume Smoke Result

Date: 2026-07-13

## Scope

`ChessOnlineApp` was built in Release/x64 and driven as a real WPF process
against the Hetzner HTTP 80 diagnostic deployment. UI Automation used only
public control patterns and status text; the transient driver and its raw output
remain under ignored `.tmp/manual-smoke` and are not part of the repository.

## User Flow

The verified click path was:

1. open the `3D Relay` tab;
2. select `asgard-convergence-3d-8x8x8-v0.1`;
3. click `Use Hetzner HTTP`;
4. click `Check Health` and observe `Healthy`, ready, and profile count 5;
5. click `Check Diagnostics` and observe supported Linux native authority;
6. click `Create Two Test Players`;
7. click `Create Test Match With Two Local Clients`;
8. click `Ready Both`, `Start Game`, and `Request Snapshot`;
9. click occupied source `(2,2,0)`, observe server legal-preview targets, then
   click legal target `(1,2,0)`;
10. click `Disconnect Primary Relay`;
11. click `Reconnect Primary Relay`;
12. click `Resume Current Match`.

## Result

Result: **PASS** in 10.26 seconds.

- legal preview contained at least one action;
- one-click target dispatch was accepted;
- accepted actions: 1;
- rejected actions: 0;
- state hash before resume: `e8443902f01a9450`;
- state hash after resume: `e8443902f01a9450`;
- action count after resume: 1;
- action counters were unchanged across resume;
- resume refreshed authoritative snapshot/action-log state;
- no `ChessOnlineApp` process remained after the bounded smoke.

## Guard Fix

The first UI run exposed a real presentation-state defect: after manual primary
relay disconnect, submit buttons remained visually enabled even though the
command handler and server would reject the action. The UI state refresh now
requires all of the following before enabling submit commands:

- player mode rather than spectator mode;
- usable connected primary relay;
- current primary seat is allowed to act;
- no submit is already pending.

Disconnect and reconnect transitions now refresh this state immediately. A
second UIA run proved that submit is disabled while disconnected and the normal
resume flow succeeds after reconnect.

## Build and Security

The Release/x64 `ChessOnlineApp` build completed with zero warnings and zero
errors. The targeted `ChessOnlineContractTests` build and executable also passed
under the bounded repository runner (one selected test executable, no timeout).
Only generated temporary users were used. Passwords and tokens remained in
memory and were not printed or written to tracked files. HTTP 80 remains a
diagnostic/development boundary; TLS/443 and neighboring services were untouched.
