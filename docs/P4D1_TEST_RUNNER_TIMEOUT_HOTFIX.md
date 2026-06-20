# P4D1 Test Runner Timeout Hotfix

The previous timeout implementation was unsafe. A SignalR-only command with `OnlineTestTimeoutSeconds=30` and `GlobalTimeoutSeconds=90` was observed hanging for much longer than the configured timeout. That means the runner-level timeout was broken.

Root cause:

- process timeout logic lived inside PowerShell;
- stdout/stderr handling and process lifetime were coupled to the runner;
- timeout was not independently proven before running SignalR;
- PowerShell wrapper attempts were abandoned after direct SignalR still hung.

Fix:

- added `tools/TestProcessWatchdog`, a dedicated C#/.NET watchdog executable;
- `tests/run-tests.ps1` now invokes that executable for every test process;
- `run-tests.ps1` no longer uses `Start-Process`, `ReadToEnd`, or the PowerShell watchdog for test execution;
- logs are written to `.tmp/test-logs`.

Proof commands:

```powershell
.\tools\TestProcessWatchdog\bin\Release\net8.0\TestProcessWatchdog.exe --file powershell --args '-NoProfile -Command "Start-Sleep -Seconds 999"' --workdir . --timeout 5 --stdout .tmp\test-logs\cs-watchdog-hang.stdout.log --stderr .tmp\test-logs\cs-watchdog-hang.stderr.log
.\tools\TestProcessWatchdog\bin\Release\net8.0\TestProcessWatchdog.exe --file powershell --args '-NoProfile -Command "Write-Output OK; exit 0"' --workdir . --timeout 10 --stdout .tmp\test-logs\cs-watchdog-ok.stdout.log --stderr .tmp\test-logs\cs-watchdog-ok.stderr.log
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only SignalR -SkipSolutionBuild -SkipTestBuild -SkipBenchmark -OnlineTestTimeoutSeconds 60 -GlobalTimeoutSeconds 120
```
