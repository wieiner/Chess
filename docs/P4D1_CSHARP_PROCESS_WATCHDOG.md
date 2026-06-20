# P4D1 C# Process Watchdog

P4D1.4 replaced the failed PowerShell timeout wrapper with a small .NET watchdog executable:

- Project: `tools/TestProcessWatchdog/TestProcessWatchdog.csproj`
- Binary after build: `tools/TestProcessWatchdog/bin/Release/net8.0/TestProcessWatchdog.exe`
- Framework: `net8.0`

The watchdog starts one child process with `System.Diagnostics.Process`, redirects stdout/stderr to files, waits with a cancellation timeout, and calls `process.Kill(entireProcessTree: true)` on timeout. It returns:

- child exit code when the child exits normally;
- `124` on timeout;
- `125` for watchdog argument/internal errors.

Required proof recorded on 2026-06-20:

- artificial hang: `powershell -NoProfile -Command "Start-Sleep -Seconds 999"`, timeout 5s -> `TimedOut=True`, exit `124` in about 6s;
- normal child: `powershell -NoProfile -Command "Write-Output OK; exit 0"`, timeout 10s -> exit `0`, stdout contains `OK`;
- direct `ChessOnlineSignalRContractTests.exe`, timeout 60s -> exit `0` in about 25s;
- `tests/run-tests.ps1 -Only SignalR ...` -> exit `0` in about 27s.

The older PowerShell watchdog is not trusted and is not used by the runner.
