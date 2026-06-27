# P4F Playable Online Client Final Report

Date: 2026-06-27

## Baseline

- Start commit: `34d2e85b370873a47f469affbfbacc1ac2b1ab32`.
- Branch: `main`.
- Baseline CI: GitHub Actions was green before P4F.
- Public HTTP health before implementation:
  - `/healthz/live`: `Healthy`;
  - `/healthz/ready`: ready JSON with `profileCount = 5`;
  - `/chess3d/diagnostics`: Linux native Chess3D authority supported.

## Implemented

P4F adds a hands-on Windows client path for the existing Hetzner HTTP 80 diagnostic deployment.

`ChessOnlineApp` now has:

- server connection panel;
- `Use Hetzner HTTP` preset;
- health and diagnostics checks;
- temporary auth/test-user panel;
- in-memory token/session handling;
- SignalR connection controls;
- profile selector with exactly the five real Chess3D RuleProfiles;
- two-local-client matchmaking flow;
- ready/start/snapshot/action-log controls;
- safe Asgard test action submission;
- sanitized session report export under `.tmp/manual-smoke`.

## Shared Client Layer

Added reusable `src/ChessOnlineClient` components:

- endpoint normalization;
- health/ready/diagnostics client;
- auth client;
- in-memory session;
- SignalR relay client construction;
- redacted client event log.

The command-line smoke tool remains available, and the P4F UI now uses the same server contract shape.

## Server Boundary

P4F did not change Nginx, systemd, UFW, x-ui, Xray, Outline, Unreal, Albatronix, TLS, or port 443.

Current boundary:

- public HTTP 80 is diagnostic/dev only;
- Linux native authority is proven;
- exactly five profiles are exposed;
- TLS/domain/443 remain future work.

## Remote Smoke

Command-line Hetzner smoke passed with:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://<HETZNER_HOST>" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -TimeoutSeconds 180 `
  -NoSecretLog
```

Result summary:

- health passed;
- two temporary users registered and logged in;
- two SignalR clients connected;
- Asgard matchmaking found a room/table;
- game started;
- one safe Asgard action was accepted;
- snapshot/action log were returned.

## Local Verification

Local gates passed:

- `git diff --check`;
- `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List`;
- `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark -MSBuildMaxCpuCount 1`;
- `pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1`.

`verify.ps1` also ran the quick benchmark and production packaging gate successfully.

## Security

P4F does not commit or print:

- access tokens;
- refresh tokens;
- temporary passwords;
- runtime stores;
- keyrings;
- certificates;
- private keys;
- raw SSH logs.

HTTP 80 remains diagnostic-only. Do not use real accounts or passwords before TLS/domain hardening.

## Limitations

- Not production auth.
- No TLS/domain/443.
- No ranked accounts.
- No reconnect/resume/spectator UI.
- Snapshot viewer is compact; it is not yet a full realtime 3D online board.
- Safe action helper is currently Asgard-specific.

## Next

- P4G: full realtime Chess3D online board integration.
- P4H: reconnect/resume/spectator.
- P4E later: TLS/domain/443 on a dedicated server or after the x-ui decision.
- P5: gameplay UX/Asgard deepening.
- P6: Chess2D PGN/UCI/Lichess path.

