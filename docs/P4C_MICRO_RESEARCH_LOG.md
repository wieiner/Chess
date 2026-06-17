# P4C Micro Research Log

## Phase 00 - Baseline / Safety

- topic: repository and CI baseline before P4C
- internet/source researched: local git state, local `scripts/verify.ps1`, GitHub CLI run list; internet research not required for this phase because it records the current repository baseline only
- key finding: `main` is clean at `7e5ce76 Add Chess3D deployment and matchmaking MVP`; previous GitHub Actions run `27678188526` succeeded; local full verify passed
- decision for this repo: start P4C from the green P4B baseline and do not begin portability refactors until the baseline report is committed and CI is green
- concrete files affected: `docs/P4C_BASELINE_REPORT.md`, `docs/P4C_MICRO_RESEARCH_LOG.md`
- risk: documentation-only phase can still fail if verify/package unexpectedly regresses after the previous commit
- test/verify plan: run `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1`, then commit/push and wait for GitHub Actions

## Phase 01 - Portability + Product Surface Audit

- topic: ASP.NET Core Linux hosting and reverse proxy baseline
- internet/source researched: Microsoft Learn, "Host ASP.NET Core on Linux with Nginx", https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx
- key finding: ASP.NET Core apps commonly run on Kestrel behind Nginx on Linux, with systemd managing the app process
- decision for this repo: keep the P4B Linux nginx/systemd templates as the intended deployment shape, but do not claim runtime support until the server/native engine boundary is portable
- concrete files affected: `docs/CHESS3D_P4C_PORTABILITY_PRODUCT_AUDIT.md`, `docs/CHESS3D_PROJECT_PLATFORM_MATRIX.md`
- risk: deployment docs could imply Linux support too strongly
- test/verify plan: docs-only phase, run `git diff --check`

- topic: .NET publish/RID portability
- internet/source researched: Microsoft Learn, ".NET RID Catalog", https://learn.microsoft.com/en-us/dotnet/core/rid-catalog; Microsoft Learn, ".NET application publishing overview", https://learn.microsoft.com/en-us/dotnet/core/deploying/
- key finding: RIDs such as `win-x64` and `linux-x64` identify runtime-specific assets; native dependencies must exist for the target runtime
- decision for this repo: P4C should treat Linux publishing as blocked until a Linux-compatible `Chess3DEngine` native artifact or adapter exists
- concrete files affected: `docs/CHESS3D_PROJECT_PLATFORM_MATRIX.md`
- risk: a `dotnet publish -r linux-x64` command alone would not make a Windows native DLL usable on Linux
- test/verify plan: no publish change in Phase 01; documentation and matrix only

- topic: SignalR behind reverse proxy and scale limits
- internet/source researched: Microsoft Learn, "ASP.NET Core SignalR hosting and scaling", https://learn.microsoft.com/en-us/aspnet/core/signalr/scale
- key finding: single-server SignalR is distinct from scaled-out SignalR; Azure SignalR/backplanes are scale features and remain out of scope
- decision for this repo: keep P4B matchmaking as a single-server MVP and document Redis/Azure SignalR as future, not P4C work
- concrete files affected: `docs/CHESS3D_PRODUCT_SURFACE_MAP.md`
- risk: product docs might overstate public matchmaking readiness
- test/verify plan: docs-only phase

- topic: WPF platform boundary
- internet/source researched: Microsoft Learn, "What is Windows Presentation Foundation", https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/
- key finding: WPF is a Windows-only UI framework
- decision for this repo: ChessApp, Chess3DApp, RubikApp, and ChessOnlineApp remain Windows desktop products; Linux portability work must target server/protocol/persistence boundaries, not WPF apps
- concrete files affected: `docs/CHESS3D_PROJECT_PLATFORM_MATRIX.md`, `docs/CHESS3D_PRODUCT_SURFACE_MAP.md`
- risk: trying to make WPF apps Linux-portable would be a large unrelated rewrite
- test/verify plan: docs-only phase

- topic: native library loading across Windows/Linux
- internet/source researched: Microsoft Learn, "Native library loading", https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading
- key finding: .NET can search platform-specific native library name variations and can use `SetDllImportResolver`, but the target native library must still exist for that platform
- decision for this repo: later phases should introduce an authority adapter boundary before attempting native Linux `.so` loading
- concrete files affected: `docs/CHESS3D_P4C_PORTABILITY_PRODUCT_AUDIT.md`
- risk: current C# P/Invoke wrappers name Windows DLLs directly
- test/verify plan: no code changes in Phase 01

