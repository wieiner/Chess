# P4D1 SignalR Test Timeout Audit

SignalR was not initially safe to use as the proof target because the runner timeout was broken. After switching to the C# watchdog, direct execution became bounded and exposed real test lifecycle issues instead of hanging indefinitely.

Findings:

1. The test wrote persistence/data-protection files under the system temp directory. In the Codex sandbox this caused access-denied behavior during JSON store replacement.
2. ASP.NET logging attempted to write to Windows EventLog in this environment, which can fail without elevated permissions.
3. `JsonOnlineStore.Save()` used `File.Replace`, which can fail on Windows/sandboxed filesystems with `IOException` even when the test owns the file.

Fixes:

- SignalR contract tests now place their store/keyring under repository `.tmp`.
- `ChessOnlineServerHost` explicitly uses console/debug logging providers instead of relying on EventLog-capable defaults.
- `JsonOnlineStore.Save()` keeps `File.Replace` as the preferred path and falls back to overwrite-copy/delete on `IOException` or `UnauthorizedAccessException`.
- Top-level SignalR test app shutdown now runs through `finally`.

Bounded results:

- direct SignalR through C# watchdog, timeout 30s before lifecycle fix -> `124` timeout, proving watchdog control;
- direct SignalR through C# watchdog after fixes, timeout 60s -> exit `0`;
- runner SignalR-only after C# watchdog integration -> exit `0`.
