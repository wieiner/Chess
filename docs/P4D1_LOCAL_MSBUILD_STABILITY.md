# P4D1 Local MSBuild Stability

Status: P4D1.4 operational note.

## Problem

This workstation showed intermittent MSBuild failures where `dotnet build` or test-project builds returned exit code 1 without useful errors. Re-running the same project directly or with diagnostic verbosity showed clean builds. The failures correlated with uncontrolled `/m` parallel builds and stale build-server style processes.

## Policy

Project scripts should avoid bare `/m`. Use explicit `/m:N` and `/nr:false` so local and CI behavior is repeatable.

## Useful knobs

- `CHESS_TEST_MSBUILD_MAX_CPU_COUNT`: default parallelism for `tests/run-tests.ps1`.
- `CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT`: default parallelism for `scripts/verify.ps1` and package build scripts.
- `CHESS_RELEASE_MSBUILD_MAX_CPU_COUNT`: default parallelism for `tools/release/Build-Production.ps1`.

## Stale process cleanup

`tests/run-tests.ps1` has `-CleanStaleBuildProcesses`, but it is intentionally not default. When supplied, it prints matching `MSBuild`, `VBCSCompiler`, and `dotnet` processes before stopping them. Use it only when the local machine is clearly wedged by stale build processes.

## Recommended local triage

1. `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List`
2. `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Online -BuildOnly -MSBuildMaxCpuCount 4`
3. `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only SignalR -SkipSolutionBuild -SkipTestBuild -SkipBenchmark -OnlineTestTimeoutSeconds 60`
4. `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1 -SkipBenchmark`

## Online Timeout Hotfix

A one-hour `OnlineTestTimeoutSeconds=180` run indicates a broken runner timeout, not a slow suite. The hotfixed runner writes stdout/stderr directly to `.tmp/test-logs`, kills timed-out process trees, and reports `TIMEOUT` in the summary. Start with `-Only SignalR -OnlineTestTimeoutSeconds 30 -GlobalTimeoutSeconds 90` when investigating SignalR hangs.
