# Chess3D P4C Linux Portability Decision

Status: P4C Phase 02 decision record.

## Decision

ChessOnlineServer remains a working Windows server today. Linux / Hetzner remains the preferred deployment target, but the current build must not be described as Linux-runtime-ready.

The practical P4C target is:

- keep the Windows-native server path green and packageable;
- make the server architecture portable-ready by extracting the Chess3D rules authority behind an adapter boundary in Phase 03;
- document Linux as blocked until a Linux-compatible rules authority exists;
- keep Hetzner runbooks as operator planning material, not proof of runtime support.

## Current Dependency Chain

`src/ChessOnlineServer/ChessOnlineServer.csproj` targets `net8.0-windows` and copies `Chess3DEngine.dll` from `bin\x64\Release`.

`src/ChessOnlineProtocol/ChessOnlineProtocol.csproj` targets `net8.0-windows` and links `src\ChessApp\NativeChess3DEngine.cs`.

`OnlineGameSession` directly constructs `NativeChess3DEngine`. `OnlineRoomRegistry` constructs `OnlineGameSession` for table creation, game start, hash validation, snapshot creation, action application, replay, and helper command generation.

That means the hosted server transport, protocol/session authority, and Windows native Chess3D engine are currently coupled.

## Deployment Options

### Option A: Keep Windows Server Only

Works today. It preserves the current green CI path and the authoritative engine behavior.

Cost: Hetzner Linux remains planning-only.

### Option B: Portable Server Shell + Windows Authority Adapter

Recommended P4C direction. The server transport/auth/persistence layer can move toward platform-neutral contracts while the current authority implementation remains Windows-native.

Cost: requires Phase 03 adapter boundary, diagnostics, and tests, but avoids rewriting gameplay rules.

### Option C: Compile Native Chess3DEngine for Linux

Correct long-term target for a true Linux server. The Linux build would need a `.so`, platform-specific native loading, and state-hash parity tests against Windows.

Cost: native C++ toolchain and CI work. This is P4D-sized, not Phase 02.

### Option D: Managed Rules Authority Subset

Possible for server-only Classic or reduced profiles, but risky because it can diverge from the native engine and from Asgard/Rubik/Hodge action semantics.

Cost: duplicate rule implementation and parity burden. Not recommended for P4C.

### Option E: Linux Gateway + Remote Windows Authority

Possible future fallback: Linux handles public ingress while a Windows process owns the authoritative engine.

Cost: distributed authority protocol, latency, state recovery, and ops complexity. Not P4C.

## Selected P4C Path

Use Option B.

Phase 03 should introduce an authority adapter boundary while preserving behavior:

- `IChessOnlineRulesAuthority`;
- `IChessOnlineGameSessionFactory`;
- possibly `IChessOnlineRulesProfileProvider`;
- Windows-native implementation backed by the current `NativeChess3DEngine`;
- diagnostics exposing authority runtime kind, OS, architecture, and native dependency status.

The project should continue to say:

- Windows server package: executable today.
- Linux Hetzner package: deployment plan / scaffold today.
- Linux authoritative runtime: blocked until P4D provides a Linux rules authority.

## Exact Linux Blockers

- `ChessOnlineServer` target framework is `net8.0-windows`.
- `ChessOnlineProtocol` target framework is `net8.0-windows`.
- `ChessOnlineProtocol` links `NativeChess3DEngine.cs` from the WPF/Windows desktop app project.
- `NativeChess3DEngine` uses Windows DLL imports.
- `ChessOnlineServer` copies `Chess3DEngine.dll`, not a Linux `.so`.
- No Linux CI validates server build, native loading, or state-hash parity.

## P4D Backlog

- Build `Chess3DEngine` as a Linux shared library.
- Add platform-specific native loader / `DllImportResolver` or equivalent wrapper.
- Split server/protocol projects to a base TFM where possible.
- Add Linux CI for `ChessOnlineServer`.
- Add Windows/Linux state-hash parity tests for all five RuleProfiles.
- Add publish tests for `linux-x64`.
- Validate SignalR + Kestrel + nginx/systemd on an actual Linux host or controlled VM.

## Current Operator Guidance

For a real game session today, use the Windows server package.

For Hetzner planning, use the Linux runbooks and templates only as scaffolding. Do not deploy secrets, keyrings, stores, or production credentials into the repository.

## Sources

- Microsoft Learn, Target frameworks in SDK-style projects: platform-specific libraries should target platform-specific TFMs such as `net*-windows`, while portable apps/libraries should target a base TFM.
- Microsoft Learn, .NET RID catalog: RIDs identify target platforms and platform-specific assets such as `win-x64` and `linux-x64`.
- Microsoft Learn, Native library loading: .NET can resolve native libraries with P/Invoke and resolver hooks, but the native library must exist for the target platform.
- Microsoft Learn, Host ASP.NET Core on Linux with Nginx: ASP.NET Core can run on Kestrel behind Nginx on Linux, which is useful after the authority blocker is removed.
