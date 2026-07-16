# P4L Rubik Repository Regression

Date: 2026-07-16

## Result

The full local repository gate passed after the Rubik physical-state and solver workflow work.

| Gate | Result | Duration / scope |
| --- | --- | --- |
| `tests/run-tests.ps1 -SkipBenchmark -MSBuildMaxCpuCount 1` | PASS | 6m51s; 8 selected and executed contract tests |
| `scripts/verify.ps1` | PASS | 22m09s; full build, assets/package checks, 9 selected and executed tests including quick benchmark |

No test executable timed out. The full gate covered Chess2D, Chess3D, Rubik, GPU ABI, Online contracts, and SignalR contracts.

## Isolation checks

- Chess3D still has exactly five real rule profile documents: Classic, Single-Side, Asgard, Rubik, and Hodge. The sixth JSON in the profile directory is the schema, not a rule profile.
- CUDA remained optional. The benchmark reported the CUDA DLL unavailable and continued successfully with CPU/Direct3D paths.
- Development and production packaging checks passed, including application executables and rule/scenario assets.
- `git ls-files .tmp` returned no tracked files.
- The phase diff contains no `src/ChessOnlineServer`, `scripts/deploy`, or `deploy` path.
- No Hetzner, service, nginx, firewall, runtime store, keyring, or certificate operation was performed.

## Commands

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark -MSBuildMaxCpuCount 1
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

The verify run used controlled MSBuild parallelism reported by the script and the decomposed test runner's per-executable watchdogs.
