# P4J Online Match UX Final Report

Date: 2026-07-01

## Baseline

Continuation started from:

- branch: `main`
- latest pushed commit before this continuation: `e0dbdf46c P4J phase 20: add online lobby UI`
- Phase 20 CI: `28498399988`, success

This continuation completed:

- `7e8134d77 P4J phase 21: verify online lobby flow`
- `1cef3f334 P4J phase 22: extend online network bug reports`
- `e9eda02b4 P4J phase 23: audit online secret handling`
- `557ef8011 P4J phase 24: verify online match UX locally`
- Phase 25 final docs commit follows this report.

## What Is Now In The Repository

P4J closes the local online match UX loop around:

- legal-preview one-click play;
- realtime resync;
- reconnect status;
- resume request/client state;
- spectator server/client/UI support;
- lobby snapshot server/client/UI support;
- sanitized network bug reports;
- secret/logging audit;
- full local verify.

No new Chess3D RuleProfile was added. The real profile set remains exactly five:

1. `classic-six-side-3d-8x8x8-v0.1`
2. `single-side-3d-8x8x8-v0.1`
3. `asgard-convergence-3d-8x8x8-v0.1`
4. `rubik-convergence-3d-8x8x8-v0.1`
5. `hodge-projection-duel-3d-8x8x8-v0.1`

## Current Public Hetzner Status

Read-only checks on `2026-07-01`:

- `GET /healthz/live`: `Healthy`
- `GET /healthz/ready`: ready JSON, `profileCount=5`, `authEnabled=true`
- `GET /chess3d/diagnostics`: Linux native authority OK, `authorityNativeLibraryName=libChess3DEngine.so`

Current public server supports:

- matchmaking;
- snapshot;
- action log;
- action submit;
- legal preview.

Current public server does not yet expose:

- `RequestResumeMatch`
- `JoinSpectator`
- `RequestLobbySnapshot`

Therefore, public HTTP 80 can still be used for current legal-preview play, but remote resume/spectator/lobby PASS requires a later server package deployment.

## UI

ChessOnlineApp now includes:

- server health/diagnostics panel;
- temporary auth/test-user panel;
- matchmaking and two-window play mode;
- realtime board snapshot viewer;
- legal target highlights and one-click preview action dispatch;
- reconnect/resync controls and status;
- resume current/selected controls;
- spectator read-only mode and report;
- lobby filter/table list and selected table actions;
- network bug report save/copy controls.

## Reports

Local reports are written under ignored `.tmp/manual-smoke`:

- session report;
- spectator report;
- network bug report;
- action-log export.

The P4J network report captures:

- server capabilities;
- supported hub methods;
- reconnect/resync state;
- resume result summary;
- spectator state;
- lobby selected row;
- legal-preview state;
- action-log tail;
- redacted UI event-log tail.

## Security

P4J did not commit:

- access tokens;
- refresh tokens;
- passwords;
- private keys;
- certificates;
- runtime DB/store files;
- keyrings;
- raw smoke reports;
- raw test logs.

HTTP 80 remains diagnostic/dev only. Use temporary users only.

## Verification

Local:

```powershell
git diff --check
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark -MSBuildMaxCpuCount 1
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

Results:

- `run-tests -SkipBenchmark -MSBuildMaxCpuCount 1`: PASS
- `scripts\verify.ps1`: PASS

CI:

- Phase 21 run `28498755380`: success
- Phase 22 run `28499172256`: success
- Phase 23 run `28499455279`: success
- Phase 24 run `28500332129`: success

## Not Touched

P4J did not touch:

- 443/TLS/domain;
- x-ui/Xray;
- Outline;
- Albatronix Docker;
- Unreal SYServer;
- nginx;
- systemd;
- UFW/firewall;
- Chess3D rules;
- Linux native authority ABI.

## Remaining Work

Recommended next steps:

1. Deploy the updated ChessOnlineServer package with `RequestResumeMatch`, `JoinSpectator`, and `RequestLobbySnapshot` to Hetzner HTTP 80 without changing network stack.
2. Re-run public remote spectator/lobby/resume smoke.
3. Improve Rubik/Hodge special action UX.
4. Later: dedicated server or TLS/domain/443 plan after x-ui decision.
5. Later: spectator/reconnect persistence across server restart.

## Result

P4J local/repository scope: PASS.

Public Hetzner current-play scope: legal-preview play remains available; resume/spectator/lobby are implementation-ready but deployment-blocked on the current public server package.
