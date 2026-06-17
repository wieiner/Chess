# Deployment Target Decision

Linux VPS remains the preferred production target for ChessOnlineServer operations, but P4B treats it as a packaging/runbook target rather than a confirmed runtime target.

Reason:
- The server project currently targets `net8.0-windows`.
- The server copies and uses the Windows native `Chess3DEngine.dll`.
- Running this package on Linux would require a portable native engine boundary or a managed/server-safe rules runtime.

P4B therefore ships:
- Linux systemd and nginx templates.
- Linux package helper scripts with an explicit portability guard.
- Windows deployment notes as the currently executable deployment path.

P4C/P4D should remove the Windows-native dependency before claiming Linux runtime support.