- topic: Phase 01 CI failure triage after docs-only audit push
- internet/source researched: GitHub Actions failed log for run `27694551492`; local SignalR contract test reproduction
- key finding: `ChessOnlineSignalRContractTests` failed only at `SignalR Hello emits ReceiveWelcome`; the hub returned the `Welcome` message, but the test checked the async client event immediately after `InvokeAsync`, creating a scheduler-sensitive CI race
- decision for this repo: keep the SignalR behavior contract, but make the test wait briefly for required async hub events with an atomic counter instead of a fixed immediate `List.Count` check
- concrete files affected: `tests/ChessOnlineSignalRContractTests/Program.cs`
- risk: SignalR broadcast/event assertions can be CI-sensitive when they assume callback delivery is synchronous with method return
- test/verify plan: run targeted `dotnet run --project tests\ChessOnlineSignalRContractTests\ChessOnlineSignalRContractTests.csproj -c Release -p:Platform=x64`, then full `tests\run-tests.ps1 -SkipBenchmark` and `scripts\verify.ps1`

## Phase 02 - Server Linux Portability Decision

- topic: server target framework portability
- internet/source researched: Microsoft Learn, "Target frameworks in SDK-style projects", https://learn.microsoft.com/en-us/dotnet/standard/frameworks
- key finding: portable apps and libraries should target a base TFM, while platform-specific projects use platform-specific TFMs such as `net*-windows`
- decision for this repo: do not claim `ChessOnlineServer` is Linux-ready while `ChessOnlineServer` and `ChessOnlineProtocol` target `net8.0-windows`; use P4C to prepare an adapter boundary first
- concrete files affected: `docs/CHESS3D_P4C_LINUX_PORTABILITY_DECISION.md`
- risk: switching target frameworks before removing the native/WPF wrapper dependency would create noisy build failures instead of real portability
- test/verify plan: docs decision plus targeted `dotnet build src\ChessOnlineServer\ChessOnlineServer.csproj -c Release -p:Platform=x64`

- topic: native Chess3D engine blocker
- internet/source researched: Microsoft Learn, ".NET RID Catalog", https://learn.microsoft.com/en-us/dotnet/core/rid-catalog; Microsoft Learn, "Native library loading", https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading
- key finding: RID-specific native assets are platform-bound; native loading helpers do not remove the need for a Linux-compatible native library
- decision for this repo: keep the Windows `Chess3DEngine.dll` authority as the current implementation and define Linux `.so` plus state-hash parity as P4D backlog
- concrete files affected: `docs/CHESS3D_P4C_LINUX_PORTABILITY_DECISION.md`
- risk: publishing `linux-x64` without a Linux `Chess3DEngine` would produce a package that starts only until authority code touches the missing native dependency
- test/verify plan: no native publish change in Phase 02

- topic: Hetzner/Linux runtime shape
- internet/source researched: Microsoft Learn, "Host ASP.NET Core on Linux with Nginx", https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx
- key finding: Kestrel behind Nginx/systemd is a valid deployment shape for ASP.NET Core after application dependencies are portable
- decision for this repo: keep Hetzner/Linux docs as runbook scaffolding now; treat the authoritative runtime as blocked until the rules authority boundary and Linux native engine exist
- concrete files affected: `docs/CHESS3D_P4C_LINUX_PORTABILITY_DECISION.md`
- risk: deployment templates can be mistaken for a working Linux product unless the blocker is repeated in the decision package
- test/verify plan: build the current Windows server path and keep CI green

## Phase 03 - Authority Adapter Boundary

- topic: authority/session construction boundary
- internet/source researched: local source audit of `OnlineGameSession`, `OnlineRoomRegistry`, `ChessOnlineServerHost`; Microsoft Learn native library loading and target framework docs from Phase 02 remain the platform reference
- key finding: `OnlineRoomRegistry` directly constructed `OnlineGameSession`, and `OnlineGameSession` directly owned `NativeChess3DEngine`, so the hosted transport and native rules authority had no replacement seam
- decision for this repo: introduce `IChessOnlineRulesAuthority` and `IChessOnlineGameSessionFactory`, keep the current Windows-native authority as the default implementation, and leave gameplay behavior unchanged
- concrete files affected: `src/ChessOnlineProtocol/OnlineRulesAuthority.cs`, `src/ChessOnlineProtocol/OnlineGameSession.cs`, `src/ChessOnlineProtocol/OnlineRoomRegistry.cs`, `src/ChessOnlineServer/ChessOnlineServerHost.cs`
- risk: adapter work could accidentally change online action semantics if the registry/session contract shifts too much
- test/verify plan: build protocol/server, run online contract tests, run SignalR contract tests, then full verify before commit

- topic: operator diagnostics for portability
- internet/source researched: local diagnostics endpoint audit; Microsoft Learn RID/native loading docs from Phase 02
- key finding: the existing `/chess3d/diagnostics` endpoint did not say whether authority runtime is portable or Windows-native
- decision for this repo: expose authority runtime kind, platform, process architecture, native library name/path, and support/portability flags in diagnostics
- concrete files affected: `src/ChessOnlineServer/ChessOnlineServerHost.cs`, `docs/CHESS3D_ONLINE_AUTHORITY_ADAPTER.md`, `docs/ARCHITECTURE.md`
- risk: diagnostics must not expose secrets; native library path is an application binary path, not a token/key/store path
- test/verify plan: existing diagnostics no-secret tests plus targeted online/SignalR contract tests

