# Next Era Baseline Status

Date: 2026-06-21

## Git State

- Branch: `main`
- HEAD: `77d1401e8b9944035c0a00894792f307fd32101d`
- `origin/main`: `77d1401e8b9944035c0a00894792f307fd32101d`
- Local commits ahead of `origin/main`: none
- Remote commits ahead of local HEAD: none
- Working tree before documentation changes: clean

## Last Known CI

- Workflow: `Windows Build`
- Run id: `27878825241`
- Commit: `77d1401 P4D1 hotfix: resolve CI MSBuild path`
- Result: success
- Duration: 3m54s
- Timestamp: 2026-06-20T17:38:45Z

## Local Baseline Gates

All commands were run with `pwsh` 7.6.3.

| Command | Result | Notes |
| --- | --- | --- |
| `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List` | PASS | Test registry lists Native, Managed, Online, Chess3D, Gpu, Rubik, Chess2D suites with explicit timeouts. |
| `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Native -SkipBenchmark -MSBuildMaxCpuCount 1` | PASS | Native contract tests completed through the C# watchdog. |
| `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Online -SkipBenchmark -OnlineTestTimeoutSeconds 180 -MSBuildMaxCpuCount 1` | PASS | Online and SignalR contract tests completed through the C# watchdog. SignalR completed in about 26 seconds. |
| `pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` | PASS | Full local verify completed successfully. |

Observed but non-blocking: during `verify.ps1`, MSBuild emitted transient copy retry warnings for `Chess3DEngine.dll` while the build was still active. The build recovered and the full verify completed green. This remains a useful contention symptom to watch in Next Era runner/build hardening.

## Runner Status

- `tests/run-tests.ps1` is decomposed into suites.
- Test executable execution is bounded by `tools\TestProcessWatchdog`.
- `.tmp\test-logs` is ignored and receives per-test stdout/stderr logs.
- `MSBuildMaxCpuCount` can be controlled from the command line and environment.
- The old PowerShell watchdog is not the timeout authority.

## Linux Server Status

Known from the current historical baseline:

- P4D1 built `libChess3DEngine.so` on Hetzner through native clang/CMake/Ninja.
- ABI parity between the Windows DLL and Linux SO was previously confirmed.
- Server-side projects are on portable `net8.0`; Windows desktop apps remain `net8.0-windows`.
- The current baseline does not yet prove a persistent Hetzner production-like service, systemd service, Nginx reverse proxy, external HTTP health, or remote SignalR/Asgard smoke. Those are the next Next Era deployment phases.

## Open Blockers

- Need a current Hetzner reality check before touching deployment paths.
- Need Linux package completion with an externally supplied tested `libChess3DEngine.so`.
- Need Kestrel smoke, systemd service, Nginx HTTP exposure, and remote SignalR/Asgard smoke before claiming the Linux server is operational.
- TLS/domain status is unknown and must be documented before any serious public-auth use.
- Historical roadmap/status docs still need a contradiction cleanup pass after the deployment reality is known.

## Exact Next Phase

Next phase: `PHASE 01 - TEST RUNNER / VERIFY HARDENING FINALIZATION`.

Primary goals:

- Audit runner/verify operations after the C# watchdog hotfix.
- Add operator diagnostics for stale build processes.
- Document suite usage, timeout logs, and controlled MSBuild parallelism.
- Keep the full gate bounded and understandable.
