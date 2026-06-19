# P4D Linux Server Publish Result

P4D phase 09 evaluated the linux-x64 `ChessOnlineServer` publish path.

## Result

No linux-x64 server package was produced in this phase.

This is an intentional blocked result. Publishing a Linux package would be misleading until the native authority and target framework blockers are resolved.

## Target framework audit

Current server-side projects still target `net8.0-windows`:

- `src/ChessOnlineServer/ChessOnlineServer.csproj`;
- `src/ChessOnlineProtocol/ChessOnlineProtocol.csproj`;
- related Windows build/test projects remain tied to the current Windows CI path.

The server also still copies the Windows native artifact:

- `Chess3DEngine.dll`.

P4D phase 04 introduced a platform-neutral resolver boundary, but that does not by itself make the server publishable for Linux.

## Guarded publish check

The existing script was run without `-Force`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Publish-ChessOnlineServer-Linux.ps1
```

It correctly stopped with the documented guard:

```text
ChessOnlineServer currently targets net8.0-windows and uses the Windows native Chess3DEngine DLL. Linux runtime publish is deferred; rerun with -Force only for portability experiments.
```

`-Force` was not used because this phase should not create a fake or partially invalid Linux package.

## Required before real publish

A real linux-x64 package requires:

1. A successful Linux native build of `libChess3DEngine.so`.
2. Server-side target framework cleanup away from `net8.0-windows` where safe.
3. Copy/publish logic that includes `libChess3DEngine.so` and excludes `Chess3DEngine.dll` for Linux runtime packages.
4. No secrets, store JSON, keyring, certificates, or runtime database in the package.
5. A follow-up Kestrel-only smoke on the target Linux host.

## Current status

| Area | Status |
| --- | --- |
| Windows build/CI | green through phase 08 |
| Native resolver boundary | present |
| Linux native `.so` | not built yet |
| linux-x64 publish | blocked |
| Hetzner Kestrel smoke | not eligible yet |
