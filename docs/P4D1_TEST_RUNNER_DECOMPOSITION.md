# P4D1 Test Runner Decomposition

Status: implemented in P4D1.4.

## Main commands

List available tests without building or running:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
```

Run native tests only:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Native -SkipBenchmark
```

Run online tests only:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Online -SkipBenchmark -OnlineTestTimeoutSeconds 180
```

Run only SignalR:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only SignalR -SkipBenchmark -OnlineTestTimeoutSeconds 60
```

Run the old full contract-test shape without benchmark:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark
```

## Suites

- `All`: all contract tests, plus benchmark unless `-SkipBenchmark` is supplied.
- `Native`: C++ native contract tests.
- `Managed`: managed contract tests.
- `Online`: online protocol and SignalR contract tests.
- `Chess3D`: Chess3D native contract tests.
- `Gpu`: GPU backend contract tests.
- `Rubik`: Rubik native contract tests.
- `Chess2D`: Chess2D native contract tests, plus benchmark unless skipped.

## Build controls

The runner never uses bare `/m` anymore. It resolves MSBuild parallelism in this order:

1. `-NoParallelBuild` means `/m:1`.
2. `-MSBuildMaxCpuCount N` means `/m:N`.
3. `CHESS_TEST_MSBUILD_MAX_CPU_COUNT`.
4. `CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT`.
5. default `/m:4`.

`-SkipSolutionBuild` keeps the existing behavior of skipping the full solution build. `-SkipTestBuild` runs existing test executables without rebuilding test projects. `-BuildOnly` builds selected tests and does not execute them.

## Runtime controls

Each executable is launched as a child process with redirected stdout/stderr and a timeout. Default timeout is 120 seconds, while online tests default to 180 seconds. On timeout the runner kills the child process tree, records `TIMEOUT`, prints the log path, and exits non-zero.

Logs are written to `.tmp/test-logs`, which is ignored by Git.

## Timeout Hotfix

P4D1.4 hotfix changes executable launch to redirect stdout/stderr directly to files. The runner no longer waits on `ReadToEndAsync()` after killing a timed-out process, so online tests must now end as PASS, FAIL, or TIMEOUT instead of hanging indefinitely.

Use `-GlobalTimeoutSeconds` as a whole-run guard, especially for online suites.
