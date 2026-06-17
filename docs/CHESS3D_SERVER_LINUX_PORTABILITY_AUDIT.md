# ChessOnlineServer Linux Portability Audit

Status in P4B: blocked for real Linux runtime execution.

Blockers:
- `src/ChessOnlineServer/ChessOnlineServer.csproj` targets `net8.0-windows`.
- The server copies `Chess3DEngine.dll`, currently a Windows native DLL.
- Contract tests and verify run on the Windows build.

Safe today:
- Build and run on Windows.
- Package templates for future Linux deployment.
- Use nginx/systemd docs as operator scaffolding.

Required future work:
- Move server authority to a portable native library or managed rules core.
- Add Linux CI for the server.
- Verify native dependency loading and state-hash parity on Linux.
