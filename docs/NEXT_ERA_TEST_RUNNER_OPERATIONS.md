# Next Era Test Runner Operations

Status: Phase 01 operations guide.

## Shell Choice

Prefer PowerShell 7 (`pwsh`) for local Next Era gates:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
```

Windows PowerShell 5.1 remains supported for compatibility with older local scripts and CI entry points:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
```

Use `-NoProfile` to avoid local profile side effects, and keep `-ExecutionPolicy Bypass` scoped to the single repo command.

## Fast Commands

List tests without building or running:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
```

Run native tests only:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Native -SkipBenchmark -MSBuildMaxCpuCount 1
```

Run online tests only:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Online -SkipBenchmark -OnlineTestTimeoutSeconds 180 -MSBuildMaxCpuCount 1
```

Run only SignalR:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only SignalR -SkipBenchmark -OnlineTestTimeoutSeconds 120 -MSBuildMaxCpuCount 1
```

Run the full contract layer without the quick benchmark:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark -GlobalTimeoutSeconds 900 -MSBuildMaxCpuCount 1
```

Run the full verification gate:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

## Timeout Model

`tests/run-tests.ps1` does not own process timeout logic directly. It calls:

```text
tools\TestProcessWatchdog\bin\Release\net8.0\TestProcessWatchdog.exe
```

The watchdog writes stdout and stderr to files under:

```text
.tmp\test-logs
```

Exit codes:

- `0`: child test passed;
- `124`: timeout;
- any other child exit code: failed test;
- `125`: watchdog argument/internal failure.

If a test reports `TIMEOUT`, inspect:

```powershell
Get-Content .\.tmp\test-logs\<TestName>.stdout.log -Tail 120
Get-Content .\.tmp\test-logs\<TestName>.stderr.log -Tail 120
```

Do not reintroduce a PowerShell pipeline-based watchdog as the timeout authority.

## MSBuild Parallelism

Bare `/m` is not allowed in project scripts. The runner resolves MSBuild node count as:

1. `-NoParallelBuild` -> `/m:1`;
2. `-MSBuildMaxCpuCount N` -> `/m:N`;
3. `CHESS_TEST_MSBUILD_MAX_CPU_COUNT`;
4. `CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT`;
5. default `/m:4`.

Every runner/verify build uses `/nr:false`.

For unstable local machines:

```powershell
$env:CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT = "1"
$env:CHESS_TEST_MSBUILD_MAX_CPU_COUNT = "1"
```

## Stale Build Processes

First inspect candidates:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\diagnostics\Find-StaleBuildProcesses.ps1
```

The stop helper is intentionally a template and dry-runs by default:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\diagnostics\Stop-StaleBuildProcesses.ps1.template
```

Only after inspecting the list:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\diagnostics\Stop-StaleBuildProcesses.ps1.template -ConfirmStop
```

Use `-IncludeAllDotnet` only when the printed command lines are known to belong to this repo. The default behavior does not stop unrelated `dotnet.exe` processes.

## Hang Diagnosis Checklist

1. Run `-List` to confirm the runner can parse arguments.
2. Run the smallest matching suite, for example `-Only SignalR`.
3. Check `.tmp\test-logs`.
4. Run `Find-StaleBuildProcesses.ps1`.
5. Re-run with `-MSBuildMaxCpuCount 1`.
6. If the watchdog returns `124`, fix the test lifecycle; do not increase timeout blindly.