## Phase 04 - Windows Server Package Hardening

- topic: Windows portable server run flow
- internet/source researched: local package scripts, `ChessOnlineServer.csproj`, `deploy\windows`, and existing `ProductionOutput\ChessOnlineServer` launcher
- key finding: the server package already copied Windows deploy templates, but it did not include first-class start/stop/smoke scripts in the packaged deploy folder
- decision for this repo: add Windows start, stop, and smoke scripts; copy them into `Deploy\windows` through the server project; keep runtime data under operator-owned `Data`
- concrete files affected: `scripts\deploy\Start-ChessOnlineServer-Windows.ps1`, `scripts\deploy\Stop-ChessOnlineServer-Windows.ps1`, `scripts\deploy\Test-ChessOnlineServer-Windows.ps1`, `src\ChessOnlineServer\ChessOnlineServer.csproj`
- risk: start/stop helpers must not commit stores, logs, key rings, or PID files
- test/verify plan: run the Windows smoke script against `ProductionOutput\ChessOnlineServer`, then run full verify

- topic: deployment artifact verification
- internet/source researched: local `scripts\verify.ps1` and production package layout
- key finding: verify checked Linux deploy templates but did not assert Windows deploy scripts or runbook presence
- decision for this repo: make Windows deploy scripts, Windows deploy templates, and the runbook part of the local and production verification gate
- concrete files affected: `scripts\verify.ps1`, `docs\CHESS3D_WINDOWS_SERVER_RUNBOOK.md`, `deploy\windows\README.md`
- risk: package checks must remain file-presence checks and avoid requiring runtime stores/secrets in `ProductionOutput`
- test/verify plan: targeted server package build plus full `scripts\verify.ps1`

## Phase 05 - Hetzner Linux Deployment Runbook

- topic: Hetzner Cloud firewall shape
- internet/source researched: Hetzner Docs, "Firewalls", https://docs.hetzner.com/cloud/firewalls/; Hetzner Cloud API docs, firewall note, https://docs.hetzner.cloud/reference/cloud
- key finding: Hetzner Cloud firewall rules must be intentionally configured; an empty inbound rule set blocks inbound traffic
- decision for this repo: document only ports 22, 80, and 443 as the public VPS firewall surface; keep Kestrel on a local loopback port behind Nginx
- concrete files affected: `docs/CHESS3D_HETZNER_LINUX_DEPLOYMENT_RUNBOOK.md`
- risk: publishing a server with a broad Kestrel bind would expose the authority endpoint without a reverse proxy plan
- test/verify plan: docs-only phase plus `git diff --check`

- topic: ASP.NET Core on Linux behind Nginx/systemd
- internet/source researched: Microsoft Learn, "Host ASP.NET Core on Linux with Nginx", https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx
- key finding: the documented production shape is Kestrel behind Nginx with a service manager such as systemd, including forwarded headers and WebSocket-aware reverse proxy config
- decision for this repo: write the Hetzner runbook around `/opt/chessonline/server`, systemd, Nginx, local app port `5077`, and health checks
- concrete files affected: `docs/CHESS3D_HETZNER_LINUX_DEPLOYMENT_RUNBOOK.md`
- risk: this remains a runbook scaffold until the Windows-native authority blocker is removed
- test/verify plan: docs-only phase plus CI

- topic: Ubuntu .NET runtime installation
- internet/source researched: Microsoft Learn, "Install .NET SDK or .NET Runtime on Ubuntu", https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install
- key finding: runtime-only servers should install the ASP.NET Core Runtime rather than a full SDK unless build tools are needed
- decision for this repo: document ASP.NET Core Runtime installation as the intended VPS path, while noting that the current package is not Linux-runnable yet
- concrete files affected: `docs/CHESS3D_HETZNER_LINUX_DEPLOYMENT_RUNBOOK.md`
- risk: installing the runtime does not solve the native `Chess3DEngine.dll` platform blocker
- test/verify plan: no command is executed against a VPS in P4C

- topic: ASP.NET Core Data Protection key persistence
- internet/source researched: Microsoft Learn, "Configure ASP.NET Core Data Protection", https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview; Microsoft Learn, "Key storage providers in ASP.NET Core", https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers
- key finding: Data Protection keys must be persisted consistently for auth/session continuity, and key storage is deployment-specific
- decision for this repo: document `/var/lib/chessonline/keyring` as the operator-owned key ring path and include it in backup/rollback instructions
- concrete files affected: `docs/CHESS3D_HETZNER_LINUX_DEPLOYMENT_RUNBOOK.md`
- risk: losing key-ring data can invalidate protected cookies/tokens even if the JSON store survives
- test/verify plan: docs-only phase with no secret/key files committed
