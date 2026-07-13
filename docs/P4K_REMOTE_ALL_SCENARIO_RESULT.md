# P4K Remote All-Scenario Result

Date: 2026-07-13

## Scope

The integrated operator smoke was run against the diagnostic HTTP deployment at
`http://178.105.220.117` with the Asgard profile. The run used temporary users,
did not print credentials or tokens, and did not change nginx, systemd, firewall,
TLS, port 443, or any neighboring service.

## Command

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Test-HetznerOnlineUx.ps1 `
  -BaseUrl "http://178.105.220.117" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -Scenario all `
  -TimeoutSeconds 420 `
  -NoSecretLog `
  -RunId "p4k-phase20-all-asgard-20260713"
```

## Result

Result: **PASS** in 2.18 seconds.

The observed order was deterministic:

1. public health and capability gate;
2. registration, login, and two SignalR connections;
3. matchmaking and game start;
4. legal action selected from `server-preview` and accepted;
5. snapshot and action-log synchronization;
6. player resume at authoritative hash and sequence;
7. active-table lobby discovery;
8. spectator join and read-only authority checks;
9. second legal action selected from `server-preview` and observed live;
10. final diagnostics.

The first action produced state hash `e8443902f01a9450` at server sequence 1.
The spectator then observed the second accepted action at state hash
`9f9bb519247e6186` and server sequence 2.

## Log Safety

The wrapper created one unique ignored stdout/stderr pair under
`.tmp/remote-ux-smoke`. The stderr file was empty. A scan found zero shared-file
or lock errors. Raw runtime logs remain untracked and are not part of this report.

## Service Evidence

Post-run public checks returned:

- live: `Healthy`;
- ready: `ready`, protocol `chess3d.relay.v1`, profile count 5;
- diagnostics: Linux native authority supported, legal preview/resume/spectator/
  lobby capabilities enabled, and native library `libChess3DEngine.so`.

A read-only scan of the recent `chessonline.service` journal found zero matches
for unhandled exception, persistence error, duplicate sequence, native authority
failure, or permission denied. Only the sanitized count was retained.

## Boundary

This is an operator smoke over HTTP 80, not a production security claim. TLS,
domain configuration, port 443, x-ui/Xray, Outline, Albatronix, Unreal SYServer,
and neighboring services were not modified.
