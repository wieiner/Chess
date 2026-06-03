# Chess3D SignalR Logging

P3F logging is intended for local development and CI diagnostics.

## Logged

- server startup/shutdown;
- health requests;
- SignalR connection lifecycle from ASP.NET Core logs;
- protocol accept/reject summaries through UI/test status;
- diagnostics counters.

## Not Logged

- session tokens;
- credentials;
- full private savegame contents by default;
- user secrets.

## Notes

The SignalR contract tests can emit verbose ASP.NET Core request logs. That output is acceptable for local/CI debugging and should not be treated as production logging guidance.

