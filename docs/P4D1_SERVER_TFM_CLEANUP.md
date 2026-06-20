# P4D1 Phase 04 - Server TFM Cleanup

Status: implemented in P4D1 Phase 04.

## Goal

Make the managed ChessOnlineServer authority path publishable for Linux without changing gameplay rules, native ABI layout, WPF clients, save/replay formats, or the five existing Chess3D RuleProfiles.

## Retargeted projects

The following server-side projects now target plain `net8.0`:

| Project | Previous TFM | New TFM | Rationale |
| --- | --- | --- | --- |
| `src/ChessOnlineProtocol/ChessOnlineProtocol.csproj` | `net8.0-windows` | `net8.0` | The linked native interop/resolver code is managed and does not use WPF APIs. |
| `src/ChessOnlinePersistence/ChessOnlinePersistence.csproj` | `net8.0-windows` | `net8.0` | Persistence code is portable-shaped and only needs ASP.NET Core framework references. |
| `src/ChessOnlineServer/ChessOnlineServer.csproj` | `net8.0-windows` | `net8.0` | ASP.NET Core Kestrel server can now be published for `linux-x64`. |

The WPF projects remain `net8.0-windows`:

- `src/ChessApp`
- `src/Chess3DApp`
- `src/ChessOnlineApp`
- `src/RubikApp`

The Windows-only contract test projects also remain `net8.0-windows` for the current CI lane.

## Native library copy behavior

`ChessOnlineServer.csproj` now keeps RID-specific native copy behavior:

- ordinary Windows build: copies `bin/x64/Release/Chess3DEngine.dll` into the server output;
- `linux-x64` publish: copies `libChess3DEngine.so` only when a tested path is provided through `Chess3DEngineLinuxPath`/the deploy script `-NativeLibraryPath` parameter;
- the Linux package path does not intentionally include the Windows DLL.

The Linux native artifact remains a build output, not tracked repository content.

## Script changes

- `scripts/deploy/Publish-ChessOnlineServer-Linux.ps1` no longer blocks on `net8.0-windows` and publishes to `DeploymentOutput/linux-x64/ChessOnlineServer` by default.
- The script accepts `-NativeLibraryPath` to include a tested `libChess3DEngine.so` artifact.
- `tools/release/Build-Production.ps1` and `scripts/verify.ps1` now use the server output folder `src/ChessOnlineServer/bin/x64/Release/net8.0`.

## Verification expected for this phase

Run from repository root:

```powershell
dotnet build src\ChessOnlineProtocol\ChessOnlineProtocol.csproj -c Release
dotnet build src\ChessOnlineServer\ChessOnlineServer.csproj -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

If MSBuild node reuse contention appears locally, clear stale MSBuild/VBCSCompiler processes and rerun with `CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT=1`.

## Still deferred

- Linux package execution smoke is Phase 05/06, not claimed here.
- No systemd/Nginx/TLS deployment is part of this phase.
- No secrets, runtime databases, keyrings, certificates, or private keys are part of this phase.
