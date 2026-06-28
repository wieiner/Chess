# P4G2 Final Manual and Operator Test Result

Date: 2026-06-28

## Local Gate

Commands:

```powershell
git diff --check
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark -MSBuildMaxCpuCount 1
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

Results:

- `git diff --check`: PASS.
- `run-tests.ps1 -List`: PASS.
- `run-tests.ps1 -SkipBenchmark -MSBuildMaxCpuCount 1`: PASS.
- `scripts\verify.ps1`: PASS, including build, production packaging, contract tests, SignalR tests, and quick benchmark.

## Remote Hetzner Operator Smoke

Base URL:

```text
http://178.105.220.117
```

Security boundary:

- temporary users only;
- `-NoSecretLog`;
- no tokens or passwords committed;
- no `.tmp` smoke logs committed;
- HTTP 80 remains diagnostic/dev-only.

### Asgard

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 -BaseUrl "http://178.105.220.117" -ProfileId "asgard-convergence-3d-8x8x8-v0.1" -TimeoutSeconds 180 -NoSecretLog
```

Result:

- PASS.
- Match: `match-16-asgard`, table `table-16`.
- Action source: `server-preview`.
- Accepted notation: `#1 S1 MOVE R (2,2,0)->(1,2,0)`.
- Final hash: `e8443902f01a9450`.

### Classic

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 -BaseUrl "http://178.105.220.117" -ProfileId "classic-six-side-3d-8x8x8-v0.1" -TimeoutSeconds 180 -NoSecretLog
```

Result:

- PASS.
- Match: `match-17-classic`, table `table-17`.
- Action source: `server-preview`.
- Accepted notation: `#1 S1 MOVE K (4,4,0)->(3,5,1)`.
- Final hash: `679085fef5801b2a`.

### Single-Side Training

Command used `-SkipActionSubmit`.

Result:

- PASS.
- Match: `match-18-single`, table `table-18`.
- Snapshot/action-log PASS.
- Final hash: `e3a0ccbe33ae47df`.

### Rubik Convergence

Command used `-SkipActionSubmit`.

Result:

- PASS.
- Match: `match-19-rubik`, table `table-19`.
- Snapshot/action-log PASS.
- Final hash: `df5cecc8f5e0c331`.
- Rubik layer-turn online submit remains behind the dedicated special-action boundary.

### Hodge Projection Duel

Command used `-SkipActionSubmit`.

Result:

- PASS.
- Match: `match-20-hodge`, table `table-20`.
- Snapshot/action-log PASS.
- Final hash: `668481062bf778bd`.
- Hodge projection online submit remains behind the dedicated special-action boundary.

## UI Smoke Status

The WPF client was build-verified by both local app build and full `verify.ps1`.

The actual click path remains the documented operator path:

1. Launch `src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe`.
2. Click `Use Hetzner HTTP`.
3. Click `Check Health`.
4. Click `Check Diagnostics`.
5. Click `Create Two Test Players`.
6. Select `Asgard` or `Classic`.
7. Click `Create Test Match With Two Local Clients`.
8. Click `Ready Both`.
9. Click `Start Game`.
10. Click `Request Snapshot`.
11. Click an occupied source cell.
12. Click a highlighted legal target.
13. Confirm accepted action, board refresh, action log, compact status, and session report.

Phase 30 did not commit raw manual `.tmp` session reports. Earlier P4G2 one-app and two-window UI result docs remain the sanitized operator references for the hands-on UI path.

## Boundaries Not Touched

This phase did not modify:

- 443/TLS/domain;
- x-ui/Xray;
- Outline;
- Albatronix Docker;
- Unreal SYServer;
- Nginx;
- UFW/firewall;
- systemd service configuration;
- Chess3D rules or native ABI.

## Limitations

- Rubik layer-turn online UI dispatch is intentionally not sent as `NormalMove`.
- Hodge projected-move online UI dispatch needs a dedicated projection submit flow.
- Single-side smoke is startup/snapshot only in this operator pass.
- HTTP 80 remains diagnostic/dev-only.
