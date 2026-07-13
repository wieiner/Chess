# P4K Full Local Verification Result

Date: 2026-07-13

## Result

Status: **PASS**

The Phase 42 gate ran sequentially after the deployed Phase 41 commit and its
GitHub Actions success. No build commands overlapped.

## Test Registry

`tests/run-tests.ps1 -List` completed successfully and reported:

- `ChessEngineContractTests`;
- `Chess3DEngineContractTests`;
- `RubikEngineContractTests`;
- `GpuBackendContractTests`;
- `ChessOnlineContractTests`;
- `ChessOnlineSignalRContractTests`;
- optional `Chess2DBenchmarkQuick`.

## Contract Gate

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\tests\run-tests.ps1 `
  -SkipBenchmark -MSBuildMaxCpuCount 1
```

Result:

- duration: 171.7 seconds;
- selected/executed: 6/6;
- native Chess2D: PASS;
- native Chess3D and exactly five profiles: PASS;
- Rubik: PASS;
- GPU backend: PASS;
- managed Online: PASS;
- hosted SignalR/auth/persistence/readiness/rate limits: PASS;
- watchdog timeout: 0.

## Full Verify

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

Result:

- duration: 495.7 seconds;
- Release x64 solution build: PASS;
- all 6 contract executables: PASS;
- production and Linux package assertions: PASS;
- models, profiles, scenarios, ABI and deployment artifacts: PASS;
- quick Chess2D benchmark: PASS;
- final marker: `Verify complete`.

The quick benchmark selected the Direct3D 11 compute backend. The optional CUDA
DLL was unavailable (`Win32 error 126`) and CUDA rows were reported as
`unavailable`; this is the expected optional behavior and did not weaken or
fail the gate.

## Working Tree

After both commands, the only tracked modification was the Phase 42 research
log awaiting this documentation commit. Build products, production packages,
watchdog logs, and remote smoke logs remained ignored. No generated binary or
runtime store became tracked.
