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
