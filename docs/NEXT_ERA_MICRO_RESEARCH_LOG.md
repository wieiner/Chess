# Next Era Micro-Research Log

This log records small source checks before each Next Era phase. It is intentionally concise: each entry ties a decision to a source, a repo action, and a verification plan.

## Phase 00 - Current State / Baseline / Pending Work Check

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| PowerShell host for baseline scripts | Microsoft Learn, "Install PowerShell on Windows, Linux, and macOS": https://learn.microsoft.com/en-us/powershell/scripting/install/install-powershell | PowerShell 7 is the cross-platform `pwsh` edition; Windows PowerShell 5.1 remains separate. | Prefer `pwsh` for Next Era local gates, while keeping script compatibility with Windows PowerShell where CI or existing scripts require it. | `docs/NEXT_ERA_BASELINE_STATUS.md` | Run `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List`, Native, Online, and `scripts\verify.ps1`. |
| Controlled MSBuild parallelism | Microsoft Learn, "MSBuild Command-Line Reference": https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference and "Build multiple projects in parallel with MSBuild": https://learn.microsoft.com/en-us/visualstudio/msbuild/building-multiple-projects-in-parallel-with-msbuild | `-maxcpucount`/`/m` controls parallel project builds; using a value makes node count explicit. | Treat bare `/m` as forbidden for project scripts. Baseline records actual controlled `/m:N` behavior and notes contention symptoms if observed. | `docs/NEXT_ERA_BASELINE_STATUS.md` | Use `-MSBuildMaxCpuCount 1` for baseline suite gates; verify may use its configured default and record result. |
| Test timeout discipline | Microsoft Learn, "dotnet test command" and VSTest timeout options: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test and https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest | Official .NET test tooling supports explicit timeout concepts; unbounded test execution is not an acceptable gate. | Keep the repository's C# `TestProcessWatchdog` as the executable timeout authority for custom contract test runners. | `docs/NEXT_ERA_BASELINE_STATUS.md` | Confirm `run-tests.ps1` lists per-test timeouts and that Native/Online tests complete through bounded executable runs. |
| Current CI baseline | GitHub Actions run list via `gh run list --limit 10` | Latest `main` run `27878825241` is green for commit `77d1401`. | Start Next Era from `77d1401` with no local commits ahead/behind. | `docs/NEXT_ERA_BASELINE_STATUS.md` | Record `HEAD`, `origin/main`, local gate results, and last CI status. |

## Phase 01 - Test Runner / Verify Hardening Finalization

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Process watchdog termination | Microsoft Learn, `System.Diagnostics.Process.Kill`: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill | .NET exposes `Kill(Boolean)` for terminating a process and optionally its descendants. | Keep `tools/TestProcessWatchdog` as the only authority for test executable timeouts; PowerShell remains orchestration only. | `docs/NEXT_ERA_TEST_RUNNER_OPERATIONS.md` | Run `-Only SignalR` and broader suites through `run-tests.ps1`; inspect per-test watchdog output. |
| Redirected process output | Microsoft Learn, `ProcessStartInfo.RedirectStandardOutput`: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardoutput | Microsoft documents deadlock risk with redirected output and recommends asynchronous read patterns. | Do not revive a PowerShell pipe-reading timeout wrapper. Keep file-backed stdout/stderr logs and async stream copy inside the C# watchdog. | `docs/NEXT_ERA_TEST_RUNNER_OPERATIONS.md` | Confirm `.tmp/test-logs/*.stdout.log` and `*.stderr.log` are written for executed tests. |
| PowerShell execution policy | Microsoft Learn, `about_Execution_Policies`: https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies | Execution policy controls script loading behavior and is a safety feature, not a replacement for script trust. | Continue using `-NoProfile -ExecutionPolicy Bypass` for repo-local scripted gates to avoid profile noise while keeping commands explicit. | `docs/NEXT_ERA_TEST_RUNNER_OPERATIONS.md`, `docs/TESTING.md` | Use explicit `pwsh -NoProfile -ExecutionPolicy Bypass -File ...` commands in docs and checks. |
| MSBuild node reuse | Microsoft Learn, MSBuild Server: https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-server | `/nr:false` disables node reuse for a command-line build invocation. | Keep `/nr:false` in runner/verify and document stale-process diagnostics for exceptional cleanup only. | `scripts/diagnostics/Find-StaleBuildProcesses.ps1`, `scripts/diagnostics/Stop-StaleBuildProcesses.ps1.template`, `docs/NEXT_ERA_TEST_RUNNER_OPERATIONS.md` | Run `Find-StaleBuildProcesses.ps1`; do not run the stop template in normal gates. |

## Phase 02 - Hetzner Linux Server Current Reality Check

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| ASP.NET Core Linux hosting | Microsoft Learn, "Host ASP.NET Core on Linux with Nginx": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | Microsoft documents the expected shape as Kestrel behind Nginx reverse proxy on Linux. | Treat Kestrel-only smoke as the next proof step; do not claim production readiness until systemd and Nginx are installed and verified. | `docs/NEXT_ERA_HETZNER_REALITY_CHECK.md` | Read-only SSH probe of toolchain, ports, paths, and service state. |
| Data Protection keyring | Microsoft Learn, ASP.NET Core Data Protection configuration: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview | File-based key rings must be protected with filesystem permissions and limited to the app. | Keep `/var/lib/chessonline/keyring` as a future production layout path; do not create or commit runtime keys in this phase. | `docs/NEXT_ERA_HETZNER_REALITY_CHECK.md` | Verify production keyring path does not exist yet. |
| SignalR deployment | Microsoft Learn, ASP.NET Core SignalR hosting and scaling: https://learn.microsoft.com/en-us/aspnet/core/signalr/scale | SignalR deployment needs explicit hosting/proxy awareness; this stage does not introduce a backplane. | Record that remote SignalR/Asgard smoke is still pending until Kestrel package and proxy path exist. | `docs/NEXT_ERA_HETZNER_REALITY_CHECK.md` | No SignalR mutation in Phase 02; only read current server state. |
| Health endpoints | Microsoft Learn, ASP.NET Core health checks: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks | Health checks are the standard lightweight probe before deeper functional smoke. | Next deployment phases should prove `/healthz/live`, `/healthz/ready`, and `/chess3d/diagnostics` before auth/matchmaking. | `docs/NEXT_ERA_HETZNER_REALITY_CHECK.md` | Record that no server is currently listening on `127.0.0.1:5077`. |

## Phase 03 - Linux Server Package Completion

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| `dotnet publish` with RID | Microsoft Learn, `dotnet publish`: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish and RID catalog: https://learn.microsoft.com/en-us/dotnet/core/rid-catalog | `dotnet publish -r <RID> --self-contained false` creates a framework-dependent RID-specific package. `linux-x64` is the target RID. | Keep `Publish-ChessOnlineServer-Linux.ps1` framework-dependent and target `linux-x64`; do not commit the native `.so`. | `scripts/deploy/Publish-ChessOnlineServer-Linux.ps1`, `docs/NEXT_ERA_LINUX_PACKAGE_RESULT.md` | Publish with a tested temporary `libChess3DEngine.so`, then inspect package contents. |
| ASP.NET Core publish directory | Microsoft Learn, ASP.NET Core directory structure: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/directory-structure | The publish directory is the deployment content root; it contains application files, dependencies, config, and static assets. | Treat `DeploymentOutput/linux-x64/ChessOnlineServer` as the package root for the next Kestrel smoke. | `docs/NEXT_ERA_LINUX_PACKAGE_RESULT.md` | Assert server DLL, native SO, sample config, profiles, scenarios, and deploy templates exist. |
| Native library loading | Microsoft Learn, native library loading: https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading | P/Invoke/native loading depends on platform-specific library names and probing paths. | Canonical Linux package file must be `libChess3DEngine.so`, not the temporary source filename. | `scripts/deploy/Publish-ChessOnlineServer-Linux.ps1` | Republish after script fix and assert only canonical `libChess3DEngine.so` remains. |
| MSBuild publish properties | Microsoft Learn, MSBuild properties: https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-properties | Command-line properties override project values but publish target behavior can vary with output path properties. | Make the publish script explicitly copy/verify the `.so` after publish rather than relying only on the csproj `AfterTargets=Publish` copy. | `scripts/deploy/Publish-ChessOnlineServer-Linux.ps1`, `scripts/verify.ps1` | Optional `CHESS_VERIFY_LINUX_PACKAGE=1` gate can verify package if `CHESS3D_LINUX_NATIVE_LIBRARY` is supplied. |

## Phase 04 - Hetzner Kestrel Smoke, Temp Mode

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Kestrel behind future proxy | Microsoft Learn, "Host ASP.NET Core on Linux with Nginx": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | Microsoft recommends Kestrel behind a reverse proxy for production, but a direct loopback Kestrel smoke is a useful first proof. | Run only temporary `127.0.0.1:5077` Kestrel smoke in `/tmp/chessonline-smoke`; leave systemd/Nginx for later phases. | `docs/NEXT_ERA_HETZNER_KESTREL_SMOKE_RESULT.md` | Copy package, start Kestrel, curl live/ready/diagnostics, stop process, verify port freed. |
| Production environment variables | Microsoft Learn, ASP.NET Core configuration: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration | Double underscore environment variables map to nested configuration keys. | Use `HostedOnline__...` environment variables for temp store/keyring/auth configuration; do not write secrets into repo. | `docs/NEXT_ERA_HETZNER_KESTREL_SMOKE_RESULT.md` | Inspect health and diagnostics output; keep runtime store/keyring only under `/tmp`. |
| Data Protection key warning | Microsoft Learn, Data Protection configuration overview: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview | Unencrypted file keyrings are acceptable only with proper environment/permission choices; warning is expected if no encryptor is configured. | Treat the temp keyring warning as acceptable for smoke only; production-like `/var/lib/chessonline/keyring` needs restricted ownership later. | `docs/NEXT_ERA_HETZNER_KESTREL_SMOKE_RESULT.md` | Record warning and defer production keyring hardening to systemd/prod-layout phases. |
| Health and diagnostics | Microsoft Learn, ASP.NET Core health checks: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks | Health endpoints are a minimal readiness signal before functional API/SignalR smoke. | Require `/healthz/live`, `/healthz/ready`, and `/chess3d/diagnostics` to pass before remote auth/matchmaking smoke. | `docs/NEXT_ERA_HETZNER_KESTREL_SMOKE_RESULT.md` | Curl all three endpoints over SSH against loopback. |

## Phase 05 - Remote SignalR Asgard Smoke

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| SignalR .NET client | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | The .NET client can connect to hubs and invoke hub methods through `HubConnection`. | Add a small `net8.0` smoke client instead of trying to drive SignalR from shell/curl. | `tools/HetznerSignalRSmoke` | Build the tool and run it through the existing C# watchdog. |
| SignalR authentication | Microsoft Learn, "Authentication and authorization in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz | SignalR clients can supply bearer tokens through an access-token provider. | Register two ephemeral users, connect both with access tokens, and avoid printing tokens/passwords. | `tools/HetznerSignalRSmoke`, `scripts/deploy/Test-HetznerSignalRMatchmaking.ps1` | Smoke must verify authenticated Hello, Asgard matchmaking, snapshot, action, and action log. |
| Health checks before hub smoke | Microsoft Learn, ASP.NET Core health checks: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks | Health/readiness endpoints are the first safe probe before functional traffic. | The smoke client checks live/ready/diagnostics before registering users or opening SignalR connections. | `tools/HetznerSignalRSmoke` | Fail fast if the remote temp server is not ready or if Linux native authority is missing. |
| Environment variable hierarchy | Microsoft Learn, ".NET configuration providers": https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers | Double underscore is the portable nested-key separator for environment variables. | Reuse the Phase 04 temp Kestrel environment and keep all runtime store/keyring files under `/tmp`. | `docs/NEXT_ERA_REMOTE_SIGNALR_ASGARD_SMOKE_RESULT.md` | Start temp server by SSH, then access it through local-forwarded loopback. |

## Phase 06 - Production-Like Linux Layout

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Linux application layout | Filesystem Hierarchy Standard 3.0: https://refspecs.linuxfoundation.org/FHS_3.0/fhs/index.html | `/opt` is appropriate for add-on application packages, while `/var/lib` and `/var/log` hold variable application state and logs. | Install package files under `/opt/chessonline/server`; keep mutable store/keyring/logs/backups under `/var/...`. | `docs/NEXT_ERA_PRODUCTION_LAYOUT_RESULT.md` | Create dirs, copy package, smoke as service user, and verify health on loopback. |
| ASP.NET Core Linux hosting | Microsoft Learn, "Host ASP.NET Core on Linux with Nginx": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | Production ASP.NET Core Linux hosting normally runs Kestrel behind a reverse proxy and a managed service. | Phase 06 still runs Kestrel directly on loopback as the `chessonline` user; systemd/Nginx are separate later phases. | `docs/NEXT_ERA_PRODUCTION_LAYOUT_RESULT.md` | No public port exposure; curl only `127.0.0.1:5077` over SSH. |
| Data Protection key permissions | Microsoft Learn, "Configure ASP.NET Core Data Protection": https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview | File-system key rings should be protected with permissions limited to the app identity. | Own `/var/lib/chessonline/keyring` by `chessonline:chessonline`; keep package files root-owned and read-only for the service user. | `docs/NEXT_ERA_PRODUCTION_LAYOUT_RESULT.md` | Verify owner/mode shape and do not commit key files. |
| Kestrel process boundary | Microsoft Learn, "Kestrel web server in ASP.NET Core": https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel | Kestrel is cross-platform and can bind to loopback for internal/proxy scenarios. | Run one foreground-style smoke as `chessonline`, write logs to `/var/log/chessonline`, then stop it. | `docs/NEXT_ERA_PRODUCTION_LAYOUT_RESULT.md` | Health and diagnostics must pass, then no listener remains on `:5077`. |

## Phase 07 - systemd Service

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| ASP.NET Core daemon hosting | Microsoft Learn, "Host ASP.NET Core on Linux with Nginx": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | Microsoft documents Linux hosting as Kestrel managed by a process manager and normally placed behind Nginx. | Install a loopback-only `chessonline.service` now; keep Nginx/public HTTP for Phase 08. | `deploy/linux/chessonline-server.service.template`, `docs/NEXT_ERA_SYSTEMD_SERVICE_RESULT.md` | `systemctl daemon-reload`, enable/restart, status, local health checks. |
| systemd service units | freedesktop.org `systemd.service`: https://www.freedesktop.org/software/systemd/man/systemd.service.html | A `.service` unit describes a process controlled and supervised by systemd. | Use `Type=simple`, `User=chessonline`, `WorkingDirectory=/opt/chessonline/server`, and full `dotnet ...dll` ExecStart. | `deploy/linux/chessonline-server.service.template` | Verify `systemctl status chessonline.service` and journal tail. |
| systemd execution context | freedesktop.org `systemd.exec`: https://www.freedesktop.org/software/systemd/man/systemd.exec.html | `WorkingDirectory`, `User`, and environment directives define the process execution context. | Put runtime paths in environment variables and preserve app package as root-owned read-only files. | `deploy/linux/chessonline-server.service.template` | Health diagnostics should report `/opt/chessonline/server/libChess3DEngine.so`. |
| Data Protection permissions | Microsoft Learn, Data Protection configuration: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview | File key rings should be access-limited to the app identity. | Keep `/var/lib/chessonline/keyring` owned by `chessonline`, not root-readable package content. | `docs/NEXT_ERA_SYSTEMD_SERVICE_RESULT.md` | Do not commit keys; only document owner/path shape. |

## Phase 08 - Nginx Reverse Proxy / Public HTTP

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| ASP.NET Core behind Nginx | Microsoft Learn, "Host ASP.NET Core on Linux with Nginx": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | Nginx should forward normal HTTP and WebSocket upgrade traffic to Kestrel. | Install a simple port-80 Nginx reverse proxy to loopback Kestrel; keep TLS for the next phase. | `deploy/linux/nginx-chessonline.conf.template`, `docs/NEXT_ERA_NGINX_PUBLIC_HTTP_RESULT.md` | `nginx -t`, local `curl http://127.0.0.1/...`, external HTTP health. |
| Forwarded headers | Microsoft Learn, "Configure ASP.NET Core to work with proxy servers and load balancers": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer | Apps behind a proxy should process `X-Forwarded-For` and `X-Forwarded-Proto` from trusted proxies. | Enable forwarded headers for loopback Nginx only; do not trust arbitrary public proxies. | `src/ChessOnlineServer/ChessOnlineServerHost.cs` | Windows build/CI plus remote diagnostics through Nginx. |
| WebSocket proxying | Microsoft Learn SignalR hosting notes: https://learn.microsoft.com/en-us/aspnet/core/signalr/scale | SignalR needs proxy support for WebSocket upgrade and long-running connections. | Keep `Upgrade`, `Connection`, and long `proxy_read_timeout` in the Nginx template. | `deploy/linux/nginx-chessonline.conf.template` | Remote health first; SignalR through public HTTP remains a later hardening check unless needed. |
| Public HTTP boundary | Microsoft Learn ASP.NET Core security/auth guidance and project security baseline | Token issuance over public HTTP is not production-safe. | Public HTTP is a diagnostic/dev exposure only until TLS/domain is configured; do not use real accounts. | `docs/NEXT_ERA_TLS_DOMAIN_STATUS.md` later | Document TLS as blocked/deferred if no domain exists. |

## Phase 09 - TLS / Domain Status

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Domain validation requirement | Let's Encrypt, "How It Works": https://letsencrypt.org/how-it-works/ | An ACME client must prove control of a domain before a certificate can be issued. | Do not run certbot against a bare IP address or placeholder domain. | `docs/NEXT_ERA_TLS_DOMAIN_STATUS.md` | Documentation-only phase; no certbot commands are executed. |
| Certbot and Nginx | Certbot, Nginx instructions: https://certbot.eff.org/instructions?ws=nginx | Certbot can obtain and install Nginx certificates when a real domain and web server configuration exist. | Keep certbot as a future P4E action after DNS/domain confirmation. | `docs/NEXT_ERA_TLS_DOMAIN_STATUS.md` | Record commands as future operator steps, not as completed work. |
| ASP.NET Core behind reverse proxy | Microsoft Learn, "Host ASP.NET Core on Linux with Nginx": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | Nginx is a supported reverse proxy in front of Kestrel; HTTPS termination belongs at the proxy boundary. | Current public HTTP is diagnostic-only; token-bearing public auth must wait for HTTPS. | `docs/NEXT_ERA_TLS_DOMAIN_STATUS.md`, `docs/BUILD_AND_RELEASE.md` | `git diff --check` and CI after doc commit. |

## Phase 10 - Chess2D Portal Integration Path

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Lichess public APIs | Lichess API docs and tips: https://lichess.org/api and https://lichess.org/page/api-tips | Lichess exposes official HTTP/NDJSON APIs; live-play integrations must distinguish human Board API use from bot/engine use. | Treat Lichess as the first realistic live-play portal target, but only after token storage, PGN/FEN/SAN, and engine-assistance boundaries are explicit. | `docs/NEXT_ERA_CHESS2D_PORTAL_INTEGRATION_AUDIT.md` | Documentation-only audit plus Chess2D suite smoke. |
| Chess.com Published Data API | Chess.com support article, "What is the PubAPI and how do I use it?": https://support.chess.com/en/articles/9650547-what-is-the-pubapi-and-how-do-i-use-it | PubAPI is read-only JSON-LD and cannot send moves or commands. | Limit Chess.com integration to profile/archive/current-game reads unless an approved interactive API is separately provided. | `docs/NEXT_ERA_CHESS2D_PORTAL_INTEGRATION_AUDIT.md` | No network calls or credentials in this phase. |
| PGN / FEN interchange | PGN specification: https://www.saremba.de/chessgml/standards/pgn/pgn-complete.htm and FEN overview: https://www.chessprogramming.org/Forsyth-Edwards_Notation | PGN is the standard game-record exchange format; FEN is the standard single-position snapshot. | Implement FEN/PGN import/export as Phase A before portal move submission or replay bridge work. | `docs/NEXT_ERA_CHESS2D_PORTAL_INTEGRATION_AUDIT.md` | Audit current `Chess_SetFen`/`Chess_GetFen` coverage and missing PGN/SAN support. |
| UCI engine adapter | Shredder UCI overview: https://www.shredderchess.com/chess-features/uci-universal-chess-interface.html and Stockfish UCI docs: https://official-stockfish.github.io/docs/stockfish-wiki/UCI-%26-Commands.html | UCI is the common text protocol between chess GUIs/tools and engines. | Defer a UCI-compatible adapter to Phase B; do not confuse the current native ABI/search API with a real UCI process protocol. | `docs/NEXT_ERA_CHESS2D_PORTAL_INTEGRATION_AUDIT.md` | Documentation-only; no UCI executable added. |

## Phase 11 - Stalled Areas Audit

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Git branch/tag inventory | Git `branch` documentation: https://git-scm.com/docs/git-branch and Pro Git branching overview: https://git-scm.com/book/en/v2/Git-Branching-Branches-in-a-Nutshell | Branch/tag inventory is the correct first pass before declaring abandoned work or hidden release lines. | Audit local/remote refs before categorizing stalled areas; current source of truth is one `main` branch. | `docs/NEXT_ERA_STALLED_AREAS_AUDIT.md` | `git branch -a`, `git tag --list`, `git ls-remote --heads origin`, and recent graph log. |
| CI backlog visibility | GitHub Actions workflow syntax: https://docs.github.com/actions/using-workflows/workflow-syntax-for-github-actions | Workflows are repository-defined gates; stalled areas should distinguish CI coverage gaps from runtime gaps. | Keep Windows Build as the current green gate and list missing Linux/public deployment gates separately. | `docs/NEXT_ERA_STALLED_AREAS_AUDIT.md` | Documentation-only plus `git diff --check` and `run-tests.ps1 -List`. |
| SignalR scaling boundary | Microsoft Learn, SignalR overview and scale guidance: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction and https://learn.microsoft.com/en-us/aspnet/core/signalr/scale | SignalR is suitable for realtime server push; scale-out/backplane choices are explicit deployment architecture work. | Keep Redis/Azure SignalR/backplane as future debt; do not sneak it into the current single-server authority. | `docs/NEXT_ERA_STALLED_AREAS_AUDIT.md` | No runtime changes in this phase. |
| Secret handling | OWASP Secrets Management Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html | Secrets require controlled storage, rotation, and non-leakage discipline. | Treat TLS certificates, portal tokens, keyrings, stores, and VPS credentials as deployment/security debt, not repo artifacts. | `docs/NEXT_ERA_STALLED_AREAS_AUDIT.md` | Docs only; verify no secret artifacts are staged. |

## Phase 12 - Roadmap / Status Cleanup

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| ASP.NET Core Linux hosting status language | Microsoft Learn, "Host ASP.NET Core on Linux with Nginx": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | Linux hosting is Kestrel behind a process manager/reverse proxy; docs should distinguish proven service smoke from production hardening. | Update central docs to say Linux package/systemd/Nginx HTTP are proven, while TLS/domain/auth hardening remain blocked. | `docs/NEXT_ERA_PROJECT_MAP.md`, `docs/ARCHITECTURE.md`, `docs/BUILD_AND_RELEASE.md`, `docs/PROJECT_STATUS.md` | Documentation-only plus `git diff --check` and `run-tests.ps1 -List`. |
| .NET target framework boundary | Microsoft Learn, target frameworks: https://learn.microsoft.com/en-us/dotnet/standard/frameworks | `net8.0` and `net8.0-windows` express different runtime/API availability. | Keep server-side projects documented as portable `net8.0`; WPF clients remain `net8.0-windows`. | `docs/NEXT_ERA_PROJECT_MAP.md`, `docs/BUILD_AND_RELEASE.md` | No project-file changes in this phase. |
| GitHub Actions current gate | GitHub Actions workflow syntax: https://docs.github.com/actions/using-workflows/workflow-syntax-for-github-actions | CI gates are workflow-defined and should be reflected as the current source validation path. | Keep Windows Build as the current green CI gate and explicitly call out missing Linux CI/smoke gates. | `docs/TESTING.md`, `docs/NEXT_ERA_PROJECT_MAP.md` | `run-tests.ps1 -List` confirms bounded runner shape. |
| Secrets and deploy docs | OWASP Secrets Management Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html | Secrets and runtime key material must stay out of source control. | Project map should classify runtime stores/keyrings/certs/tokens as non-source artifacts. | `docs/NEXT_ERA_PROJECT_MAP.md`, `docs/BUILD_AND_RELEASE.md` | Verify only docs are staged; no runtime artifacts. |

## Phase 13 - Mode Incubator, No Runtime Modes Yet

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Architecture decision records | Microsoft Learn, "Maintain an architecture decision record": https://learn.microsoft.com/en-us/azure/well-architected/architect-role/architecture-decision-record | ADR-style docs record context, alternatives, consequences, and choices that may be hard to reverse. | Treat future mode ideas as an incubator document, not runtime profile changes. | `docs/NEXT_ERA_MODE_INCUBATOR.md` | Documentation-only plus `git diff --check`, profile count spot-check, and `run-tests.ps1 -List`. |
| README/documentation purpose | GitHub Docs, "About READMEs": https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes | Repository docs should help readers understand what the project does and how to use it. | Keep incubator concepts clearly labeled as non-runtime so the project map remains truthful. | `docs/NEXT_ERA_MODE_INCUBATOR.md`, `docs/PROJECT_STATUS.md`, `docs/ROADMAP.md` | No JSON profiles, schemas, or tests are added. |
| JSON Schema boundary | JSON Schema docs: https://json-schema.org/docs and specification: https://json-schema.org/specification | JSON Schema standardizes and validates JSON document structure. | Do not add schema enum values or profile descriptors for incubator ideas; schema changes wait until a real runtime mode phase. | `docs/NEXT_ERA_MODE_INCUBATOR.md` | Confirm no new `assets/rules/profiles/*.json` files. |

## Phase 14 - Final Server / Project Hardening Report

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Health endpoint reporting | Microsoft Learn, ASP.NET Core health checks: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks | Health endpoints are the safe final probe before claiming a server is reachable. | Final report records public `/healthz/live`, `/healthz/ready`, and `/chess3d/diagnostics` results. | `docs/NEXT_ERA_FINAL_REPORT.md` | Probe public HTTP and loopback over SSH with short timeouts. |
| systemd service status | freedesktop.org `systemd.service`: https://www.freedesktop.org/software/systemd/man/systemd.service.html | `systemctl is-active` and `is-enabled` provide concise service state checks. | Final report records `chessonline.service` as active/enabled only if the read-only probe confirms it. | `docs/NEXT_ERA_FINAL_REPORT.md` | SSH read-only `systemctl` probe; no service mutation. |
| Nginx reverse proxy status | Microsoft Learn, "Host ASP.NET Core on Linux with Nginx": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | Nginx should proxy external HTTP/WebSocket traffic to loopback Kestrel. | Final report records Nginx active state and `nginx -t`; TLS remains future work until a domain exists. | `docs/NEXT_ERA_FINAL_REPORT.md` | SSH read-only Nginx status/config test and public HTTP probe. |
| GitHub Actions final gate | GitHub Actions workflow syntax: https://docs.github.com/actions/using-workflows/workflow-syntax-for-github-actions | CI run IDs are the durable external proof for pushed commits. | Final report lists phase commit/run IDs and waits for the Phase 14 run after push. | `docs/NEXT_ERA_FINAL_REPORT.md` | Local `run-tests.ps1`, `verify.ps1`, then push and `gh run watch`. |

## Phase 15 - Current Reachability Refresh

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Firewall reachability boundary | Ubuntu Server documentation, "Firewall": https://documentation.ubuntu.com/server/how-to/security/firewalls/ | A host firewall can block inbound ports even when a local service is listening. | Record that Nginx is listening locally but external port 80 is not reachable while `ufw` omits `80/tcp`. Do not change firewall policy in a docs refresh. | `docs/NEXT_ERA_FINAL_REPORT.md`, `docs/NEXT_ERA_PROJECT_MAP.md` | Read-only `ufw status`, `ss`, loopback curl, and external TCP probe. |
| Nginx/service distinction | Microsoft Learn, "Host ASP.NET Core on Linux with Nginx": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | Kestrel, Nginx, and firewall/public reachability are separate deployment layers. | Keep the report precise: `chessonline.service` and Nginx are healthy, while public HTTP is currently blocked externally. | `docs/NEXT_ERA_FINAL_REPORT.md`, `docs/NEXT_ERA_PROJECT_MAP.md` | SSH read-only service/Nginx checks plus local workstation public probe. |
| Sensitive endpoint notation | OWASP Secrets Management Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html | Operational details should avoid leaking unnecessary environment-specific secrets and identifiers. | Replace the real VPS IP in tracked operator examples with `<HETZNER_HOST>`. | `docs/NEXT_ERA_FINAL_REPORT.md` | `rg` for the raw host IP in tracked docs. |

## Phase 16 - Hetzner SignalR Smoke Tooling Fix

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| SignalR .NET client smoke | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | The .NET client is the appropriate tool for invoking hub methods and using bearer auth. | Keep the real SignalR workflow in `tools/HetznerSignalRSmoke`; make PowerShell a thin operator wrapper. | `tools/HetznerSignalRSmoke/Program.cs`, `scripts/deploy/Test-HetznerSignalRMatchmaking.ps1` | Build smoke tool and run real public HTTP Asgard smoke. |
| Auth endpoint safety | Microsoft Learn, "Authentication and authorization in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz | SignalR clients can provide bearer tokens while the transport and auth endpoint policy remain deployment-sensitive. | Generate temporary users/passwords, login both, do not print tokens, and keep HTTP use marked diagnostic-only until TLS exists. | `tools/HetznerSignalRSmoke/Program.cs`, `docs/NEXT_ERA_HETZNER_USAGE.md` | `-NoSecretLog` smoke run; inspect stdout for sanitized step output only. |
| Script parameter compatibility | Microsoft Learn, PowerShell advanced parameters: https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_functions_advanced_parameters | Operator scripts should expose explicit parameters and remain parseable without running network calls. | Add `-BaseUrl`, `-ServerUrl`, `-ProfileId`, `-DryRun`, `-NoSecretLog`, and `-SkipActionSubmit`; keep legacy tunnel parameters. | `scripts/deploy/Test-HetznerSignalRMatchmaking.ps1` | Script parse test and dry-run tests for both URL parameters. |

## P4F Phase 00 - Playable Online Client Baseline

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| HTTP health probes | Microsoft Learn, "Health checks in ASP.NET Core": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks | ASP.NET Core health checks are exposed as HTTP endpoints and are appropriate for liveness/readiness probes. | Use `/healthz/live`, `/healthz/ready`, and `/chess3d/diagnostics` as the P4F baseline before UI work. | `docs/P4F_PLAYABLE_ONLINE_CLIENT_BASELINE.md` | `curl.exe --connect-timeout 10` for all three public HTTP endpoints. |
| SignalR over diagnostic HTTP | Microsoft Learn, "Security considerations in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/security | SignalR deployments must account for token handling and transport security. | Keep HTTP 80 as diagnostic/dev only; P4F UI must not print tokens or encourage real credentials. | `docs/P4F_PLAYABLE_ONLINE_CLIENT_BASELINE.md` | Dry-run the smoke wrapper and keep real token-bearing smoke under `-NoSecretLog`. |
| CI baseline | GitHub Actions workflow syntax: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax | GitHub Actions workflow runs are the current external repository gate. | Record the latest green Windows Build run before starting P4F UI work. | `docs/P4F_PLAYABLE_ONLINE_CLIENT_BASELINE.md` | `gh run list --limit 10` and phase commit CI. |

## P4F Phase 01 - Online Client UI Boundary Audit

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| SignalR WPF client shape | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | The .NET client is the supported path for WPF/desktop clients to build a `HubConnection`, register hub callbacks, and invoke hub methods asynchronously. | Keep `ChessOnlineApp` as the practical P4F MVP surface because it already references `Microsoft.AspNetCore.SignalR.Client` and has a P3F hub panel. | `docs/P4F_ONLINE_CLIENT_UI_AUDIT.md` | Documentation-only audit plus `run-tests.ps1 -List`. |
| Bearer token SignalR auth | Microsoft Learn, "Authentication and authorization in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz | SignalR clients can provide bearer tokens when the connection is established; token handling remains transport/security sensitive. | Add P4F auth/test-user UI through HTTP auth endpoints and pass access tokens to the hub without logging them. | `docs/P4F_ONLINE_CLIENT_UI_AUDIT.md` | Future phases test token redaction and construction without network in CI. |
| WPF incremental UI layout | Microsoft Learn, "Layout - WPF": https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/layout | WPF layout is panel-driven and can be extended incrementally when controls remain grouped and resizable. | Extend the existing ChessOnlineApp panels rather than embedding the full Chess3D visual board during the first playable online MVP. | `docs/P4F_ONLINE_CLIENT_UI_AUDIT.md` | Build ChessOnlineApp in later implementation phases. |

## P4F Phase 02 - Shared Online Client SDK Layer

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| SignalR client boundary | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | A typed desktop/client layer can wrap `HubConnection` setup, callbacks, and method invocation. | Add `src/ChessOnlineClient` as a `net8.0` reusable layer for WPF, smoke tools, and future tests. | `src/ChessOnlineClient`, `docs/P4F_ONLINE_CLIENT_SDK.md` | Build project and run online contract tests without remote network. |
| Bearer token handoff | Microsoft Learn, "Authentication and authorization in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz | Bearer tokens are supplied through an access token provider at connection time. | Store tokens only in `ChessOnlineClientSession` memory and pass them to `ChessOnlineRelayClient`; never log token values. | `src/ChessOnlineClient/ChessOnlineRelayClient.cs`, `src/ChessOnlineClient/ChessOnlineClientSession.cs` | Contract tests verify redaction and construction without network. |
| Client-side secret redaction | Microsoft Learn, "Security considerations in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Token-bearing SignalR flows require careful logging boundaries. | Centralize token/password redaction in `ChessOnlineSecretRedactor` before adding P4F UI logs. | `src/ChessOnlineClient/ChessOnlineSecretRedactor.cs` | Contract tests ensure access/refresh token/password fragments are not preserved in event logs. |

## P4F Phase 03 - ChessOnlineApp Server Connection Panel

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Health UI | Microsoft Learn, "Health checks in ASP.NET Core": https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks | Health checks are lightweight enough for an operator UI to call before opening a realtime connection. | Add `Check Health` and `Check Diagnostics` buttons to `ChessOnlineApp` before auth/matchmaking panels. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp`; no remote call in CI. |
| Desktop SignalR connect | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | Desktop clients should create a `HubConnection` from a concrete hub URL and surface connection errors. | Normalize P4F base URL to `/chess3d/relay`, while preserving the old direct hub URL fallback. | `src/ChessOnlineApp/MainWindow.xaml.cs` | Build app and keep existing P3F hub buttons working. |
| Diagnostic HTTP warning | Microsoft Learn, "Security considerations in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Token-bearing realtime traffic over public HTTP must be treated as non-production. | Show an inline HTTP 80 diagnostic-only warning in the connection panel; TLS/443 remains deferred. | `src/ChessOnlineApp/MainWindow.xaml`, `docs/P4F_CHESSONLINEAPP_CONNECTION_PANEL.md` | Manual UI smoke later; no secrets committed. |

## P4F Phase 04 - Temporary Auth Panel

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| SignalR bearer auth in clients | Microsoft Learn, "Authentication and authorization in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz | The SignalR .NET client can provide access tokens when the hub connection is created. | Have `ChessOnlineApp` pass the in-memory primary test-user access token to `/chess3d/relay` when connecting. | `src/ChessOnlineApp/MainWindow.xaml.cs` | Build app; later manual smoke verifies server auth. |
| HTTP auth safety | Microsoft Learn, "Security considerations in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Public HTTP token flows need clear non-production warnings and careful logging. | Add temp-user and manual-login controls with token/password values kept out of logs and source files. | `src/ChessOnlineApp/MainWindow.xaml`, `docs/P4F_AUTH_TEST_USERS.md` | Build app and inspect tracked files for no token values. |
| Test-user UX | Microsoft Learn, WPF layout overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/layout | Small grouped panels are appropriate for incremental desktop workflows. | Add an `Auth / Test Users` panel instead of a larger redesign. | `src/ChessOnlineApp/MainWindow.xaml` | Build app. |

## P4F Phase 05 - Matchmaking Panel

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Two-client SignalR smoke UX | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | A desktop process can own multiple `HubConnection` instances for operator/test workflows. | Add `Create Test Match With Two Local Clients` using the SDK relay client and current five-profile selector. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build app; remote UI smoke remains manual. |
| Authenticated matchmaking | Microsoft Learn, "Authentication and authorization in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz | Authenticated hub methods depend on the access token bound to each connection. | Use two in-memory temporary users for the two local clients; do not print tokens/passwords. | `src/ChessOnlineApp/MainWindow.xaml.cs`, `docs/P4F_MATCHMAKING_PANEL.md` | Build app. |
| Incremental WPF control center | Microsoft Learn, WPF layout overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/layout | Additional controls can be grouped without replacing the whole window. | Add match buttons/status under the existing P3F panel instead of redesigning the app. | `src/ChessOnlineApp/MainWindow.xaml` | Build app. |

## P4F Phase 06 - Snapshot Viewer

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Snapshot/action log UI | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | Client-side hub method results can be projected into small desktop status views without changing the server contract. | Add compact snapshot status, action counters, and action-log list to `ChessOnlineApp`. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build app. |
| Safe action smoke | Project remote smoke result and existing online authority contract | The proven Asgard smoke action is a normal move from `(2,3,0)` to `(2,3,1)` with expected state hash. | Add `Submit Safe Asgard Test Action` for the Asgard profile only. | `src/ChessOnlineApp/MainWindow.xaml.cs`, `docs/P4F_ONLINE_SNAPSHOT_VIEWER.md` | Build app; remote UI smoke later. |
| Session report safety | OWASP Secrets Management Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html | Runtime reports must avoid credentials and tokens. | Save sanitized client session reports under ignored `.tmp/manual-smoke`. | `src/ChessOnlineApp/MainWindow.xaml.cs` | No report files are committed. |

## P4F Phase 07 - Remote Smoke Result

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Operator smoke boundary | Existing `tools/HetznerSignalRSmoke` and P4F UI code | The command-line smoke remains the fastest reliable proof of remote auth/SignalR/matchmaking/action. | Run the public HTTP Asgard smoke after the UI MVP code and record sanitized results. | `docs/P4F_MANUAL_HETZNER_UI_SMOKE_RESULT.md` | Smoke PASS with `-NoSecretLog`; WPF click path remains manual. |
| Manual UI validation | Microsoft Learn, WPF layout overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/layout | WPF manual workflows should be documented when screenshot automation is out of scope. | Document exact click path rather than adding fragile UI automation. | `docs/P4F_MANUAL_HETZNER_UI_SMOKE_RESULT.md` | Local build plus operator manual smoke. |

## P4G Phase 00 - Realtime Online Board Baseline

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| P4F reality check | Existing `docs/P4F_PLAYABLE_ONLINE_CLIENT_FINAL_REPORT.md` and current `gh run list` | P4F ended green with a diagnostic UI path, but the board is still a compact snapshot/action viewer rather than a realtime playable board. | Start P4G from `7bbe1acc` and explicitly target board rendering, selection, preview, and server action dispatch. | `docs/P4G_REALTIME_ONLINE_BOARD_BASELINE.md` | `gh run list`, P4F smoke, `curl.exe` health checks. |
| Public HTTP SignalR boundary | Microsoft Learn, "Security considerations in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Public HTTP token-bearing SignalR is acceptable only as a diagnostic/dev path; tokens must not be logged and real passwords must not be used. | Keep P4G over HTTP 80 for hands-on testing only; leave TLS/domain/443 and x-ui/Xray untouched. | `docs/P4G_REALTIME_ONLINE_BOARD_BASELINE.md` | Smoke with `-NoSecretLog`; no remote smoke in CI. |
| Desktop realtime client path | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | The .NET SignalR client supports desktop apps, hub method calls, callbacks, and async flow needed for realtime board sync. | Continue incrementally in `ChessOnlineApp` and `src/ChessOnlineClient` instead of embedding or rewriting the full local Chess3D UI immediately. | `docs/P4G_REALTIME_ONLINE_BOARD_BASELINE.md` | Phase 01 audit before code changes. |

## P4G Phase 01 - Online Board Integration Boundary Audit

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Authoritative board projection | Existing `OnlineSnapshot.SaveGameJson` and `docs/CHESS3D_SAVEGAME_FORMAT.md` | The online snapshot already embeds the v0.1 savegame with a 512-cell `projectedBoard`; no extra mode or server mutation is needed to render a first online board. | Add the first P4G board adapter as a read-only client-side parser over `SaveGameJson`, not as a new rules engine. | `docs/P4G_ONLINE_BOARD_INTEGRATION_AUDIT.md` | Documentation-only audit plus `run-tests.ps1 -List`. |
| Local Chess3D UI reuse boundary | Microsoft Learn, WPF layout and controls overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/layout | WPF can be extended incrementally with grouped controls and data-bound models; a large window embedding is not required for a first integrated board. | Do not embed `Chess3DWindow` into `ChessOnlineApp` in one jump; build an online board view model first. | `docs/P4G_ONLINE_BOARD_INTEGRATION_AUDIT.md` | Later phases build and smoke the app. |
| Realtime SignalR event flow | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | The desktop client can receive hub callbacks and invoke hub methods asynchronously, which is enough for snapshot refresh after accepted/rejected actions. | Use existing `ChessOnlineRelayClient` events as the sync source, then add ordered board refresh/gap handling incrementally. | `docs/P4G_ONLINE_BOARD_INTEGRATION_AUDIT.md` | Later phases add client tests without remote smoke in CI. |

## P4G Phase 02 - Online Board Snapshot Adapter

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Savegame-to-board adapter | Existing `docs/CHESS3D_SAVEGAME_FORMAT.md` and `OnlineSnapshot.SaveGameJson` | The authoritative snapshot already contains all projected top-piece cells needed for a first online board view. | Add `OnlineChess3DBoardSnapshotParser` in `src/ChessOnlineClient` and keep it read-only. | `src/ChessOnlineClient/OnlineChess3DBoardSnapshot.cs`, `docs/P4G_ONLINE_BOARD_SNAPSHOT_ADAPTER.md` | `ChessOnlineContractTests` parse a real authority snapshot and malformed snapshot. |
| JSON parsing boundary | Microsoft Learn, `System.Text.Json` DOM docs: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-dom | `JsonDocument` supports lightweight read-only parsing without creating a new protocol dependency. | Parse only the required savegame fields and fail cleanly for malformed or incomplete snapshots. | `src/ChessOnlineClient/OnlineChess3DBoardSnapshot.cs` | Managed online contract tests. |

## P4G Phase 03 - Online Board Renderer

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| WPF board surface | Microsoft Learn, WPF `UniformGrid` and panel layout docs: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/panels-overview | A compact grid can render a first board slice without embedding the full local `Chess3DWindow`. | Add a `P4G Realtime Board Snapshot` slice grid to `ChessOnlineApp`. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs`, `docs/P4G_ONLINE_BOARD_RENDERER.md` | Build `ChessOnlineApp`; no remote smoke in CI. |
| Authoritative refresh | Existing P4F SignalR action/snapshot flow | Accepted actions already return action-log data, and the client can request a fresh snapshot immediately after acceptance. | Refresh the online board from server snapshot after safe Asgard action acceptance instead of mutating locally. | `src/ChessOnlineApp/MainWindow.xaml.cs` | Build app and remote smoke remains manual/operator. |

## P4G Phase 04 - Realtime Board Event Sync

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| SignalR callback surfacing | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | Hub callbacks are the correct desktop client mechanism for receiving pushed server state. | Expose `ChessOnlineRelayClient.MessageReceived` and let `ChessOnlineApp` react to `Receive*` callbacks. | `src/ChessOnlineClient/ChessOnlineRelayClient.cs`, `src/ChessOnlineApp/MainWindow.xaml.cs`, `docs/P4G_REALTIME_BOARD_EVENT_SYNC.md` | Build client/app; no remote smoke in CI. |
| Authoritative board sync | Existing `OnlineSnapshot.SaveGameJson` contract | Snapshot-bearing pushed events can redraw the online board without local engine mutation. | Parse snapshots from server callbacks and append action-log event notation, keeping direct Invoke results separate. | `src/ChessOnlineApp/MainWindow.xaml.cs` | Build app and keep P4F direct button flow intact. |

## P4G Phase 05 - Online Click-To-Move MVP

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| Server-authoritative move submit | Existing `OnlineActionCommand` and `OnlineGameSession.TryApply` | The protocol already supports `NormalMove` with source/target coordinates and expected state hash. | Add From/To selection controls to the P4G board and submit normal moves to the server without local mutation. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs`, `docs/P4G_ONLINE_CLICK_TO_MOVE_MVP.md` | Build `ChessOnlineApp`; remote/manual smoke remains operator-driven. |
| Honest legal preview boundary | Existing P4G audit | Online legal preview is not exposed yet, so the client must show server rejection text instead of pretending to know legality. | Keep the first click-to-move path as server-accepted/rejected MVP; legal target highlighting is a later append-only protocol step. | `docs/P4G_ONLINE_CLICK_TO_MOVE_MVP.md` | Build app; no protocol change. |

## P4G2 Phase 00 - Current Online Playability Baseline

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Desktop SignalR client baseline | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | The .NET client is the supported desktop path for async hub calls and callbacks. | Keep P4G2 on `ChessOnlineApp` + `ChessOnlineClient` and measure current playability before adding legal preview. | `docs/P4G2_CURRENT_PLAYABILITY_BASELINE.md` | Public HTTP health, smoke, app build. |
| Public HTTP security boundary | Microsoft Learn, "Security considerations in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Public HTTP with auth tokens is diagnostic-only and requires careful log redaction. | Keep HTTP 80 for dev smoke only; use temp users; do not log tokens/passwords; leave TLS/443 deferred. | `docs/P4G2_CURRENT_PLAYABILITY_BASELINE.md` | Smoke with `-NoSecretLog`; no secrets committed. |
| WPF incremental playability | Microsoft Learn, WPF data binding overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/ | WPF UI can evolve through view state and bindings/controls without a large rewrite. | Treat the P4G board renderer as a stepping stone toward legal target/turn UI, not as a finished game UX. | `docs/P4G2_CURRENT_PLAYABILITY_BASELINE.md` | `dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64`. |

## P4G2 Phase 01 - UI Playability Gap Audit

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF UI state model | Microsoft Learn, WPF data binding overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/ | Playable desktop UI should expose state directly instead of relying on operator-only logs and manual coordinate entry. | Document the current From/To workflow as a gap and target source-click legal preview next. | `docs/P4G2_UI_PLAYABILITY_GAP_AUDIT.md` | Documentation-only phase plus CI after commit. |
| SignalR callback UX | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | Hub callbacks can keep multiple desktop clients synchronized, but UI must surface connection/state changes clearly. | Treat current event sync as minimal and document missing duplicate/gap/resync UX. | `docs/P4G2_UI_PLAYABILITY_GAP_AUDIT.md` | Documentation-only phase. |
| HTTP/token safety | Microsoft Learn, "Security considerations in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Public HTTP auth flows must remain diagnostic-only and avoid leaking credentials through logs. | Keep P4G2 UI work on temporary users and sanitized logs only. | `docs/P4G2_UI_PLAYABILITY_GAP_AUDIT.md` | No secret artifacts committed. |

## P4G2 Phase 02 - Online Legal Preview Contract Audit

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Source-cell legal preview | Existing native `Chess3D_BuildLegalActionPreviewForCell` ABI and `NativeChess3DEngine` wrapper | The local engine already has a non-mutating selected-cell preview DTO with kind, coordinates, flags, piece codes, side, and reason code. | Define online legal preview as a server-side wrapper over the existing native preview, not a new rules engine. | `docs/P4G2_LEGAL_PREVIEW_CONTRACT_AUDIT.md` | Documentation-only phase plus `git diff --check`. |
| SignalR request/response boundary | Microsoft Learn, "ASP.NET Core SignalR .NET client": https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | Hub methods can return typed results and also invoke client callbacks; request-only UI helpers do not need table-wide broadcast. | Plan append-only `RequestLegalPreview`/`LegalPreviewResult` protocol messages that reply to the caller only. | `docs/P4G2_LEGAL_PREVIEW_CONTRACT_AUDIT.md` | Future implementation tests for no mutation and stale hash handling. |
| Token-safe diagnostics | Microsoft Learn, "Security considerations in ASP.NET Core SignalR": https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Coordinates and state hashes are safe to log; bearer tokens and credentials are not. | Keep preview DTOs free of tokens/passwords and preserve HTTP 80 as diagnostic-only. | `docs/P4G2_LEGAL_PREVIEW_CONTRACT_AUDIT.md` | Secret scan remains part of later hardening. |

## P4G2 Phase 03 - Legal Preview DTOs

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| DTO compatibility | Existing `OnlineProtocolMessage` and `OnlineProtocolJson` contract | The protocol already tolerates future JSON fields while known message types are explicitly allow-listed. | Add legal preview DTOs append-only and register new message types without changing existing payloads. | `src/ChessOnlineProtocol/OnlineProtocolDtos.cs`, `src/ChessOnlineProtocol/OnlineProtocolJson.cs` | `ChessOnlineContractTests` JSON roundtrip. |
| JSON source generation | Microsoft Learn, `System.Text.Json` source generation: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation | DTOs used by trimmed/published apps should be represented in serializer metadata when the project keeps a context. | Add legal preview request/result/option/target/error to the protocol JSON context. | `src/ChessOnlineProtocol/OnlineProtocolDtos.cs` | Build and managed contract tests. |
| Secret-free preview payload | Microsoft Learn, SignalR security: https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Protocol payloads that can be logged should avoid bearer tokens and credentials. | Keep preview DTOs limited to table identity, coordinates, action shape, state hash, flags, and reasons. | `src/ChessOnlineProtocol/OnlineProtocolDtos.cs`, `docs/P4G2_LEGAL_PREVIEW_DTOS.md` | DTO tests assert no token/password fields are required. |

## P4G2 Phase 04 - Server Legal Preview Hub Method

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Caller-only preview response | Microsoft Learn, SignalR .NET client: https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | Hub methods can return a result and optionally invoke one client callback; table broadcast is unnecessary for local preview. | Add `RequestLegalPreview` as a caller-only hub method with `ReceiveLegalPreviewResult`. | `src/ChessOnlineServer/Chess3DRelayHub.cs` | Server build and online contract tests. |
| Server authority no-mutation | Existing `OnlineRoomRegistry.SubmitAction` state hash checks | The registry already guards accepted actions with hash and seat checks; preview needs the same table/seat boundary but must not advance server state. | Add registry/session legal preview path with state hash before/after assertion and no serverSeq increment. | `src/ChessOnlineProtocol/OnlineRoomRegistry.cs`, `src/ChessOnlineProtocol/OnlineGameSession.cs` | Tests for no mutation and stale hash. |
| Preview diagnostics safety | Microsoft Learn SignalR security: https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Coordinates and state hashes are safe UI diagnostics; tokens and auth headers are not. | Return preview data only; do not add token/password fields or broad server internals. | `docs/P4G2_SERVER_LEGAL_PREVIEW.md` | Secret/logging audit remains later. |

## P4G2 Phase 05 - Client Legal Preview Support

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Desktop SignalR invoke shape | Microsoft Learn, SignalR .NET client: https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | The existing client wrapper already centralizes `InvokeAsync` calls and callback registration. | Add `RequestLegalPreviewAsync` to `ChessOnlineRelayClient` using the same message construction path. | `src/ChessOnlineClient/ChessOnlineRelayClient.cs` | Build and contract tests without remote network. |
| WPF-friendly preview state | Microsoft Learn, WPF data binding overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/ | UI should bind to stable view state instead of walking raw protocol DTOs in every control handler. | Add small client-side legal preview state/marker view models. | `src/ChessOnlineClient/OnlineLegalPreviewState.cs` | Client model tests in `ChessOnlineContractTests`. |
| Token-safe event logs | Microsoft Learn SignalR security: https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Hub payload logging must not expose bearer tokens. | Preview event labels contain only message type, seq, and error reason through the existing redacted event log path. | `src/ChessOnlineClient/ChessOnlineRelayClient.cs` | Existing event log redaction tests plus preview state tests. |

## P4G2 Phase 06 - UI Legal Target Highlights

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF click/highlight state | Microsoft Learn, WPF data binding and controls: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/ | Incremental UI state can be kept as simple view models and refreshed on selection without embedding the full local Chess3D window. | Add legal-preview status/list and color markers to the existing P4G board slice. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp`. |
| SignalR callback threading | Microsoft Learn, SignalR .NET client: https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | Desktop callbacks can arrive away from the UI thread; UI changes must be marshalled through the dispatcher. | Keep preview display updates inside existing app event/dispatcher patterns. | `src/ChessOnlineApp/MainWindow.xaml.cs` | Build plus existing contract tests. |
| Preview safety boundary | Existing Phase 04 server no-mutation tests | Preview is authoritative metadata, not a local action. | UI highlights targets but still submits through `SubmitAction` later; no local board mutation. | `docs/P4G2_UI_LEGAL_TARGETS.md` | Manual remote smoke later; no remote smoke in CI. |

## P4G2 Phase 07 - One-Click Legal Dispatch

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Exact action dispatch | Existing `OnlineActionCommand` and Phase 05 preview option command mapping | Legal preview options already contain enough action shape to submit through the existing server authority. | Clicking a highlighted target submits the matching preview option instead of building a rough From/To command. | `src/ChessOnlineApp/MainWindow.xaml.cs` | Build app and contract tests. |
| Duplicate-submit safety | Microsoft Learn WPF command/control event basics: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/ | Rapid clicks can trigger duplicate async handlers unless the UI keeps a pending flag. | Add a pending submit guard around preview-option dispatch. | `src/ChessOnlineApp/MainWindow.xaml.cs` | Build app; manual smoke later. |
| Fallback UX | Existing P4G manual From/To flow | The rough coordinate flow remains useful for debugging while preview dispatch is young. | Preserve `Use Selected as From/To` and `Submit Normal Move` as advanced fallback. | `docs/P4G2_ONE_CLICK_LEGAL_DISPATCH.md` | Documentation plus local build. |

## P4G2 Phase 08 - Seat And Turn Model Audit

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Seat ownership | Existing `OnlineRoomRegistry.JoinTableSeat`, `ActorMatchesSeat`, and matchmaking status | Non-Hodge seats map seat index to side id; Hodge seats map seat index to macro-player. | Document the current mapping and use it for Phase 09 UI indicators instead of changing protocol semantics. | `docs/P4G2_SEAT_TURN_AUDIT.md` | Documentation-only phase plus CI. |
| Current turn data | Existing `OnlineSnapshot.TurnSummary` and `SaveGameJson` fields | Snapshot already carries turn summary, and savegame includes current side/macro-player parsed by the online board adapter. | Phase 09 can display current side/macro-player from existing snapshot data. | `docs/P4G2_SEAT_TURN_AUDIT.md` | Later UI build. |
| Wrong actor rejection | Existing `SubmitAction` and `RequestLegalPreview` actor checks | Server rejects wrong actor at authority boundary; UI should explain it before submit when possible. | Document "can act now" as a UI-derived hint, not a replacement for server validation. | `docs/P4G2_SEAT_TURN_AUDIT.md` | Future UI tests and manual smoke. |

## P4G2 Phase 09 - Seat And Turn UI

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF state display | Microsoft Learn, WPF data binding overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/ | A compact status line can expose computed UI state without a large MVVM rewrite. | Add a small seat/turn status helper and render it in the existing online tab. | `src/ChessOnlineClient/OnlineSeatTurnState.cs`, `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp` and run contract tests. |
| UI thread updates | Microsoft Learn, WPF threading model: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model | SignalR callbacks that update WPF controls must use the dispatcher. | Keep seat/turn refresh inside existing dispatcher-backed relay event handling. | `src/ChessOnlineApp/MainWindow.xaml.cs` | Build app; no remote smoke in CI. |
| Authority boundary | Existing `OnlineRoomRegistry.ActorMatchesSeat` and `OnlineMatchmakingStatus.Tickets` | Client-side "can act now" is only a usability hint; server still validates seat ownership and action legality. | Disable local submit when the primary player clearly does not own the current side/macro, but do not weaken server checks. | `src/ChessOnlineClient/OnlineSeatTurnState.cs`, `docs/P4G2_SEAT_TURN_UI.md` | Contract tests for disconnected, no snapshot, my turn, opponent turn, and Hodge macro turns. |

## P4G2 Phase 10 - Two-Window Manual Play Mode

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR client lifetime | Microsoft Learn, ASP.NET Core SignalR .NET client: https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | A desktop client can keep one hub connection per authenticated player and receive callbacks after `StartAsync`. | Add a primary-player manual relay flow for a single window instead of requiring the one-app secondary client. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build app and run online contract tests. |
| Window-local identity | Microsoft Learn, WPF application model/data binding docs: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/ | Per-window state should be held in instance fields, not static process-wide state. | Keep two-window mode on `_p4fPrimarySession/_p4fPrimaryRelay`; the second app instance owns its own independent fields. | `src/ChessOnlineApp/MainWindow.xaml.cs`, `docs/P4G2_TWO_WINDOW_PLAY_MODE.md` | Contract tests and manual instructions; no remote CI dependency. |
| Token/log safety | Microsoft Learn, SignalR security: https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Access tokens may be sent by the client but should not appear in logs. | Reuse existing `ChessOnlineClientSession` token handling and redacted status; new manual status logs show only short player ids. | `src/ChessOnlineApp/MainWindow.xaml.cs`, `docs/P4G2_TWO_WINDOW_PLAY_MODE.md` | Existing redaction tests plus build. |

## P4G2 Phase 11 - Realtime Resync Hardening

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR callback ordering | Microsoft Learn, ASP.NET Core SignalR .NET client: https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | Clients should treat callbacks as asynchronous notifications and maintain local sequence/state tracking. | Add a lightweight `OnlineRealtimeSyncState` that tracks server sequence, duplicate events, gaps, stale hashes, and snapshot hash. | `src/ChessOnlineClient/OnlineRealtimeSyncState.cs`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Contract tests for duplicate/gap/resync state. |
| WPF dispatcher | Microsoft Learn, WPF threading model: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model | SignalR callbacks cannot safely update WPF controls off the UI thread. | Keep realtime status rendering inside the existing `Dispatcher.Invoke` callback path. | `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp`. |
| Logging safety | OWASP Logging Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html | Diagnostic logs should avoid sensitive credentials and focus on operational state. | Realtime status logs include message type, seq, gap/duplicate/resync flags, and hashes, but no tokens/passwords. | `src/ChessOnlineClient/OnlineRealtimeSyncState.cs`, `docs/P4G2_REALTIME_RESYNC_HARDENING.md` | Existing redaction tests plus secret-free review. |

## P4G2 Phase 12 - Classic Online Play Path

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Phase 12R interrupted work recovery | Local Git worktree audit: `git status`, `git diff`, `git rev-parse` | The dirty tree contained intended Phase 12 smoke-tool/docs changes only. | Preserve and finish the interrupted work; do not reset or stash. | `docs/P4G2_PHASE12_RECOVERY_NOTE.md`, `tools/HetznerSignalRSmoke/Program.cs` | Build smoke tool, rerun Classic/Asgard smoke, targeted contract tests. |
| Remote smoke profile parameter | Existing `scripts/deploy/Test-HetznerSignalRMatchmaking.ps1` and `tools/HetznerSignalRSmoke` | The smoke wrapper accepts `-ProfileId`; the tool still has legacy "Asgard" labels but validates snapshot ruleset against the supplied profile. | Run the existing remote smoke against `classic-six-side-3d-8x8x8-v0.1` and document the result honestly. | `docs/P4G2_CLASSIC_ONLINE_PLAY_RESULT.md` | Remote smoke plus local contract tests. |
| Classic legal action path | Existing `OnlineRoomRegistry.BuildFirstLegalNormalMoveCommand` and legal preview tests | Classic online authority already exposes normal legal moves, stale hash checks, snapshots, and action-log replay. | Treat Classic as the clearest normal-move online path and document limitations separately from Asgard. | `docs/P4G2_CLASSIC_ONLINE_PLAY_RESULT.md` | `ChessOnlineContractTests` and GitHub Actions. |
| HTTP diagnostic boundary | Microsoft Learn SignalR security: https://learn.microsoft.com/en-us/aspnet/core/signalr/security | Auth over non-TLS HTTP is acceptable only as a controlled diagnostic/dev environment. | Keep remote Classic smoke as operator/dev validation only; no CI remote dependency and no real passwords. | `docs/P4G2_CLASSIC_ONLINE_PLAY_RESULT.md` | No secrets committed; smoke uses temporary users. |
| Deployed hub compatibility | Remote Hetzner smoke result, SignalR hub method error | The current public Hetzner build accepts health/auth/matchmaking/start/action but does not yet expose the newer `RequestLegalPreview` hub method. | Make the smoke tool prefer server legal preview and fall back to versioned known-safe actions for older deployed hubs. | `tools/HetznerSignalRSmoke/Program.cs`, `docs/P4G2_CLASSIC_ONLINE_PLAY_RESULT.md` | Classic and Asgard remote smokes both PASS. |

## P4G2 Phase 13 - Hetzner Server Version Gap Audit

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| ASP.NET Core behind Nginx deployment boundary | Microsoft Learn, Host ASP.NET Core on Linux with Nginx: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | Updating the app payload and restarting its service is separate from changing Nginx/TLS/firewall. | Audit only the ChessOnlineServer binary set and systemd service metadata in Phase 13; do not touch Nginx, UFW, 443, or x-ui/Xray. | `docs/P4G2_HETZNER_SERVER_VERSION_GAP.md` | Local server build, public health/diagnostics, read-only SSH file timestamps. |
| SignalR hub method availability | Microsoft Learn, ASP.NET Core SignalR hubs and .NET client: https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs | A missing hub method at runtime indicates the deployed server binary does not match the client/hub contract. | Treat `Method does not exist` as a server-version gap, not a client or rules bug. | `docs/P4G2_HETZNER_SERVER_VERSION_GAP.md` | `rg RequestLegalPreview`, local server build, remote smoke result. |

## P4G2 Phase 14 - Server Capabilities

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Append-only diagnostics contract | Existing `/chess3d/diagnostics` DTO and Microsoft Learn ASP.NET Core minimal APIs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis | Existing clients already read diagnostics as JSON; adding fields is backward-compatible. | Extend existing diagnostics JSON with feature flags instead of replacing the endpoint. | `src/ChessOnlineProtocol/OnlineProtocolDtos.cs`, `src/ChessOnlineServer`, `docs/P4G2_SERVER_CAPABILITIES.md` | Contract tests verify new flags and existing fields. |
| Operator feature visibility | GitHub Actions docs and existing smoke failures | Remote smoke should know whether fallback was needed because server lacks preview. | Expose `requestLegalPreview`, `matchmaking`, `actionLog`, and `realtimeResync` capability booleans. | diagnostics DTO/docs | Server build and online contract tests. |

## P4G2 Phase 15 - Hetzner Deploy Package

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Linux publish boundary | Microsoft Learn, .NET publish command and ASP.NET Core Linux hosting guidance | Framework-dependent `linux-x64` publish can package the server without changing Nginx/systemd/firewall. | Use existing deploy publish script or equivalent `dotnet publish`; package only server binaries/assets/native `.so`, not runtime stores/keyrings. | `docs/P4G2_HETZNER_DEPLOY_PACKAGE.md` | Inspect package contents and exclude secrets before copy/deploy. |
| Secret exclusion | OWASP Secrets Management / Logging guidance | Deployment archives must not contain generated stores, keyrings, certs, or tokens. | Keep package under `.tmp`, document contents, and do not commit archive/binaries. | docs only unless script needs correction | `rg`/package listing review. |

## P4G2 Phase 16 - Hetzner Backup Before Deploy

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Linux service rollback boundary | Microsoft Learn, Host ASP.NET Core on Linux with Nginx: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | The ASP.NET Core app payload and systemd unit can be backed up independently of Nginx/TLS/firewall. | Back up only `/opt/chessonline/server` and `/etc/systemd/system/chessonline.service`; do not touch nginx, UFW, 443, x-ui/Xray, Outline, Albatronix, or Unreal. | `docs/P4G2_HETZNER_BACKUP_RESULT.md` | SSH backup command, systemd status, loopback health/diagnostics before deploy. |
| Rollback readiness | Existing Hetzner deployment layout | A timestamped tarball under `/opt/chessonline/backups` is sufficient for the next server-only deploy rollback. | Record backup path and pre-deploy health in docs before copying the new package. | docs only | Confirm backup file exists and service remains active. |

## P4G2 Phase 17 - Hetzner Legal Preview Deploy

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Server-only deploy boundary | Microsoft Learn, Host ASP.NET Core on Linux with Nginx: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx | Restarting a Kestrel systemd service does not require changing Nginx, TLS, firewall, or other services. | Replace only `/opt/chessonline/server` with the prepared package and restart `chessonline.service`; leave 443/x-ui/Xray/Outline/Albatronix/Unreal untouched. | `docs/P4G2_HETZNER_LEGAL_PREVIEW_DEPLOY_RESULT.md` | Loopback health, public HTTP health, diagnostics capability flags, Classic/Asgard remote smoke. |
| Runtime data safety | OWASP secrets guidance and existing deployment layout | Runtime stores/keyrings must not be bundled into deploy archives or accidentally exposed in Git. | Inspect remote service configuration before deploy; preserve runtime data paths if they are outside the published payload. | docs only unless deployment layout requires a safe copy step | `systemctl cat`, server directory listing, no runtime artifacts in Git. |

## P4G2 Phase 18 - Remote Legal Preview Smoke

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR legal preview validation | Microsoft Learn, ASP.NET Core SignalR .NET client: https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | Hub method invocation should be verified through the same operator client path that users will use for diagnostics. | Run the smoke tool against public HTTP 80 for Asgard and Classic and require `action-source=server-preview`. | `docs/P4G2_REMOTE_LEGAL_PREVIEW_SMOKE_RESULT.md` | Remote smoke with `-BuildSmokeTool`, no fallback, snapshot/action log PASS. |
| Security/logging boundary | Microsoft Learn SignalR security and OWASP Logging guidance | Access tokens and temporary passwords must not be printed in operator smoke logs. | Use `-NoSecretLog`; commit only sanitized result docs, not raw `.tmp` logs. | docs only | Review stdout summaries for no tokens/passwords. |

## P4G2 Phase 19 - One-App UI Play Smoke

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF UI automation boundary | Microsoft Learn, WPF threading model and UI Automation guidance | Native WPF windows can be built and launched from the shell, but reliable click-path proof requires either operator interaction or explicit automation metadata. | Inspect `ChessOnlineApp` XAML/control names before claiming manual UI proof; if automation is not reliable, document a truthful operator smoke result/checklist. | `docs/P4G2_ONE_APP_UI_PLAY_RESULT.md` | Build app, inspect controls, run remote smoke as backend equivalent, and record whether UI click path was manually/automatically executed. |
| Dispatcher and SignalR callbacks | Microsoft Learn, SignalR .NET client and WPF Dispatcher docs | SignalR callbacks must update UI through the dispatcher; existing UI code should be verified by build and targeted smoke. | Keep Phase 19 focused on UI playability proof, not rule/server changes. | docs/app build only unless a small UI fix is needed | `dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64`. |

## P4G2 Phase 20 - Two-Window UI Play Smoke

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Two-window WPF client state | Microsoft Learn, WPF application model/data binding docs | Separate app instances keep separate in-memory auth/session state and can connect independently to SignalR. | Launch two `ChessOnlineApp` processes and drive manual matchmaking through public HTTP 80 with temporary users. | `docs/P4G2_TWO_WINDOW_UI_PLAY_RESULT.md` | UI Automation click path: register temp users, manual matchmaking, ready/start, snapshot, legal preview submit, peer action log. |
| Realtime/action-log proof | Microsoft Learn, ASP.NET Core SignalR .NET client | A second client should be able to request authoritative action log after the first client submits an accepted action. | Treat B-window action-log request after A-window legal-preview submit as the practical two-window smoke proof for this phase. | docs only | Public diagnostics counters and sanitized UIA log summary. |

## P4G2 Phase 21 - Five-Profile Online Coverage Matrix

Date: 2026-06-27

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Profile isolation | Existing rule-profile catalog and online smoke tooling | Scenario/playthrough JSON are not modes; online selector must continue to expose exactly five real profiles. | Record five-profile online coverage without adding profiles or pretending special actions are normal moves. | `docs/P4G2_FIVE_PROFILE_ONLINE_COVERAGE_MATRIX.md` | Remote smoke where safe; document untested/unsupported special-action boundaries honestly. |
| Special-action boundary | Microsoft Learn SignalR client and existing protocol DTOs | A legal-preview transport can list actions, but UI submit must respect action kind. | Use snapshot-only smoke for profiles whose special action UX is not yet fully operator-proven. | docs only unless smoke uncovers a targeted issue | Asgard/Classic full action smoke; Rubik/Hodge/Single startup/snapshot where supported. |

## P4G2 Phase 22 - Special Action Boundary Audit

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR action dispatch errors | Microsoft Learn, ASP.NET Core SignalR .NET client: https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client | Hub calls should surface invocation failures clearly; clients should not reinterpret unsupported commands as another action kind. | Treat unsupported special online actions as UI-disabled or explicit-reject cases, never as `NormalMove`. | `docs/P4G2_SPECIAL_ACTION_BOUNDARY_AUDIT.md` | Audit `OnlineActionKinds`, preview mapping, and `ChessOnlineApp` dispatch path. |
| WPF command boundary | Microsoft Learn, WPF Dispatcher and data binding docs | UI command state should reflect whether the action can be submitted safely from the current context. | Keep generic board click restricted to normal moves; use dedicated panels for Rubik layer turns, Hodge projections, and reserve restore. | docs now; `ChessOnlineApp` guardrails in Phase 23 | Build app and contract tests after guardrails. |
| Logging and secrets | OWASP Logging Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html | Logs should avoid secrets and should make security-relevant failures observable. | Log action-kind boundary failures without tokens/passwords and without raw auth headers. | docs now; UI status text in Phase 23 | `rg` secret audit later in Phase 29. |

## P4G2 Phase 23 - Special Action Guardrails

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Generic board submit safety | Existing `OnlineLegalPreviewState`, `OnlineActionKinds`, and Microsoft SignalR client guidance | A SignalR command should preserve its declared action kind; UI should reject unsupported kinds before invocation. | Add a shared dispatch policy that permits only `NormalMove` through generic board click submit. | `src/ChessOnlineClient/OnlinePreviewActionDispatchPolicy.cs`, `src/ChessOnlineApp/MainWindow.xaml.cs` | App build plus contract tests for Normal/Rubik/Hodge/Reserve/unknown action kinds. |
| User-facing error clarity | WPF status text pattern in `ChessOnlineApp` | Rejections should be visible in the move status and event log, not silently ignored. | Report dedicated panel requirements for Rubik layer turns, Hodge projections, and reserve restores. | `src/ChessOnlineApp/MainWindow.xaml.cs`, docs | Build, targeted online contract tests. |

## P4G2 Phase 24 - Rubik Layer Action UI Boundary

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Rubik action separation | Existing `OnlineActionKinds.RubikLayerTurn`, Rubik online fixtures, WPF control state guidance | Layer turns need axis/layer/quarter-turn context and should not be hidden behind source/target click-to-move. | Add a Rubik-only UI group with controls and disabled dispatch status until explicit online layer-turn submit is finalized. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs`, `src/ChessOnlineClient/OnlinePreviewActionDispatchPolicy.cs` | App build and contract tests for Rubik panel visibility policy. |
| Operator clarity | Existing P4G2 coverage matrix | Rubik match/snapshot works, but special action UX remains bounded. | Show a visible boundary for Rubik so the profile does not look broken or silently unsupported. | `docs/P4G2_RUBIK_LAYER_ACTION_UI.md` | Build, targeted online contract tests. |

## P4G2 Phase 25 - Hodge Projection UI Boundary

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Hodge action separation | Existing `OnlineActionKinds.HodgeProjectedMove`, Hodge online fixtures, and legal-preview DTOs | Hodge projected moves need primary plus mirror preview and all-or-nothing explanation. | Add a Hodge-only UI group with disabled dispatch status until explicit projection submit is finalized. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs`, `src/ChessOnlineClient/OnlinePreviewActionDispatchPolicy.cs` | App build and contract tests for Hodge panel visibility policy. |
| NormalMove protection | Existing Phase 23 dispatch policy | Projection composite actions must not be downgraded to generic normal moves. | Keep generic board submit blocked for `HodgeProjectedMove`; route future work to the Hodge panel. | `docs/P4G2_HODGE_PROJECTION_UI_BOUNDARY.md` | Contract tests for Hodge action rejection and panel visibility. |

## P4G2 Phase 26 - Actual Online Play User Guide

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Operator play instructions | Existing P4G2 remote/UI smoke docs and Microsoft SignalR client guidance | The user needs exact health, build, run, one-app, and two-window steps, not only smoke logs. | Add a player/operator guide that describes actual UI click paths and profile coverage. | `docs/P4G2_ACTUAL_ONLINE_PLAY_USER_GUIDE.md`, `README.md`, project status docs | Docs-only diff check and CI after commit. |
| HTTP security warning | Microsoft SignalR security docs and OWASP logging guidance | Public HTTP 80 is diagnostic-only; users must not enter real credentials. | Repeat temp-user-only and no-real-password guidance in the user guide and current status docs. | docs only | Phase 29 secret/log audit. |

## P4G2 Phase 27 - Online Playability UI Polish

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF status readability | Microsoft Learn WPF data binding and threading guidance | Operator-facing UI should update from existing UI event paths and avoid background-thread mutation. | Add a compact WPF status line fed by existing match, turn, realtime, preview, and action counter state. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp`; verify disconnected startup has no null-reference crash. |
| SignalR client observability | Microsoft Learn, ASP.NET Core SignalR .NET client guidance | Clients should expose connection and action state clearly, while hub calls remain explicit and bounded. | Surface connection state, action counts, server sequence, realtime resync state, and preview count in one line. | `docs/P4G2_PLAYABILITY_UI_POLISH.md` | App build and targeted smoke path through existing online controls. |
| Secret-free status text | Microsoft SignalR security guidance and OWASP Logging Cheat Sheet | UI/log status should avoid access tokens, refresh tokens, passwords, and raw authorization values. | Show only `anonymous` or `temp-user`; do not display credentials or token material. | `src/ChessOnlineApp/MainWindow.xaml.cs`, docs | Phase 29 secret/log audit plus code inspection. |

## P4G2 Phase 28 - Play Session Reports

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Bug report reproducibility | Microsoft SignalR client guidance and existing P4G2 replay/resync docs | Online issues need enough room/table/snapshot/seq context to reproduce without requiring raw tokens. | Expand local session reports with snapshot hash, legal-preview details, realtime counters, UI status strings, and action/event log tails. | `src/ChessOnlineApp/MainWindow.xaml.cs`, `docs/P4G2_PLAY_SESSION_REPORTS.md` | Build `ChessOnlineApp`; inspect report fields for no token/password values. |
| Clipboard-safe summaries | OWASP Logging Cheat Sheet | Logs and copied summaries should be useful but not include secrets or raw authorization material. | Add `Copy Sanitized Summary` for concise bug reports with shortened player ids and redacted token/password markers. | `src/ChessOnlineApp/MainWindow.xaml` | App build and Phase 29 secret/log audit. |

## P4G2 Phase 29 - Secret and Log Audit

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Secret scanning boundary | OWASP Logging Cheat Sheet and existing repo redaction tests | Secret-related words appear legitimately in DTO names, auth code, redaction tests, and warnings; raw runtime values must not be tracked. | Document scan results and classify expected references versus forbidden artifacts. | `docs/P4G2_SECRET_LOG_AUDIT.md` | `rg` scan plus `.gitignore` checks for `.tmp`, deployment, and production outputs. |
| Public HTTP endpoint | Microsoft SignalR security guidance | HTTP 80 with auth is diagnostic/dev-only and should use temporary users. | Keep the public IP in operator docs/scripts where already used, but preserve warnings and no-token logging. | docs only | Secret/log audit and future TLS/domain phase. |

## P4G2 Phase 30 - Actual Online Play Verification

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Local verification gate | GitHub Actions docs and existing decomposed runner docs | Local tests should mirror CI enough to catch build/test/package regressions without requiring remote Hetzner smoke in CI. | Run `git diff --check`, test list, full `run-tests -SkipBenchmark`, and `scripts/verify.ps1`. | `docs/P4G2_FINAL_MANUAL_TEST_RESULT.md` | Sequential local commands with controlled MSBuild parallelism. |
| Remote operator smoke | Microsoft SignalR .NET client guidance | Remote smoke should remain operator-driven and use temporary users over diagnostic HTTP 80. | Re-run Asgard and Classic full action smoke, plus snapshot-only coverage for Single/Rubik/Hodge. | docs only | Smoke tool with `-NoSecretLog`; do not commit raw `.tmp` logs. |

## P4G2 Phase 31 - Final Actual Online Play Report

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Final operator handoff | Existing P4G2 docs, GitHub Actions results, and Microsoft SignalR guidance | The final report should distinguish playable paths from bounded special-action work and avoid claiming production readiness. | Summarize commits, CI, local verify, remote smokes, UI click paths, security boundaries, and next phases. | `docs/P4G2_ACTUAL_ONLINE_PLAY_FINAL_REPORT.md` | `git diff --check`, `run-tests -List`, and CI after docs commit. |
| Production boundary | Microsoft SignalR security guidance | HTTP 80 with temporary users is useful for diagnostics but not production account traffic. | Keep TLS/domain/443 deferred and explicitly note that x-ui/Xray/Outline/Albatronix/Unreal were untouched. | final report docs | Secret/log audit plus final status check. |

## P4I Phase 00 - Visual Online Board Path Audit

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF incremental UI polish | Microsoft Learn WPF controls/data binding guidance | A simple existing `UniformGrid` can be improved safely without embedding the full local Chess3D rendering stack. | Keep the compact snapshot grid as a reliable fallback and add readability polish incrementally. | `docs/P4I_VISUAL_ONLINE_BOARD_AUDIT.md` | Docs-only audit, then app build when code changes begin. |
| Online authority boundary | Existing P4G2 snapshot/legal-preview docs | The online board must render authoritative server snapshots, not local engine guesses. | Use `OnlineChess3DBoardSnapshotParser`, legal-preview targets, and action log as the visual source of truth. | docs only | Future Phase 33/34 app build and contract tests. |

## P4I Phase 01 - Online Board Readability

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF grid readability | Microsoft Learn WPF controls and layout docs | Small button grids need stable dimensions and concise labels to remain usable. | Add coordinate headers, marker labels, and a board legend while keeping the existing `UniformGrid` fallback. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp`; run online contract tests. |
| Server-snapshot authority | Existing online snapshot/legal-preview contracts | Visual markers should reflect authoritative snapshot and preview state only. | Render selected/from/to/legal/capture/special markers from local UI state plus server legal preview; no speculative local engine moves. | `docs/P4I_BOARD_READABILITY.md` | App build and contract tests; remote smoke unchanged. |

## P4I Phase 02 - Online Action History UI

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Action history usability | WPF list/control guidance and existing online action-log DTOs | A visible action log is more useful if selected notation can be copied/exported without raw session data. | Add selected-action status, copy selected notation, and sanitized action-log export. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | App build and online contract tests. |
| Secret-free export | OWASP Logging Cheat Sheet | Exported bug artifacts should avoid tokens, passwords, and auth headers. | Export only ruleset, room/table, short player ids, snapshot hash, server seq, counters, and action strings. | `docs/P4I_ONLINE_ACTION_HISTORY_UI.md` | Secret/log scan remains covered by P4G2 audit; export path under `.tmp`. |

## P4I Phase 03 - Playability Micro Polish

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Reduce manual online steps | Existing P4G2 UI click path and Microsoft SignalR client guidance | Accepted actions should refresh authoritative state and visible action history without extra manual clicks. | Reuse the action-log request path and auto-refresh the log after accepted actions while keeping manual buttons. | `src/ChessOnlineApp/MainWindow.xaml.cs` | App build and online contract tests. |
| Operator fallback | Existing P4G2 user guide | Automation should not remove manual fallback buttons because remote diagnostics can need explicit refresh. | Keep `Request Snapshot` and `Request Action Log`; add auto-refresh as convenience only. | `docs/P4I_PLAYABILITY_MICRO_POLISH.md` | Build/test and docs. |

## P4I Phase 04 - Online Board Layer Navigation

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| 3D board slice usability | Existing P4I grid-board docs and WPF controls guidance | A single visible Z slice can hide legal targets on other layers, making a valid preview feel empty. | Add a per-layer occupied/legal/capture/special summary and quick buttons for layers that contain legal targets. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp`; run online contract tests. |
| Authoritative preview boundary | Existing `LegalPreviewState` and server legal-preview contract | Layer navigation should not generate moves locally; it should only summarize server-provided preview targets. | Compute layer counts from `OnlineChess3DBoardSnapshot` plus `LegalPreviewState.Targets`, and auto-focus only after server preview exists. | `docs/P4I_ONLINE_LAYER_NAVIGATION.md` | App build and targeted tests; no remote server change. |

## P4I Phase 05 - Online Action History Board Highlight

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Action log readability | Existing action-log UI and WPF selection event guidance | Selecting a notation row is more useful if the board shows the move's from/to cells. | Parse coordinate pairs from selected action notation and highlight them as read-only history markers. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp`; run online contract tests. |
| Submit safety | Existing generic dispatch guardrails | History selection must not mutate the current action command fields or turn into a replay/submit action. | Keep history markers separate from `_p4gMoveFrom`/`_p4gMoveTo`; selecting history does not submit anything. | `docs/P4I_ACTION_HISTORY_BOARD_HIGHLIGHT.md` | App build and targeted tests; no server change. |

## P4J Phase 00 - Online Match UX Baseline

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Current playable online state | Existing P4G2/P4I reports and GitHub Actions history | Classic and Asgard already pass server-preview action smoke; P4I improved the board but match lifecycle UX is still diagnostic-heavy. | Record a clean baseline before adding reconnect/resume/spectator/lobby work. | `docs/P4J_ONLINE_MATCH_UX_BASELINE.md` | Git status, latest CI, Hetzner health, Classic/Asgard remote smoke, `ChessOnlineApp` build. |
| HTTP diagnostic boundary | Existing Hetzner docs and SignalR security guidance | Public HTTP 80 is acceptable only for temporary diagnostic users; TLS/domain/443 remain deferred. | Keep remote smoke operator-driven and do not touch nginx/UFW/x-ui/443 in P4J Phase 00. | docs only | Curl health/ready/diagnostics plus smoke tool with `-NoSecretLog`. |

## P4J Phase 01 - SignalR Reconnect Path Audit

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR reconnect lifecycle | Microsoft Learn, ASP.NET Core SignalR .NET client | ASP.NET Core SignalR automatic reconnect is opt-in through `WithAutomaticReconnect`, and clients can observe reconnecting/reconnected/closed events. | Document that the shared relay client already opts in, but does not yet expose a testable reconnect state model to UI. | `docs/P4J_SIGNALR_RECONNECT_AUDIT.md` | Docs-only audit plus code search. |
| WPF callback threading | Microsoft Learn, WPF threading model / Dispatcher | SignalR callbacks must marshal UI updates to the WPF dispatcher, and dispatcher work should stay small. | Keep UI updates in dispatcher callbacks, but future phases should surface compact reconnect events rather than doing heavy work in callbacks. | docs only | Code audit of `P4FRelayMessageReceived` and P3F callbacks. |
| SignalR token safety | Microsoft Learn, SignalR security | Access tokens may appear in transport URLs/logs in some SignalR transports; logs should avoid token values and HTTP remains diagnostic-only. | Reconnect UI/errors must use safe messages and never print access/refresh tokens. | docs only | Future reconnect model tests should include redaction. |

## P4J Phase 02 - Client Reconnect State Model

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Testable reconnect state | Microsoft Learn SignalR .NET client lifecycle docs and Phase 01 audit | SignalR lifecycle events should be represented by a client state model before being wired into WPF. | Add `OnlineReconnectState`, `OnlineConnectionState`, `OnlineReconnectEvent`, summaries, and health snapshots in `ChessOnlineClient`. | `src/ChessOnlineClient/OnlineReconnectState.cs` | Build client and run `ChessOnlineContractTests`. |
| Safe reconnect errors | Microsoft Learn SignalR security and OWASP Logging guidance | Reconnect errors can include token-like strings; UI summaries should show redacted messages only. | Reuse `ChessOnlineSecretRedactor` in reconnect state and test redaction. | `tests/ChessOnlineContractTests/Program.cs` | Contract tests for token/password redaction in state summaries. |

## P4J Phase 03 - SignalR Automatic Reconnect Wiring

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR lifecycle events | Microsoft Learn SignalR .NET client docs | `HubConnection` exposes `Reconnecting`, `Reconnected`, and `Closed`; callbacks should be lightweight. | Wire lifecycle callbacks inside `ChessOnlineRelayClient` and publish compact `OnlineReconnectSummary` events. | `src/ChessOnlineClient/ChessOnlineRelayClient.cs` | Build client/app and run online contract tests. |
| UI resync boundary | Phase 01 audit and current P4G realtime resync code | Low-level client does not know room/table context for snapshot/action-log requests. | Mark post-reconnect snapshot/action-log flags in the shared client, but leave actual refresh to the UI layer in Phase 04. | `docs/P4J_SIGNALR_AUTORECONNECT.md` | Contract tests ensure client can be constructed without network. |

## P4J Phase 04 - Reconnect UI Guards

Date: 2026-06-28

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF dispatcher safety | Microsoft Learn WPF threading model / Dispatcher | SignalR callbacks do not run as WPF UI events; UI mutations must be marshalled back to the Dispatcher. | Handle `ReconnectStateChanged` in `ChessOnlineApp` through `Dispatcher.InvokeAsync` and keep the handler limited to status/resync refresh. | `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp`; run online contract tests. |
| Guarded online actions | Microsoft Learn SignalR .NET client reconnect lifecycle and Phase 03 client state | During reconnecting/closed/disconnected states, UI buttons can still be clickable unless explicitly guarded. | Add a visible reconnect status line and a shared `CanUseP4FPrimaryRelay`/`EnsureP4FPrimaryRelayUsable` guard for ready/start/snapshot/action-log/legal-preview/action submit paths. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineClient` and `ChessOnlineApp`; targeted `ChessOnlineContractTests`. |
| Post-reconnect resync | Existing P4G realtime resync code | After SignalR reconnected, the safe recovery operation is to refresh server snapshot and action log from authoritative state. | When `OnlineReconnectSummary` requests resync, `ChessOnlineApp` calls the existing snapshot/action-log refresh path and clears the resync request flag. | `docs/P4J_RECONNECT_UI.md` | Manual UI smoke later; no remote smoke required for Phase 04. |

## P4J Phase 05 - Manual Reconnect Smoke

Date: 2026-06-29

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Operator reconnect smoke | Microsoft Learn SignalR .NET client lifecycle docs and existing P4F/P4G UI flow | A useful reconnect smoke must disconnect the client relay only; restarting Kestrel/nginx would test deployment operations, not player reconnect UX. | Add small operator controls to disconnect/reconnect the primary `ChessOnlineRelayClient` while retaining room/table/session context. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp`; run targeted online contract tests; manual click-through remains operator-run. |
| Authoritative recovery after reconnect | Existing P4G realtime resync and action-log code | Reconnect should rebuild the visible board from server snapshot/action log, not trust stale local preview state. | Manual reconnect clears legal preview, reconnects SignalR, then requests authoritative snapshot and action log. | `docs/P4J_RECONNECT_MANUAL_SMOKE_RESULT.md` | UI smoke criteria documented; no server/network-stack changes. |
| Security boundary | Microsoft SignalR security guidance and OWASP Logging Cheat Sheet | Reconnect diagnostics should not print access/refresh tokens or temporary passwords. | Keep status text to connection state, short context, and sanitized logs; do not commit `.tmp` smoke reports. | docs/code only | Existing redaction tests plus no raw smoke logs committed. |

## P4J Phase 06 - Resume Contract Audit

Date: 2026-06-29

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Match resume boundary | Existing persistence code and Microsoft SignalR reconnect guidance | SignalR reconnect restores transport, but match resume after client/server restart needs explicit room/table/seat validation and snapshot/action-log response. | Treat reconnect and resume as separate features; Phase 06 documents the minimum append-only resume contract before adding DTOs. | `docs/P4J_RESUME_CONTRACT_AUDIT.md` | Docs-only audit plus code search. |
| Persistence reality | Existing `JsonOnlineStore`, `PlayerSessionEntity`, and `Chess3DRelayHub` persistence paths | Tables, seats, action logs, session last-seen, and snapshot savegame are persisted, but `OnlineRoomRegistry` is not yet rehydrated on startup despite the `RestoreRoomsOnStartup` option. | First resume method should support active in-memory matches; persisted-after-restart resume must return a clear deferred reason until registry/session rehydration exists. | docs only | No code change in Phase 06. |
| Token safety | OWASP Logging Cheat Sheet and current client redaction helpers | Resume context must not store access/refresh tokens or temporary passwords in tracked output. | Store only non-secret context such as room/table/ruleset/seat/last hash/seq in future client code. | docs only | Future DTO tests should assert no token fields. |

## P4J Phase 07 - Resume DTOs And Capability Flag

Date: 2026-06-29

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Append-only protocol evolution | Existing `OnlineProtocolMessage` JSON envelope and GitHub Actions compatibility checks | New online features can be introduced safely by adding optional payload properties and message constants without removing old fields. | Add `OnlineResumeRequest`, `OnlineResumeResult`, and `OnlineResumeCandidate` as optional DTO payloads. | `src/ChessOnlineProtocol/OnlineProtocolDtos.cs`, `src/ChessOnlineProtocol/OnlineProtocolJson.cs` | Protocol roundtrip tests and `ChessOnlineContractTests`. |
| Honest capability reporting | Phase 06 audit and existing diagnostics flags | Advertising resume before the hub method exists would mislead clients. | Add `ResumeMatchSupported`, expose it as `/chess3d/diagnostics.resumeMatch`, and keep it `false` until Phase 08 implements `RequestResumeMatch`. | `src/ChessOnlineProtocol/OnlineRoomRegistry.cs`, `src/ChessOnlineServer/ChessOnlineServerHost.cs` | Diagnostics tests assert false and no hub method listing yet. |
| No-token resume context | OWASP Logging guidance and existing redaction tests | Resume DTOs should carry room/table/seat/hash/seq, not credentials. | Do not add token/password fields to resume DTOs; tests check serialized DTOs for token field names. | `tests/ChessOnlineContractTests/Program.cs` | Targeted online contract tests. |

## P4J Phase 08 - Server Resume Match Method

Date: 2026-06-29

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Active match resume | Existing `OnlineRoomRegistry` authority and Microsoft SignalR group guidance | A reconnecting player must be re-added to room/table groups and receive authoritative snapshot/action log, but board state must not mutate. | Add `RequestResumeMatch` for active in-memory tables, returning snapshot and action-log tail. | `src/ChessOnlineProtocol/OnlineRoomRegistry.cs`, `src/ChessOnlineServer/Chess3DRelayHub.cs` | Registry tests for success, wrong player, wrong table, no mutation. |
| Runtime rehydration boundary | Phase 06 persistence audit | Persisted tables are not yet rehydrated into active native sessions after server restart. | Keep server-restart resume deferred; the active resume method returns clear failure for missing/non-active runtime tables. | `docs/P4J_SERVER_RESUME_MATCH.md` | Documentation and tests avoid claiming restart resume. |
| Capability flip | Existing diagnostics endpoint | Once the hub method exists, clients need a machine-readable capability flag. | Set `ResumeMatchSupported=true`, list `RequestResumeMatch`, and expose `/chess3d/diagnostics.resumeMatch=true`. | `src/ChessOnlineServer/ChessOnlineServerHost.cs`, tests | Build server and targeted online contract tests. |

## P4J Phase 09 - Client Resume Support

Date: 2026-06-29

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR resume client method | Microsoft Learn, ASP.NET Core SignalR .NET client | Hub methods should be invoked through the shared `HubConnection` client and treated as server-authoritative responses. | Add `RequestResumeMatchAsync` to `ChessOnlineRelayClient` and remember the last resume result for UI/session reporting. | `src/ChessOnlineClient/ChessOnlineRelayClient.cs` | Build `ChessOnlineClient`; targeted online contract tests. |
| UI thread and state updates | Microsoft Learn, WPF threading model / Dispatcher | WPF UI state must be updated from UI event handlers or marshalled through the dispatcher. | Keep resume as a button-driven UI action that reuses existing snapshot/action-log render helpers. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp`. |
| Resume context security | OWASP Logging Cheat Sheet and Phase 06 resume audit | Resume context should be non-secret: room/table/seat/hash/seq are useful, tokens/passwords are not. | Store resume context in memory and sanitized session reports only; do not persist credentials. | `src/ChessOnlineApp/MainWindow.xaml.cs`, `docs/P4J_CLIENT_RESUME.md` | Contract tests for callback registration and no-token DTOs; diff audit. |

## P4J Phase 09R - Resume Baseline Check

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Baseline commit and CI | GitHub Actions run list and local git history | `origin/main` is at `34e8101f9 P4J phase 09: add client resume support`, and the latest Windows Build succeeded. | Continue from Phase 09 without reset or rescue work. | `docs/P4J_PHASE09_BASELINE_CHECK.md` | `git rev-parse`, `gh run list`, `git diff --check`. |
| Remote capability drift | Hetzner `/chess3d/diagnostics` and Microsoft SignalR client guidance | The deployed HTTP 80 server is healthy and supports legal preview, but does not yet advertise `resumeMatch` or `RequestResumeMatch`. | Record the deployment gap honestly before resume manual smoke; local client/server code is ahead of deployed Hetzner package. | `docs/P4J_PHASE09_BASELINE_CHECK.md` | Curl health/ready/diagnostics and local client/app builds. |
| Token-safe diagnostics | Microsoft SignalR security guidance and OWASP Logging Cheat Sheet | SignalR/auth diagnostics can leak tokens if raw URLs or bearer values are logged. | Baseline doc records only capability booleans and sanitized status, not tokens/passwords. | docs only | Review generated docs before commit. |

## P4J Phase 10 - Resume Manual Smoke

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Resume smoke prerequisite | Microsoft Learn, ASP.NET Core SignalR .NET client and local Phase 08/09 code | A client can only invoke a hub method that the deployed server exposes; current Hetzner diagnostics omit `RequestResumeMatch`. | Do not claim remote resume PASS until the server package containing Phase 08 is deployed. Record a blocked smoke result instead. | `docs/P4J_RESUME_MANUAL_SMOKE_RESULT.md` | Build `ChessOnlineApp`, curl diagnostics, `git diff --check`. |
| WPF state recovery | Microsoft Learn, WPF Dispatcher/threading guidance | The `Resume Current Match` button runs on the UI event path and should render authoritative snapshot/action-log only after server response. | Keep the manual checklist focused on active match, disconnect/reconnect, then resume; no background UI automation committed. | docs only | Manual operator checklist in result doc. |
| Safe reporting | OWASP Logging Cheat Sheet and Microsoft SignalR security | Manual smoke notes must not include temporary passwords, bearer tokens, or raw logs. | Store only sanitized status and capability facts in docs; `.tmp/manual-smoke` remains untracked. | docs only | Inspect docs before commit. |

## P4J Phase 11 - Spectator Contract Audit

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR table groups | Microsoft Learn, ASP.NET Core SignalR groups | SignalR groups are the right lightweight boundary for broadcasting table events to all interested connections. | Spectators should join the existing table group, but without allocating a player seat. | `docs/P4J_SPECTATOR_CONTRACT_AUDIT.md` | Docs-only audit and code search. |
| Authenticated spectators | Microsoft Learn, SignalR authentication/authorization | Authenticated hub connections provide a player/user identity without exposing tokens to hub method payloads. | Require temporary authenticated users for spectator mode in P4J; anonymous public spectating is deferred. | docs only | Later DTO/server tests. |
| Read-only privacy boundary | OWASP Logging Cheat Sheet | Lobby/spectator output must not expose tokens, connection ids, passwords, or full private player data. | Spectator may receive snapshot/action-log table state, but cannot call mutating methods or receive secrets. | docs only | Future secret/privacy audit. |

## P4J Phase 12 - Spectator DTOs

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Append-only protocol evolution | Existing online protocol JSON envelope and SignalR hub method patterns | Optional payload properties allow new client/server capabilities without breaking older messages. | Add `JoinSpectator`/`JoinSpectatorResult` DTOs and message constants append-only. | `src/ChessOnlineProtocol/OnlineProtocolDtos.cs`, `src/ChessOnlineProtocol/OnlineProtocolJson.cs` | Protocol roundtrip tests. |
| Honest capability reporting | Existing diagnostics endpoint and GitHub Actions compatibility checks | Advertising spectator mode before server hub implementation would mislead clients. | Add `SpectatorModeSupported`, but keep it false and do not list `JoinSpectator` until Phase 13. | `src/ChessOnlineServer/ChessOnlineServerHost.cs`, tests | Diagnostics tests assert false/no method. |
| Secret-free spectator payload | OWASP Logging Cheat Sheet | Read-only viewer state should carry room/table/ruleset/seq, not credentials. | Spectator DTOs contain no token/password/Authorization fields. | tests/docs | Contract tests check serialized payload text. |

## P4J Phase 13 - Server Spectator Mode

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR table groups | Microsoft Learn, ASP.NET Core SignalR groups | Adding a connection to an existing group is sufficient for broadcast delivery without changing game authority. | `JoinSpectator` adds the connection to room/table groups but does not allocate an `OnlineSeat`. | `src/ChessOnlineServer/Chess3DRelayHub.cs`, `src/ChessOnlineProtocol/OnlineRoomRegistry.cs` | Registry tests plus server build. |
| Read-only server authority | Existing `SubmitAction`, `Ready`, and `StartGame` seat checks | Mutating methods already require `TrySeat`; a spectator with no seat is rejected by the existing server authority. | Keep spectator read-only by construction; do not add UI-only security assumptions. | `tests/ChessOnlineContractTests/Program.cs` | Tests assert spectator submit is rejected and snapshot/action-log requests work. |
| Capability flip | Existing `/chess3d/diagnostics` feature flags | Once the hub method exists, diagnostics should advertise it for clients and operators. | Set `SpectatorModeSupported=true` and list `JoinSpectator`. | diagnostics/tests/docs | Targeted online contract tests. |

## P4J Phase 14 - Client Spectator Support

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Shared client boundary | Microsoft Learn, ASP.NET Core SignalR .NET client | WPF should call a reusable client method instead of constructing raw hub messages in code-behind. | Add `JoinSpectatorAsync` to `ChessOnlineRelayClient`. | `src/ChessOnlineClient/ChessOnlineRelayClient.cs` | Build `ChessOnlineClient`; targeted contract tests. |
| Read-only client state | Phase 11 spectator contract and WPF UI safety guidance | UI needs a compact state to disable submit and display the spectator context. | Add `OnlineSpectatorClientState` with room/table/ruleset/id/seq and submit-disabled reason. | `src/ChessOnlineClient/OnlineSpectatorClientState.cs` | State tests without network. |
| Secret-safe event handling | OWASP Logging Cheat Sheet | Spectator callbacks should store only sanitized table state, not auth secrets. | Remember `LastSpectatorResult` and register `ReceiveJoinSpectatorResult`; DTO tests already cover no token fields. | client/tests | Contract tests check callback registration and initial state. |

## P4J Phase 15 - Spectator UI

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF read-only interaction | Microsoft Learn, WPF controls and event handling | Disabling mutating buttons in the UI is a clarity layer, not the security boundary; server-side seat checks remain authoritative. | Add a Spectator play mode and route all submit checks through the existing `CanP4FPrimaryAct` guard. | `src/ChessOnlineApp/MainWindow.xaml`, `src/ChessOnlineApp/MainWindow.xaml.cs` | Build `ChessOnlineApp`; targeted online contract tests. |
| SignalR spectator client flow | Microsoft Learn, ASP.NET Core SignalR .NET client | The client can invoke `JoinSpectator` once connected and authenticated, then request snapshot/action-log over the same hub. | Add Join/Snapshot/ActionLog/Follow/Report spectator controls using `ChessOnlineRelayClient.JoinSpectatorAsync`. | WPF code-behind and docs | App build and manual smoke checklist. |
| Secret-free session reports | OWASP Logging Cheat Sheet | UI reports may include room/table IDs and state hashes, but must not include tokens, passwords, or Authorization headers. | Save spectator reports under `.tmp/manual-smoke` with explicit redaction flags and short spectator IDs only. | `docs/P4J_SPECTATOR_UI.md` | `git diff --check`; inspect report schema. |

## P4J Phase 16 - Spectator Manual Smoke

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Deployed capability check | Current Hetzner `/chess3d/diagnostics` | The deployed server reports `requestLegalPreview=true`, but does not list `JoinSpectator` and has no `spectatorMode` field. | Do not fake a remote spectator PASS; record the blocker until the Phase 13+ server package is deployed. | `docs/P4J_SPECTATOR_MANUAL_SMOKE_RESULT.md` | Curl diagnostics and local app build. |
| Manual smoke honesty | GitHub Actions and operator-smoke boundary | Remote smoke is not a CI gate and should only be claimed when the deployed server exposes the required hub method. | Phase 16 documents local UI readiness and remote deployment gap separately. | docs only | `dotnet build ChessOnlineApp`; `git diff --check`. |
| Safe reporting | OWASP Logging Cheat Sheet | Manual smoke docs should contain capability facts and no temp credentials or bearer tokens. | Include sanitized endpoint/capability summary only. | docs only | Inspect generated docs before commit. |

## P4J Phase 17 - Lobby Contract Audit

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Minimal lobby surface | Microsoft Learn, ASP.NET Core SignalR groups and current `OnlineRoomRegistry` | The registry already owns active rooms/tables/seats, but diagnostics only exposes aggregate counts. | Add a future safe lobby snapshot instead of overloading diagnostics text or exposing raw registry objects. | `docs/P4J_LOBBY_CONTRACT_AUDIT.md` | Docs-only audit and code search. |
| Privacy boundary | OWASP Logging Cheat Sheet and SignalR security guidance | Lobby rows are public-ish operational state and must not expose tokens, connection IDs, passwords, or full player IDs. | Lobby rows may include counts and short/anonymous seat summaries only. | docs only | Later DTO tests should scan serialized lobby payloads. |
| Spectator integration | Phase 11-16 spectator design | Lobby is the natural way to choose a table to spectate, but remote spectator still requires deployed `JoinSpectator`. | Phase 18 should include spectatorCount when available and let UI spectate selected tables once server package is aligned. | docs only | Future lobby DTO/server tests. |

## P4J Phase 18 - Server Lobby Snapshot

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Append-only lobby protocol | Existing `OnlineProtocolMessage` optional payload pattern | New request/result DTOs can be added without breaking old clients. | Add `RequestLobbySnapshot` and `LobbySnapshot` with optional payloads. | `src/ChessOnlineProtocol/OnlineProtocolDtos.cs`, `OnlineProtocolJson.cs` | JSON roundtrip tests. |
| Registry-owned active tables | Current `OnlineRoomRegistry` state model | Active room/table/seat state already exists in memory and can be projected into safe rows. | Add a dedicated snapshot builder instead of exposing raw room/table objects. | `src/ChessOnlineProtocol/OnlineRoomRegistry.cs` | Empty and active lobby contract tests. |
| Privacy and capability reporting | OWASP Logging Cheat Sheet and current diagnostics pattern | Lobby must advertise capability while omitting tokens, connection IDs, and full private player data. | Add `LobbySnapshotSupported` and `RequestLobbySnapshot` in supported methods. Seat labels are shortened. | server/protocol/tests/docs | Targeted online contract tests and server build. |

## P4J Phase 19 - Client Lobby Support

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR client method parity | Microsoft Learn, ASP.NET Core SignalR .NET client | Client SDK should expose the same hub methods UI needs, instead of building raw messages in WPF. | Add `RequestLobbySnapshotAsync` and remember `LastLobbySnapshot`. | `src/ChessOnlineClient/ChessOnlineRelayClient.cs` | Build `ChessOnlineClient`; targeted contract tests. |
| UI-friendly lobby rows | WPF binding guidance | WPF should bind/display compact rows, not raw protocol DTOs with nested seat data. | Add `OnlineLobbyTableDisplayRow` and `OnlineLobbyFilterState`. | `src/ChessOnlineClient/OnlineLobbyClientState.cs` | Unit-style contract tests without network. |
| Secret-safe display | OWASP Logging Cheat Sheet | Lobby display labels should not contain tokens or raw connection ids. | Use protocol-provided short player labels and no auth data in display rows. | client/tests/docs | Redaction/display tests. |

## P4J Phase 20 - Lobby UI

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF list display | Microsoft Learn, WPF data binding and controls | A compact list/detail view is safer than a large redesign and can use display rows from the client SDK. | Add Refresh Lobby, ruleset filter, active table list and selected details in `ChessOnlineApp`. | `src/ChessOnlineApp/MainWindow.xaml`, `.xaml.cs` | Build `ChessOnlineApp`. |
| Lobby to spectator flow | Phase 15 spectator UI and Phase 19 client lobby SDK | Lobby rows provide room/table IDs; spectator UI can reuse those IDs. | Add copy/spectate selected table actions and keep submit read-only in spectator mode. | WPF code-behind | App build and targeted online contract tests. |
| Safe lobby status | OWASP Logging Cheat Sheet | Active table UI must not display tokens, full connection IDs, or passwords. | Use `OnlineLobbyTableDisplayRow.DisplayLabel` and short seat summaries only. | docs/tests/UI | Inspect docs and run targeted tests. |

## P4J Phase 21 - Lobby Manual Smoke

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR client invocation | Microsoft Learn, ASP.NET Core SignalR .NET client | A client can only invoke hub methods that the deployed hub exposes; missing methods must be treated as a deployment/version gap, not a UI failure. | Check `/chess3d/diagnostics` for `RequestLobbySnapshot` before claiming remote lobby smoke PASS. | `docs/P4J_LOBBY_MANUAL_SMOKE_RESULT.md` | Curl diagnostics plus local app build. |
| SignalR security | Microsoft Learn, SignalR security considerations | Connection/runtime secrets must not be exposed in logs or manual reports. | Lobby smoke docs include only capability flags, counts, room/table flow, and no tokens or passwords. | docs only | Inspect docs before commit. |
| Operator smoke logging | OWASP Logging Cheat Sheet | Logs and reports should be useful for diagnosis without storing sensitive values. | Record the remote blocker and click path, but keep raw `.tmp/manual-smoke` reports untracked. | docs only | `git diff --check`; `run-tests -List`. |

## P4J Phase 22 - Network Bug Reports

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR troubleshooting context | Microsoft Learn, ASP.NET Core SignalR .NET client | Network bugs need connection state, last server sequence, reconnect/resync state, and the hub feature surface to be reproducible. | Add a dedicated network bug report that includes reconnect, resume, spectator, lobby, legal preview, counters, capabilities and action-log tail. | `src/ChessOnlineApp/MainWindow.xaml`, `.xaml.cs` | Build `ChessOnlineApp`; targeted online contract tests. |
| Secret-safe diagnostics | Microsoft Learn, SignalR security considerations | Access tokens and bearer headers must never be logged or copied into operator reports. | Keep tokens in memory only and add redaction for token/password/Authorization/private-key-like log lines. | UI report code and docs | Inspect report builder and run `git diff --check`. |
| Bug-report logging boundary | OWASP Logging Cheat Sheet | Logs should be sufficient for diagnosis while excluding secrets and high-risk raw runtime artifacts. | Save reports only under `.tmp/manual-smoke` and include explicit redaction/security flags. | docs/UI | Confirm `.tmp` remains untracked and report docs warn not to commit raw reports. |

## P4J Phase 23 - Secret and Privacy Audit

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Sensitive log scanning | OWASP Logging Cheat Sheet | Access tokens, refresh tokens, passwords, private keys, and Authorization headers require explicit exclusion/redaction. | Run repo-wide targeted `rg` audit and document expected code identifiers versus disallowed literal secrets. | `docs/P4J_SECRET_LOG_AUDIT.md` | Search output review and `git diff --check`. |
| SignalR auth privacy | Microsoft Learn, SignalR security considerations | Bearer tokens may be transported by the SignalR client but must not be printed in client logs or bug reports. | Keep token-bearing values inside client session objects and report only redacted status/short player IDs. | UI/client docs | Inspect report builders and smoke docs. |
| Local artifact boundary | GitHub Actions docs and existing `.gitignore` | Local `.tmp` reports should never become CI artifacts or tracked content. | Confirm `.tmp/`, manual smoke reports, logs, stores, and keyrings are ignored/documented. | `.gitignore`, docs | `git status --short` after local checks. |

## P4J Phase 24 - Full Local Verify

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Bounded local gate | Existing P4D1 test-runner decomposition | Full local verification should use the decomposed runner with controlled MSBuild parallelism and watchdog timeouts. | Run `run-tests -SkipBenchmark -MSBuildMaxCpuCount 1` before `scripts/verify.ps1`. | docs only | Record exact command outcomes in Phase 24 result doc. |
| Remote smoke boundary | GitHub Actions docs and project policy | Remote Hetzner smoke remains manual/operator-only and must not become a required CI step. | Local verify remains self-contained; remote capability blockers are documented separately. | docs only | Check local commands only. |
| Secret-safe verification output | OWASP Logging Cheat Sheet | Local logs under `.tmp` are ignored and should not be committed. | Record summaries, not raw logs. | docs only | `git status --short` before commit. |

## P4J Phase 25 - Final Report and User Guide

Date: 2026-07-01

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Operator play guide | Microsoft Learn, ASP.NET Core SignalR .NET client | Users need a concrete click path and capability checks, not only protocol docs. | Add a P4J guide for health, diagnostics, resume, spectator, lobby, network reports, and current remote blockers. | `docs/P4J_ONLINE_MATCH_UX_USER_GUIDE.md` | Docs review and `git diff --check`. |
| Deployment boundary | Microsoft Learn, host ASP.NET Core on Linux with Nginx; project Hetzner docs | Current public deployment is HTTP 80 behind nginx and should not be changed in P4J finalization. | State that no server/network change was made; lobby/spectator remote PASS requires later server package deployment. | final report docs | Curl health/diagnostics only. |
| Final status summary | GitHub Actions docs and project CI runs | CI success should reference exact run ids and local verify commands. | Record phase commits, latest CI, local verify, and remaining work. | `docs/P4J_ONLINE_MATCH_UX_FINAL_REPORT.md`, project docs | `git diff --check`; final CI after commit. |

## P4K Phase 00 - Deployment Baseline

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Public health baseline | Microsoft Learn, ASP.NET Core health checks | Health endpoints are suitable for read-only liveness/readiness probes before deploy actions. | Use `/healthz/live`, `/healthz/ready`, and `/chess3d/diagnostics` to record the pre-deploy gap. | `docs/P4K_DEPLOYMENT_BASELINE.md` | Curl public endpoints and record capability flags. |
| SignalR deployment gap | Microsoft Learn, ASP.NET Core SignalR .NET client and SignalR security | Clients must only claim support for deployed hub methods; missing methods are a version/deployment gap, not a UI success. | Record missing `RequestResumeMatch`, `JoinSpectator`, and `RequestLobbySnapshot` on current public Hetzner package. | docs only | Diagnostics `supportedHubMethods` review. |
| Linux/nginx boundary | Microsoft Learn, Host ASP.NET Core on Linux with Nginx | Kestrel behind nginx can be inventoried without changing nginx/firewall/ports. | Perform read-only service/port/file metadata inventory and avoid runtime store/keyring contents. | docs only | SSH `systemctl`, `ss`, and `stat` metadata only. |

## P4K Phase 01 - Package Runtime Boundary

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Framework-dependent Linux publish | Microsoft Learn, .NET application publishing overview and `dotnet publish` command | `dotnet publish -r linux-x64 --self-contained false` is the expected package shape when the server already has the .NET runtime. | Keep P4K packages framework-dependent and include the tested `libChess3DEngine.so` explicitly. | `docs/P4K_PACKAGE_RUNTIME_BOUNDARY.md` | Build `ChessOnlineServer`, inspect package script, and later publish into untracked output only. |
| Data Protection key ring | Microsoft Learn, Configure ASP.NET Core Data Protection and Key storage providers | Data Protection keys are mutable runtime state and should live outside immutable app package files. | Keep `/var/lib/chessonline/keyring` out of packages and never copy/log/commit key files. | `docs/P4K_PACKAGE_RUNTIME_BOUNDARY.md` | Verify service template path and avoid keyring content reads. |
| Linux systemd/nginx boundary | Microsoft Learn, Host ASP.NET Core on Linux with Nginx | Updating the Kestrel app payload behind existing nginx does not require changing nginx, TLS, firewall, or 443. | Later P4K deploy can replace `/opt/chessonline/server` and restart only `chessonline.service`; Phase 01 stays docs-only. | `docs/P4K_PACKAGE_RUNTIME_BOUNDARY.md` | Local docs/build/list checks only. |

## P4K Phase 02 - Server Build Identity

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Append-only diagnostics JSON | Microsoft Learn, ASP.NET Core minimal APIs | Adding new JSON fields is compatible with existing clients that ignore unknown fields. | Add a `build` object to `/chess3d/diagnostics` while preserving `serverCommit` and all old fields. | `src/ChessOnlineServer/ChessOnlineServerHost.cs`, protocol DTOs | Server build and targeted online contract tests. |
| Publish metadata file | Microsoft Learn, `dotnet publish` command | Publish output can include additional content files next to the app DLL. | Generate `server-build.json` in the publish output with commit/package/time metadata only. | `scripts/deploy/Publish-ChessOnlineServer-Linux.ps1` | Build/publish later; diagnostics works with or without the file. |
| Secret-safe build identity | OWASP Logging Cheat Sheet | Build/deploy metadata should not contain local paths, usernames, tokens, or machine names. | Store only commit, UTC build time, package id, and assembly informational version fallback. | `docs/P4K_SERVER_BUILD_IDENTITY.md` | Contract test checks no token/local-path markers in serialized diagnostics. |

## P4K Phase 03 - Capability Predeploy Gate

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Hub method parity | Microsoft Learn, ASP.NET Core SignalR .NET client | A remote smoke can only pass if the deployed hub exposes the invoked methods. | Treat local source capability checks as a pre-deploy gate before copying a server package. | `docs/P4K_CAPABILITY_PREDEPLOY_GATE.md` | `rg` for methods/flags and targeted online contract tests. |
| Diagnostics feature flags | Microsoft Learn, ASP.NET Core health checks and minimal APIs | Machine-readable readiness/diagnostics should expose server capabilities before functional smoke. | Require `resumeMatch`, `spectatorMode`, `lobbySnapshot`, and matching `supportedHubMethods` before claiming remote PASS. | docs only | Contract tests and later public `/chess3d/diagnostics`. |
| Secret-safe capability reporting | Microsoft Learn SignalR security; OWASP Logging Cheat Sheet | Capability lists do not need tokens, player credentials, or connection IDs. | Keep the pre-deploy gate to method names, booleans, counts, and package identity only. | docs only | Secret scan remains a later P4K gate. |

## P4K Phase 04 - Harden Linux Server Packaging

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Reproducible publish metadata | Microsoft Learn, `dotnet publish` command | Publish output can be inspected and augmented after the build step. | Generate `server-build.json` and `server-package-manifest.json` in the publish output. | `scripts/deploy/Publish-ChessOnlineServer-Linux.ps1` | Run script against `.tmp` output and inspect required files. |
| SHA-256 package manifest | GitHub Actions artifact guidance and OWASP logging guidance | Manifests should identify files without embedding secrets or local machine details. | Store relative path, length, and SHA-256 for package files; exclude runtime stores/keyrings. | publish script/docs | Manifest parse via PowerShell JSON conversion. |
| Controlled cleanup | Existing P4D1 safety policy | Cleanup must not remove arbitrary directories. | `-Clean` is accepted only for output paths under `.tmp/`. | publish script | Self-check uses `.tmp/p4k-phase04-publish`. |
| Secret-like file guard | OWASP Logging Cheat Sheet | Tokens, passwords, keyrings, and private-key-like files should fail packaging. | Add `-FailOnSecretLikeFiles` filename guard for deploy package output. | publish script | Self-check runs with the switch enabled. |

## P4K Phase 05 - Build Deployment Server Package

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Framework-dependent `linux-x64` publish | Microsoft Learn, `dotnet publish` command and .NET application publishing overview | `dotnet publish -r linux-x64 --self-contained false` is a supported framework-dependent package shape when the target host has the runtime. | Build an untracked `.tmp` package for `ChessOnlineServer` and include the tested Linux native authority library explicitly. | `scripts/deploy/Publish-ChessOnlineServer-Linux.ps1`, `docs/P4K_SERVER_PACKAGE_RESULT.md` | Server build, publish script run, manifest parse, archive hash. |
| Runtime key separation | Microsoft Learn, ASP.NET Core Data Protection key management | Data Protection key rings are runtime state and must not be embedded in immutable application payloads. | Keep keyring/store paths out of the package and document that `/var/lib/chessonline` remains untouched. | package docs | Inspect archive listing and manifest. |
| Artifact provenance and secret boundary | GitHub Actions artifact/security guidance and OWASP Logging guidance | Deployable artifacts need file identity while avoiding plaintext secrets or runtime logs. | Generate `server-build.json`, `server-package-manifest.json`, SHA-256 archive hash, and remove PDB/dev appsettings before archive creation. | publish script/docs | Archive listing checks for required files and absence of PDB/dev configs. |

## P4K Phase 06 - WSL Package Preflight

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Optional local Linux smoke | Microsoft Learn, .NET on Linux and `dotnet --info` diagnostics | A local Linux preflight is useful only when the distro has the .NET runtime/SDK available. | Check WSL read-only and skip the preflight when `.NET` is absent; do not install toolchains in this phase. | `docs/P4K_WSL_PACKAGE_PREFLIGHT.md` | `wsl -l -v`, `wsl -d Ubuntu -- dotnet --info`, `uname`, `command -v`. |
| Safe deploy gate | Microsoft Learn, Host ASP.NET Core on Linux with Nginx | The actual app will run behind existing nginx on Hetzner, so local WSL is optional, not a substitute for backup-first remote deployment. | Treat WSL preflight as skipped and continue with Hetzner inventory/backup gates. | docs only | Record skipped prerequisite and no server changes. |
| Runtime state boundary | Microsoft Learn, ASP.NET Core Data Protection | Runtime keyrings and stores stay on the server and must not be copied into local WSL/package tests. | Do not copy `/var/lib/chessonline`; only package payload would be tested if prerequisites existed. | docs only | Confirm no runtime content read or copied. |

## P4K Phase 07 - Hetzner Predeploy Inventory

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Linux service inventory | Microsoft Learn, Host ASP.NET Core on Linux with Nginx | A Kestrel app behind nginx can be inventoried through `systemctl show`, port checks, and local health probes without changing nginx or firewall state. | Capture `chessonline.service` metadata, ports, payload hashes, and health before backup/deploy. | `docs/P4K_HETZNER_PREDEPLOY_INVENTORY.md` | Read-only SSH, `ss`, `stat`, `sha256sum`, `curl`. |
| Health/readiness baseline | Microsoft Learn, ASP.NET Core health checks | Liveness/readiness endpoints are appropriate deployment gates before package replacement. | Record both local Kestrel and public nginx health results plus capability flags. | docs only | `curl` local and public endpoints. |
| Runtime secret boundary | Microsoft Learn, ASP.NET Core Data Protection; OWASP Logging Cheat Sheet | Keyrings and persistence stores are sensitive runtime state and should not be printed or copied during deploy inventory. | Read only ownership/mode metadata for `/var/lib/chessonline` and `/var/lib/chessonline/keyring`. | docs only | No `ls` or file reads inside runtime/keyring paths. |

## P4K Phase 08 - Hetzner Server Backup

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Backup before payload replacement | Microsoft Learn, Host ASP.NET Core on Linux with Nginx | App payload replacement behind a systemd service should have an operator rollback point before files are swapped. | Create a server-side tarball of `/opt/chessonline/server` and the `chessonline.service` unit before deployment. | `docs/P4K_HETZNER_BACKUP_RESULT.md` | `tar`, `sha256sum`, `ls -lh`, required-entry listing. |
| Runtime data separation | Microsoft Learn, ASP.NET Core Data Protection | Data Protection keyrings and app persistence stores are mutable runtime state, not immutable payload. | Do not archive `/var/lib/chessonline` in Phase 08; record only app payload rollback artifact. | docs only | Confirm backup path/entries include server payload and unit only. |
| Secret-safe backup reporting | OWASP Logging Cheat Sheet | Reports should include enough rollback metadata without printing runtime credentials or key material. | Record archive path, size, SHA-256, mode and required entries; do not print config contents or keyring/store files. | docs only | Inspect command output and doc content. |

## P4K Phase 09 - Guarded Deploy/Rollback Tool

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Systemd payload replacement | Microsoft Learn, Host ASP.NET Core on Linux with Nginx | Updating a Kestrel app behind nginx can be scoped to the app payload and its service, with nginx/firewall untouched. | Add a script that swaps only `/opt/chessonline/server` and restarts only `chessonline.service`. | `scripts/deploy/Deploy-ChessOnlineServer-Hetzner.ps1` | Parse test and local dry-run. |
| Health/capability gate | Microsoft Learn, ASP.NET Core health checks and SignalR .NET client docs | Deployment success should be gated by health endpoints and the expected hub capability surface. | Verify live/ready/diagnostics and P4K methods after real deploy; dry-run prints the plan. | deploy script/docs | Dry-run output and later remote phases. |
| Secret-safe operator tooling | Microsoft Learn SignalR security; OWASP Logging Cheat Sheet | Deploy logs should avoid tokens, runtime store content, and key material. | Validate archives for secret-like names and avoid printing runtime files or credentials. | deploy script/docs | Archive validation plus `git diff --check`. |

## P4K Phase 10 - Deployment Dry Run

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Dry-run deploy safety | Microsoft Learn, Host ASP.NET Core on Linux with Nginx | A deploy plan can be validated locally before touching the remote service. | Run `Deploy-ChessOnlineServer-Hetzner.ps1 -DryRun` from a clean tree against the real archive and expected commit. | `docs/P4K_DEPLOY_DRY_RUN_RESULT.md` | Script dry-run output and public health after dry-run. |
| Health baseline after no-op | Microsoft Learn, ASP.NET Core health checks | Health endpoints should remain unchanged when no deploy mutation occurred. | Re-check public live/ready/diagnostics after dry-run and expect the old capability surface. | docs only | `curl` public endpoints. |
| Secret-safe operator output | OWASP Logging Cheat Sheet | Dry-run logs should show plan metadata, not credentials or tokens. | Record archive SHA, package id, target path, and capability plan only. | docs only | Inspect dry-run output and doc. |

## P4K Phase 11 - Stage Hetzner Server Package

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Staged upload before service mutation | Microsoft Learn, Host ASP.NET Core on Linux with Nginx | A package can be uploaded and verified before stopping the Kestrel service. | Stage the archive under `/opt/chessonline/incoming` and verify checksum/content before any server directory swap. | `docs/P4K_HETZNER_UPLOAD_RESULT.md` | `scp`, remote `sha256sum`, `tar -tzf`, build identity checks. |
| Capability gap confirmation | Microsoft Learn, ASP.NET Core health checks and SignalR .NET client | The active server should remain unchanged after staging only. | Re-check public health/diagnostics and expect old capability surface until Phase 12 swap. | docs only | Public `curl` after upload. |
| Secret-safe staging | OWASP Logging Cheat Sheet | Staging logs should not expose tokens, stores, keyrings, or config contents. | Record archive path, mode, hash, required entries and commit only. | docs only | No runtime state read; no token output. |

## P4K Phase 12 - Atomic Server Directory Swap

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| App-only payload replacement | Microsoft Learn, Host ASP.NET Core on Linux with Nginx | Kestrel app payload can be updated behind existing nginx by replacing app files and restarting the app service only. | Swap `/opt/chessonline/server`, keep previous directory, and restart only `chessonline.service`. | `docs/P4K_HETZNER_ATOMIC_SWAP_RESULT.md` | Guarded deploy script plus immediate loopback/public health. |
| Rollback on failed health | Microsoft Learn, ASP.NET Core health checks | Post-swap health gates should decide whether to keep or rollback the payload. | Use `-RollbackOnFailure`; no rollback was needed because diagnostics passed. | deploy result docs | Check service active, health, expected commit and capabilities. |
| Neighbor service boundary | OWASP Logging and operational safety guidance | Deployment logs should avoid unrelated services and sensitive runtime data. | Do not touch nginx, 443, x-ui/Xray, Outline, Docker, Unreal, PostgreSQL, or `/var/lib/chessonline`. | docs only | Record allowed/forbidden mutations and immediate diagnostics. |

## P4K Phase 13 - Immediate Post-Deploy Health Gate

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Multi-hop health gate | Microsoft Learn, ASP.NET Core health checks | A deployment should pass loopback service health and public proxy health before gameplay smoke. | Verify Kestrel loopback, nginx-local, and public HTTP live/ready/diagnostics. | `docs/P4K_HETZNER_DEPLOY_RESULT.md` | `curl` on loopback, local nginx, public HTTP. |
| SignalR capability verification | Microsoft Learn, ASP.NET Core SignalR .NET client | Clients should only use hub methods that deployed diagnostics advertise. | Assert `RequestResumeMatch`, `JoinSpectator`, and `RequestLobbySnapshot` in `supportedHubMethods`. | deploy result docs | Diagnostics grep assertions. |
| Post-deploy risk scan | OWASP Logging Cheat Sheet and operational logging guidance | Journal checks should look for actionable failure classes without exposing secrets. | Scan recent `chessonline.service` journal for crash/native/persistence/permission/sequence/unhandled failure markers. | docs only | Sanitized journal risk scan summary. |

## P4K Phase 14 - Rollback Command Readiness

Date: 2026-07-11

| Topic | Source checked | Key finding | Decision for this repo | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Rollback command safety | Microsoft Learn, Host ASP.NET Core on Linux with Nginx | A Kestrel app payload rollback can be limited to the app service and payload directory while nginx/firewall/TLS stay untouched. | Add an explicit `-RollbackTo` path and `-RollbackDryRun` mode to the existing deploy tool; run only dry-run in this phase. | `scripts/deploy/Deploy-ChessOnlineServer-Hetzner.ps1`, `docs/P4K_ROLLBACK_READINESS.md` | Remote read-only validation of current payload, previous payload, backup archive, and planned command. |
| Health-gated rollback | Microsoft Learn, ASP.NET Core health checks | A real rollback should be followed by loopback live/ready/diagnostics checks. | The script's actual rollback path includes service restart and health checks, but Phase 14 does not execute it because the deployed server is healthy. | deploy script/docs | Dry-run prints exact health gates and service boundary. |
| Legacy payload identity | Existing Hetzner deployment layout and OWASP Logging Cheat Sheet | The previous deployed directory predates `server-build.json`, so rollback readiness must not invent a build ID or print secrets. | Verify the new active build ID, allow legacy rollback payloads without build identity, and record the missing identity honestly. | docs only | SSH metadata reads only; no runtime store/keyring content. |

## P4K Phase 15 - Remote Online UX Smoke Tooling

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Awaited hub invocation | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | Hub calls are asynchronous; `InvokeAsync` should be awaited and failures handled explicitly. Client callbacks must be registered before `StartAsync`. | Keep every smoke hub call awaited, register the spectator action callback before connection start, and bound the entire process with the C# watchdog. | `tools/HetznerSignalRSmoke/Program.cs` | Release build, scenario dry-runs, targeted online contract tests, later remote scenarios. |
| Capability-driven protocol use | Microsoft Learn SignalR client guidance and current `/chess3d/diagnostics` contract | A deployed hub method must be advertised and present before an explicit scenario can pass. | Gate resume, spectator, and lobby scenarios on both their boolean feature and `supportedHubMethods`; missing support is a failure, not a fallback PASS. | smoke tool/docs | Invalid/missing capability produces a sanitized non-zero result; remote proof follows in Phases 16-19. |
| SignalR secret exposure | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | Access tokens may appear in transport query strings, and connection tokens/IDs should not be exposed in reports. | Do not dump exception stacks or secret-bearing URLs; shorten public player identifiers and redact query strings in wrapper output. | smoke tool, wrapper, docs | Source scan and redacted-tail dry-run checks. |
| Log data exclusion | [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html) | Access tokens, session identifiers, passwords, keys, and other primary secrets should be removed, masked, or not logged. | Keep generated credentials in memory, redact sensitive wrapper tails, and store unique raw logs only below ignored `.tmp/`. | `scripts/deploy/Test-HetznerOnlineUx.ps1` | Two dry-runs produce distinct paths; `.gitignore` includes `.tmp/`; secret scan before finalization. |
| CI monitoring boundary | [GitHub Docs: Monitor workflows](https://docs.github.com/en/actions/how-tos/monitor-workflows) | Workflow runs and their logs should be monitored after each pushed phase. | Push this tooling as one phase commit, watch its GitHub Actions run, and keep remote Hetzner smoke operator-only. | docs/process | `gh run list` and `gh run watch` after push. |

## P4K Phase 16 - Remote Match Resume Proof

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SignalR reconnect identity | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | A restarted connection is a new transport connection; application match identity must be restored explicitly through server-authoritative state. | Stop/start the authenticated primary relay and invoke `RequestResumeMatch` with player, room, table, seat, ruleset, last hash, and sequence context. | smoke tool result docs | Public Asgard and Classic `resume` scenarios. |
| Authenticated reconnect | [Microsoft Learn: SignalR authentication and authorization](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz) | The .NET client provides its access token when establishing each connection. | Reuse the in-memory temporary-user token only inside `AccessTokenProvider`; do not log or persist it. | operator smoke only | `-NoSecretLog`, sanitized output, no raw logs committed. |
| Authority no-mutation invariant | Existing `RequestResumeMatch` registry contract and OWASP logging guidance | Resume should return the authoritative snapshot/history and restore group membership without changing board state or action count. | Assert same room/table/ruleset/hash/sequence/action count and matching action-log history after reconnect. | `docs/P4K_REMOTE_RESUME_SMOKE_RESULT.md` | Bounded scenario runs for Asgard and Classic, followed by public health. |

## P4K Phase 17 - Resume Authority Boundaries

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Authenticated hub identity | [Microsoft Learn: SignalR authentication and authorization](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz) | The server should bind each connection to its authenticated user; request payload identity alone is not authority. | Probe wrong-player and unseated temporary-user resume attempts over separately authenticated connections. | smoke tool/wrapper/result docs | Expected `playerNotInTable`, unchanged snapshot/action counters. |
| Safe hub failures | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | Detailed server exceptions should not be exposed to remote clients. | Require explicit protocol failure reasons and reject stack-trace-shaped failure text in negative probes. | `tools/HetznerSignalRSmoke/Program.cs` | Remote wrong room/table/ruleset/player probes. |
| Stale client reconciliation | Existing `OnlineRoomRegistry.RequestResumeMatch` contract | `LastKnownServerSeq` selects an action-log tail, while the current runtime returns an authoritative snapshot rather than rejecting a stale client hash. | Test stale hash and old sequence as successful reconciliation, document that `staleState` is currently not emitted by resume, and verify no mutation. | smoke tool/result docs | Hash/action count/accepted-action diagnostics before and after all probes. |

## P4K Phase 18 - Remote Spectator Proof

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Group broadcast observation | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | Client callbacks should be registered before connection start so early server events are not missed. | Register `ReceiveActionAccepted` before starting spectator C, join the table group, then require the next accepted player action broadcast. | remote smoke/result docs | Asgard and Classic spectator scenarios. |
| Transport identity is not authority | [Microsoft Learn: SignalR authentication and authorization](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz) | Authentication identifies the viewer, but game mutation still requires an assigned seat/actor authority. | Use an authenticated temporary spectator with no seat and require Ready, StartGame, and SubmitAction to be rejected. | smoke tool/result docs | Snapshot hash before/after rejected calls plus accepted action from a seated player. |
| Spectator privacy | [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html) | Logs should exclude session identifiers, tokens, passwords, and other authentication secrets. | Report only shortened spectator/player ids, room/table context, hash/seq, and sanitized reasons; keep raw logs ignored. | `docs/P4K_REMOTE_SPECTATOR_SMOKE_RESULT.md` | `-NoSecretLog`, source scan, no `.tmp` files staged. |

## P4K Phase 19 - Remote Lobby Discovery Proof

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Safe discovery payload | [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html) | Discovery/operational records should omit tokens, passwords, full session identifiers, and private runtime data. | Serialize the selected lobby row during smoke and reject secret-bearing field names; require shortened seat labels. | smoke tool/result docs | Public lobby scenario and source/output review. |
| Lobby-to-hub workflow | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | Hub method results should be awaited and used as authoritative inputs to subsequent calls. | Use the exact selected row's room/table/ruleset/seat data for `JoinSpectator` and same-player `RequestResumeMatch`. | `tools/HetznerSignalRSmoke/Program.cs` | Start match, discover exact row, join spectator, resume player, compare hash/action count. |
| Public state minimization | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | Connection transport identifiers and secret-bearing URLs should not be exposed to other clients. | Assert row schema contains only lifecycle/count/short-label fields and never `ConnectionId`, token, password, email, key, or store data. | result docs | Serialized row deny-list plus no-secret wrapper output. |

## P4K Phase 20 - Integrated Remote Online UX

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Deterministic operator flow | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | Awaited hub calls provide a deterministic client-side sequence when each result gates the next operation. | Keep `all` ordered as health/capability gate, play, resume, lobby, spectator, final diagnostics; reserve pre-match lobby only for the dedicated lobby scenario. | `tools/HetznerSignalRSmoke/Program.cs` | One bounded public `all` run and log-order index check. |
| Unique diagnostic logs | [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html) | Operational logs need controlled destinations and must not expose secrets. | Use one unique stdout/stderr pair for the combined run, redact displayed tails, and keep raw files under ignored `.tmp`. | wrapper/result docs | Check resolved paths, no shared-file error, no raw log staged. |
| Post-smoke service evidence | [Microsoft Learn: Host ASP.NET Core on Linux with Nginx](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx) | Application health and service logs are distinct operational signals behind a reverse proxy. | Check public live/ready/diagnostics and read only a sanitized journal risk count; do not alter nginx/systemd/network configuration. | `docs/P4K_REMOTE_ALL_SCENARIO_RESULT.md` | Public curl and SSH journal marker count after smoke. |

## P4K Phase 21 - Post-deploy Five-profile Regression

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Sequential remote regression | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | Hub invocations and connection disposal are asynchronous operations that should be awaited before starting the next isolated client flow. | Run the five profiles one at a time with unique run IDs and bounded watchdog timeouts. | result docs | Five sequential `play` runs and post-run health checks. |
| Profile-safe action dispatch | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | Server authorization remains authoritative; clients must not infer permissions from transport state. | Submit server-preview legal actions for Classic and Asgard; use `-SkipActionSubmit` for Single, Rubik, and Hodge in this regression rather than fabricating special actions. | smoke result docs | Assert accepted preview action for Classic/Asgard and clean snapshot/action-log completion for the other profiles. |
| Secret-minimized evidence | [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html) | Authentication tokens, passwords, and sensitive session data should be excluded from operational logs. | Use temporary users, `-NoSecretLog`, ignored `.tmp` logs, and retain only sanitized outcomes/hashes in tracked documentation. | result docs | Empty stderr, redacted wrapper output, and Git status review. |

## P4K Phase 22 - Post-smoke Server Isolation

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Service health behind nginx | [Microsoft Learn: Host ASP.NET Core on Linux with Nginx](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx) | Kestrel and the reverse proxy have separate process/listener health that should be inspected independently. | Read only service states and listeners for ChessOnline/nginx and the already-known neighboring ports; make no service or network changes. | inventory doc | `systemctl is-active`, filtered `ss`, public health, and localhost ChessOnline health. |
| Operational log minimization | [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html) | Operational verification should avoid collecting credentials, tokens, and unnecessary raw event payloads. | Record journal risk counts and service summaries only; do not retain raw journal, environment, configuration, or command lines containing secrets. | `docs/P4K_POSTSMOKE_SERVER_INVENTORY.md` | Sanitized marker counts and deny-list review. |
| Deployment isolation | [GitHub Actions documentation](https://docs.github.com/en/actions) | Automated repository verification should be reproducible and should not depend on mutable external infrastructure. | Keep this inventory manual/read-only and outside CI; confirm no firewall, nginx, TLS, or neighbor-service mutation commands are issued. | inventory doc | Command review plus current listener/container/resource inventory. |

## P4K Phase 23 - WPF Resume Smoke

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF UI automation boundary | [Microsoft Learn: WPF threading model](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model) | WPF controls belong to the UI dispatcher; an external UI Automation client can invoke controls without reaching into application internals. | Use an ephemeral UIA driver under ignored `.tmp/manual-smoke`; do not add a fragile UI test to CI. | `src/ChessOnlineApp/MainWindow.xaml.cs`, result docs | Release build, launch one app, invoke named controls, poll UIA status text with bounded waits, close process. |
| Resume authority | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | Reconnecting transport and restoring application state are distinct operations; authoritative state must be requested after reconnect. | Exercise Disconnect Primary Relay, Reconnect Primary Relay, then Resume Current Match and verify refreshed hash/action log with no duplicate action. | result docs | Accepted server-preview action followed by disconnect/reconnect/resume and status assertions. |
| Secret-safe manual evidence | [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html) | Test evidence should omit passwords, access tokens, and sensitive session identifiers. | Keep generated credentials in app memory, write automation evidence only under ignored `.tmp`, and track a sanitized summary without runtime IDs. | result docs | Inspect sanitized status/report and Git status before commit. |

## P4K Phase 24 - WPF Spectator Smoke

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Independent WPF clients | [Microsoft Learn: WPF threading model](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model) | Each WPF application process owns an independent dispatcher and UI state. | Use separate A/B-host and C-spectator processes, each driven through external UI Automation with bounded waits. | result docs | Start match in A, pass only room/table display values to C, close both processes in `finally`. |
| Spectator authorization | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | A connected client must still be authorized for each operation; transport connection alone does not grant mutation authority. | Require the C UI to show spectator/read-only state and keep Ready, Start, and all submit commands disabled before and after selecting history. | result docs | UIA enabled-state checks plus live/action-log observation after A submits. |
| Live update evidence | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | Client handlers receive hub events asynchronously and should update UI through the app's synchronization boundary. | Verify spectator realtime seq/hash advances after A's accepted server-preview action, then refresh/follow authoritative snapshot and log. | result docs | Compare pre/post spectator snapshot hash, action count, realtime seq, and list contents. |

## P4K Phase 25 - WPF Lobby Smoke

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Safe table discovery | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | Public client state must not expose connection identifiers, access tokens, or other transport credentials. | Select the newly created Asgard row by displayed room/table, scan the row/status against a secret-field deny-list, and copy only room/table into spectator fields. | result docs | WPF lobby refresh, exact row selection, displayed-text deny-list, field equality checks. |
| UI-to-authority boundary | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | Hub method responses remain authoritative even when a client selected a row locally. | Prove Spectate Selected succeeds for an authenticated temp viewer, while Resume Selected for that unseated viewer returns a safe authority rejection. | result docs | Join spectator, request snapshot, then expect `playerNotInTable` without state mutation. |
| Bounded manual UI evidence | [Microsoft Learn: WPF threading model](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model) | UI work and asynchronous completion must remain coordinated through the WPF dispatcher. | Drive two independent WPF processes through external UIA with per-step waits and a C# watchdog; keep runtime output under ignored `.tmp`. | result docs | Bounded run, process cleanup, sanitized tracked summary. |

## P4K Phase 26 - Three-client End-to-end Flow

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Independent player contexts | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | Each `HubConnection` maintains independent connection and reconnect state. | Use three separate `ChessOnlineApp` processes: manual player A, manual player B, and lobby-discovered spectator C. | result docs | Distinct temp registration, matchmaking, ready/start, and UI state assertions per process. |
| Shared authoritative history | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | Group broadcast does not replace server authorization or authoritative state reconciliation. | Require A's accepted preview action to advance B and C realtime state to the same hash; keep C mutation controls disabled. | result docs | Compare A/B/C hash and seq, action log visibility, read-only guards. |
| Disconnect isolation and resume | [Microsoft Learn: WPF threading model](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model) | Independent app dispatchers continue processing even when another process disconnects. | Disconnect A only, prove B snapshot and C follow still work, then reconnect/resume A and compare the latest authoritative hash. | result docs | B/C operations during A outage, A resume, bounded cleanup, ignored sanitized reports. |

## P4K Phase 27 - Spectator Lifecycle Audit

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Hub disconnect lifecycle | [Microsoft Learn: Use hubs in ASP.NET Core SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs) | `OnDisconnectedAsync` is the application hook for connection cleanup; SignalR itself removes disconnected connections from groups. | Treat SignalR groups as transport routing, not as the authoritative spectator registry or count. | audit docs | Trace `JoinSpectator`, `OnDisconnectedAsync`, and generic connection removal. |
| Stable viewer identity | [Microsoft Learn: Manage users and groups in SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups) | Connections are transient and one user can have multiple connections; group membership is not a security boundary and is not retained across reconnect/server restart. | Key future spectator membership by authenticated player identity plus current connection, never by public `ConnectionId`. | audit docs | Compare DTO identity fields with internal connection/session mappings. |
| Public data minimization | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | Connection identifiers and transport credentials should not be exposed as public application state. | Keep lobby output count-only; prohibit connection IDs in DTOs, diagnostics, reports, and logs. | audit docs | Inspect public spectator/lobby DTOs and serialization tests. |

## P4K Phase 28 - Internal Spectator Registry

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| User versus connection identity | [Microsoft Learn: Manage users and groups in SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups) | A user may have multiple transient connections; group membership is connection-scoped and not a durable membership store. | Count distinct authenticated viewers per table in a server-internal registry and retain only the current transport connection for routing cleanup. | server registry, hub, tests, docs | First/duplicate/second/reconnect count tests. |
| Group replacement | [Microsoft Learn: Use hubs in ASP.NET Core SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs) | Group operations use connection IDs and application code owns its higher-level membership semantics. | On same-viewer reconnect, replace the registry mapping and remove the superseded connection from room/table groups without exposing its ID. | hub, registry | Reconnect count remains stable and lobby count reflects distinct viewers. |
| Public minimization | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | Transport identifiers are sensitive implementation detail and should not become public protocol state. | Keep connection IDs inside the server assembly; mutate only `SpectatorCount` in the existing lobby DTO. | server registry, lobby response, tests | Serialize lobby and assert no connection IDs/tokens; game hash and seats unchanged. |

## P4K Phase 29 - Disconnect Cleanup

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Disconnect callback | [Microsoft Learn: Use hubs in ASP.NET Core SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs) | `OnDisconnectedAsync` runs for intentional and failed connections; SignalR removes transport group membership automatically. | Use the callback to remove the server's spectator record and mark a seat disconnected only when its session has no other live connection. | hub, registries, tests, docs | Spectator count decrement, duplicate cleanup, multi-connection-safe player marker. |
| Resume identity | [Microsoft Learn: Manage users and groups in SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups) | Reconnecting creates transport membership that applications must re-establish; user identity is separate from connection ID. | Preserve seat ownership and game context on disconnect; authenticated `Hello` marks the retained seat connected before resume. | room registry, persistence, hub | Disconnect player, inspect lobby, reconnect/resume, inspect same seat/hash. |
| State isolation | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | Authorization must be tied to stable user identity, not transport identifiers. | Cleanup changes only presence metadata and spectator counts; never board, action log, server sequence, or state hash. | tests, docs | Hash equality across spectator/player disconnect and resume. |

## P4K Phase 30 - Room Lifecycle Policy

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Deterministic time | [Microsoft Learn: Testing with FakeTimeProvider](https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing) | Time-dependent cleanup is reliable only when code consumes an injected UTC clock that tests can advance explicitly. | Require `TimeProvider` injection and fake-clock tests before any expiry implementation. | policy docs | Boundary tests at TTL-minus-one, TTL, and TTL-plus-one without sleeping. |
| Bounded cleanup | [Microsoft Learn: Background tasks with hosted services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services) | Hosted background work must honor cancellation and avoid unbounded work per iteration. | Use a configurable interval and maximum removals per run; expose aggregate counters only. | policy docs | Fake-clock service test and bounded batch assertions. |
| Resume preservation | [Microsoft Learn: Manage users and groups in SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups) | Connection loss is transport state; stable user/application state must be managed separately. | Never delete `active` or `disconnected-resumable` tables in the first cleanup implementation. | policy docs | Active/resumable fixtures survive arbitrarily advanced cleanup clock. |

## P4K Phase 31 - Bounded Room Cleanup

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Injectable UTC clock | [Microsoft Learn: Testing with FakeTimeProvider](https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing) | Injecting `TimeProvider` avoids real sleeps and makes expiry boundaries deterministic. | Add optional `TimeProvider` to the in-memory registry and service; production uses `TimeProvider.System`, tests use a manual provider. | protocol registry, server cleanup, tests | Explicit clock advances across waiting and retention boundaries. |
| Hosted service bounds | [Microsoft Learn: Background tasks with hosted services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services) | Background loops must honor cancellation and constrain each unit of work. | Use `PeriodicTimer`, configurable interval, and maximum removals; expose a callable single-run coordinator for tests. | server service/options | Batch-limit tests and clean cancellation/build. |
| Persistence safety | Repository `IOnlineRoomPersistenceStore` audit plus [Microsoft Learn: Hosted services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services) | Current store has no atomic room/table delete contract, so deleting persisted records would be unsafe. | Phase 31 cleans only eligible in-memory tables and orphan spectator records; persistent deletion is explicitly deferred. | registry, docs | Verify retained active/resumable state and document persistence boundary. |

## P4K Phase 32 - Lifecycle Deployment

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Linux publish boundary | [Microsoft Learn: dotnet publish](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) | `dotnet publish` produces the application files intended for deployment to a hosting system. | Reuse the existing reproducible linux-x64 publish script, external tested `libChess3DEngine.so`, build identity, manifest, and secret-file gate. | deployment artifacts, result docs | Verify manifest, exactly five profiles, package SHA-256, ELF native library, and archive contents before upload. |
| Kestrel process ownership | [Microsoft Learn: Host ASP.NET Core on Linux with Nginx](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx) | Nginx proxies requests while `systemd` owns start/stop/restart of the Kestrel application process. | Update only the ChessOnline payload through the existing guarded service swap; do not edit nginx, UFW, ports, TLS, or neighboring services. | operator deployment, result docs | Backup current payload, verify expected current commit, atomic directory swap, bounded health gate, automatic rollback on failure. |
| Post-deploy authority | Repository P4K deploy tooling and operator smoke contracts | A successful process restart is insufficient without build identity, capability, gameplay, and neighbor checks. | Require diagnostics to report the new commit/package and lifecycle counters, then run the remote `all` scenario and compare neighboring listeners. | result docs | Loopback/public health, diagnostics, service state, listener snapshot, journal scan, remote all scenario. |

## P4K Phase 33 - Restart Rehydration Audit

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Startup ordering | [Microsoft Learn: Background tasks with hosted services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services) | A short `IHostedService.StartAsync` can complete initialization before the web server accepts requests. | A future restore service can validate and install healthy matches before exposing them, but Phase 33 remains audit-only. | audit docs | Trace host DI/startup and identify a future bounded startup hook. |
| Transport membership after restart | [Microsoft Learn: Manage users and groups in SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups) | Connection/group membership is transient and must be re-established for each connection. | Restore seat ownership by stable authenticated `PlayerId`; force every restored seat disconnected and let explicit resume rebuild SignalR groups. | audit docs | Compare persisted seats/session last-known match with current resume authorization. |
| Native state truth | Repository save/load contracts and native roundtrip tests | Savegame v0.1 can recreate all five profile states transactionally and yield a deterministic hash, but online persistence currently retains the start snapshot and appends later actions separately. | Do not claim exact restart recovery until snapshot/log ordering, continuity, schema version, and quarantine policy are implemented and tested. | audit docs | Map every persisted field and identify crash windows and missing validation. |

## P4K Phase 34 - Restart Rehydration Design

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Pre-listen restore | [Microsoft Learn: Background tasks with hosted services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services) | Short startup initialization can finish before the server begins accepting requests. | Use a bounded startup service to validate private candidate sessions and atomically install only healthy matches before readiness. | design docs | Future tests instantiate a fresh registry/store and assert no partial table visibility. |
| Resume transport boundary | [Microsoft Learn: Manage users and groups in SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups) | SignalR group membership is connection-scoped and isn't retained across reconnect/restart. | Persist stable seat ownership only; restore all seats disconnected and rebuild groups after authenticated explicit resume. | design docs | Future resume tests use new connections and stable player identity. |
| Safe failure telemetry | [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html) | Logs should avoid credentials and sensitive identifiers while retaining actionable event classifications. | Quarantine invalid descriptors with aggregate reason codes; never log savegame JSON, action JSON, tokens, passwords, or connection IDs. | design docs | Future diagnostics expose counts/reason categories only. |

## P4K Phase 37 - SignalR Logging Audit

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Query-token logging | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | WebSocket/SSE access tokens may appear in the query string, and ASP.NET Core request-start Info logs can record that URL. | Treat current framework Info request logging as a hardening gap even though the sanitized remote count currently finds no `access_token=` entries. | audit docs | Count sensitive markers in remote journal without printing matching lines; recommend category filtering before production use. |
| Detailed hub exceptions | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | `EnableDetailedErrors` can disclose exception details and should stay disabled outside controlled development. | Retain the false default/configuration and generic client errors; log only server-side exception type/context. | audit docs | Trace hub option binding and exception response paths. |
| Raw diagnostic logs | [Microsoft Learn: SignalR logging and diagnostics](https://learn.microsoft.com/en-us/aspnet/core/signalr/diagnostics) | Server diagnostic logs may contain sensitive application information and shouldn't be published raw. | Keep watchdog/smoke/UI reports under ignored `.tmp`; track sanitized summaries only. | audit docs | Verify `.gitignore`, report writers, and tracked docs. |

## P4K Phase 38 - Rate Limit And Abuse Audit

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Partitioned limits | [Microsoft Learn: Rate limiting middleware in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit) | ASP.NET Core supports named and partitioned limiters with explicit permit, window, and queue policies. | Partition authenticated work by stable player ID and unauthenticated HTTP work by normalized remote IP; never partition on a raw token. | audit docs | Future unit tests prove user isolation and redacted diagnostics. |
| Mutation queueing | [Microsoft Learn: Rate limiting middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit) | Queueing can defer requests rather than reject immediately and must be deliberately bounded. | Use queue limit 0 for auth mutations, matchmaking mutation, action submission, resume/spectator joins, and expensive preview work. | audit docs | Future burst tests require immediate HTTP 429 or safe hub rejection. |
| Existing guard reality | Repository `OnlineHubConnectionRegistry.AllowCommand` and hub routing audit | Current guard is one per-connection timestamp list and doesn't cover every hub/HTTP path; reconnect can reset the partition. | Retain it as defense-in-depth initially, but add method-class/player-aware guards only after deterministic tests and normal-flow simulation. | audit docs | Map every endpoint/method and classify cost/mutation. |

## P4K Phase 39 - Low-risk Application Limits

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| HTTP fixed-window policies | [Microsoft Learn: Rate limiting middleware in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit) | Named fixed-window policies support bounded permit counts, zero queues, and standard 429 rejection. | Add separate register/login/session/diagnostics policies; leave health endpoints outside those restrictive budgets. | server host/options/tests | Isolated test host proves allowed requests, burst 429, safe response, and usable health. |
| Stable hub partition | Repository hub/auth flow plus [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | Authenticated identity is stable across transport reconnect while connection IDs are transient. | Keep the existing global hub ceiling but partition authenticated calls by normalized player ID; anonymous fallback remains connection-scoped. | connection registry, hub, tests | Fake-clock tests prove reconnect cannot reset one player's budget and another player remains isolated. |
| Request log defense | [Microsoft Learn: SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) | Hosting/transport Info logs can capture token-bearing query URLs. | Set Hosting and Http.Connections categories to Warning while retaining safe application aggregate logs. | server host/tests/docs | Inspect logger rules and rerun auth/SignalR contracts. |

## P4K Phase 40 - Readiness Hardening

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Dependency readiness | [Microsoft Learn: Health checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks) | Readiness should test dependencies needed to process traffic, while liveness remains a basic process probe. | Keep `/healthz/live` unchanged and make `/healthz/ready` verify native authority, the exact five-profile set, writable persistence/keyring storage, registry construction, and normalized configuration. | server readiness probe/host/tests/docs | Test healthy and independently failed dependencies with safe public reason codes. |
| Keyring availability | [Microsoft Learn: Configure ASP.NET Core Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview) | A configured file key ring must remain accessible and protected with appropriate filesystem permissions. | Probe the configured keyring directory with a bounded create/delete marker without returning its path or key names. | server readiness probe/tests/docs | Inject an unavailable filesystem fixture and assert only `keyRingUnavailable` is public. |
| Public failure detail | [OWASP Error Handling Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Error_Handling_Cheat_Sheet.html) | Public errors should avoid internal implementation, path, permission, and stack details. | Return a fixed `notReady` status plus aggregate reason codes; retain details only inside the process boundary. | server endpoint/tests/docs | Assert serialized failure JSON contains no fixture path, exception, stack, key file, or native absolute path. |

## P4K Phase 41 - Low-risk Hardening Deployment

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Linux process boundary | [Microsoft Learn: Host ASP.NET Core on Linux with Nginx](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx) | Nginx forwards to Kestrel while systemd independently manages the application process. | Replace only the `/opt/chessonline/server` payload and restart only `chessonline.service`; do not alter nginx, ports, firewall, or neighboring services. | deployment result docs only; ignored package | Guarded deploy verifies archive identity, backup, service health, and atomic previous-directory retention. |
| CI gate monitoring | [GitHub Docs: Monitor workflows](https://docs.github.com/en/actions/how-tos/monitor-workflows) | Workflow status and per-step logs are the authoritative remote build result. | Package only the Phase 40 commit after its Windows Build success, then commit the sanitized deployment record and wait for its own CI. | research/deploy docs | Record run IDs and distinguish CLI transport failures from job conclusions. |
| Rollback discipline | Existing guarded deploy script and Phase 32 operator evidence | A new payload must not replace the last healthy payload without a separate backup and automatic health rollback. | Create a fresh backup, require exact commit/SHA/profile metadata, use `RollbackOnFailure`, and retain the prior directory. | ignored archive and remote ChessOnline payload only | Public live/ready/diagnostics, remote all scenario, journal scan, and listener inventory must all pass. |

## P4K Phase 42 - Full Local Verification

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Reproducible local gate | Repository decomposed runner contract plus [GitHub Docs: Monitor workflows](https://docs.github.com/en/actions/how-tos/monitor-workflows) | Local bounded tests identify the failing executable, while the clean GitHub workflow remains the independent package/build authority. | Run list, all contract tests without benchmark at `/m:1`, then full `verify.ps1` sequentially; do not overlap builds. | verification result docs | Record duration/result by group, optional CUDA status, and tracked-tree cleanliness. |
| Generated artifact boundary | Existing `.gitignore`, package manifest, and verify assertions | Build/package output is evidence but must stay outside tracked source. | Inspect status before and after the full gate and commit only sanitized result/research documents. | docs only | `git status --short`, `git diff --check`, runner summary, verify summary. |

## P4K Phase 43 - Final Remote Regression

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Operator regression | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | Client handlers must be registered before connection start and every hub invocation awaited and observed. | Use the existing bounded C# smoke tool/wrapper as the remote protocol authority; run scenarios sequentially with unique ignored logs. | remote regression docs only | Prove health, play, resume, lobby, spectator, combined flow, and all five profiles against deployed build identity. |
| Rate-aware smoke | Phase 39 fixed-window implementation and [Microsoft Learn: Rate limiting middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit) | Fixed-window budgets deliberately reject bursts sharing one partition until renewal. | Do not bypass or restart the limiter for operator tests; group generated-temp-user scenarios within the 5/10-minute registration budget and wait for genuine window renewal where required. | ignored smoke logs/docs | Every scenario must PASS without a 429; journal aggregate checks confirm normal flow stays below limits. |
| Secret-safe evidence | [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html) | Test evidence should exclude credentials, tokens, and sensitive identifiers while retaining event/result metadata. | Keep raw unique logs ignored, use `NoSecretLog`, and record only run IDs, durations, profile/result, aggregate journal counts, and shortened hashes. | remote regression docs | Search journal/log marker counts without printing matching secret-bearing lines. |

## P4K Phase 44 - Final Report And Operator Guidance

Date: 2026-07-13

| Topic | Official source checked | Key finding | Decision for this repository | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Linux service operations | [Microsoft Learn: Host ASP.NET Core on Linux with Nginx](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx) | Nginx and systemd have separate proxy and process responsibilities; application updates should preserve that boundary. | Document only the guarded ChessOnline payload workflow and explicitly prohibit changes to nginx, firewall, TLS/443, and neighboring services in routine P4K operation. | operator/final guides | Cross-check commands against the tracked deploy script and Phase 41 evidence. |
| SignalR reconnect UX | [Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) | Reconnecting transport state does not itself restore application membership or authoritative game state. | The user guide requires reconnect followed by explicit `Resume Current Match`, snapshot, and action-log refresh. | user/final guides | Use the Phase 23 WPF resume result and Phase 43 remote resume regression as evidence. |
| Secret-safe operator evidence | [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html) | Operational logs should retain actionable status without recording credentials or authentication material. | Keep raw reports under ignored `.tmp`, use generated temporary users only, and show aggregate journal checks rather than matching secret-bearing lines. | operator/final guides | `git diff --check`, runner registry parse, and tracked-document review. |
| Workflow conclusion | [GitHub Docs: Monitor workflows](https://docs.github.com/en/actions/how-tos/monitor-workflows) | A pushed documentation closeout is complete only after the resulting workflow reaches a terminal successful conclusion. | Push the Phase 44 commit and wait for its GitHub Actions run before reporting the final CI result. | final report | Record final commit, run ID, conclusion, and clean worktree. |

## P4L Phase 00 - State And Asset Baseline

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Sequential product build | [Microsoft Learn: dotnet build](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build) | `dotnet build` builds a project and dependencies through MSBuild and supports an explicit configuration and project properties. | Build RubikApp, ChessApp, and Chess3DApp separately in Release/x64 so shared native outputs are not built concurrently. | baseline docs only | Record exit code, warnings, errors, and output path for each project. |
| Independent CI evidence | [GitHub Docs: Monitor workflows](https://docs.github.com/en/actions/how-tos/monitor-workflows) | Workflow status and per-job logs are the authoritative remote result after a push. | Preserve the clean P4K baseline, commit only the Phase 00 research/baseline documents, push, and wait for the new Windows Build run. | baseline/research docs | Confirm `HEAD == origin/main`, latest pre-phase CI success, then wait for the Phase 00 run conclusion. |
| Scope isolation | Repository P4K final report and P4L request | Local Rubik, Chess2D, and asset work does not require a server deployment or protocol change. | Freeze Hetzner/network/server state and keep exactly five Chess3D RuleProfiles throughout P4L. | baseline docs | Status/diff review must contain no server deploy artifact or profile addition. |

## P4L Phase 01 - Standalone Rubik State And Visual Audit

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Per-surface WPF materials | [Microsoft Learn: GeometryModel3D](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.media3d.geometrymodel3d?view=netframework-4.8.1) | A `GeometryModel3D` has one front material; multiple independently colored surfaces require multiple geometry models grouped together. | Treat the current one-`GeometryModel3D` cubie as a renderer limitation and plan separate body/sticker geometries instead of trying to encode six colors in one solid material. | audit docs only | Trace `CreateCube` and its material assignment. |
| WPF 3D cost boundary | [Microsoft Learn: Maximize 3D Performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/maximize-wpf-3d-performance) | Each additional material/model has a rendering cost; reusable/frozen meshes and materials reduce overhead. | The later sticker renderer must reuse simple meshes/materials, render exposed stickers only, and preserve current hit-test ownership. | audit docs only | Record performance and hit-test risks before implementation. |
| State/solver separation | Repository native ABI and contract tests | Integer cubie permutation and trusted history can support layer animation/reversal but cannot reconstruct arbitrary sticker orientation. | Preserve integer-cell ABI and reverse-history solver; add future facelet/orientation state append-only as separate authoritative data. | engine/UI/test audit docs | Map create/reset/rotate/set-cells/history/solve flows and no-mutation expectations. |

## P4L Phase 02 - Canonical Rubik Facelet Coordinates

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| NxNxN face notation | [World Cube Association Regulations, Article 12](https://www.worldcubeassociation.org/regulations/full/) | WCA defines U/R/F/D/L/B outer turns as clockwise when the named face is viewed directly, with prime and `2` variants and explicit wide-block notation. | Use U/R/F/D/L/B as the portable face names and define every face grid from an outside observer with row 0 at visual top and column 0 at visual left. | coordinate spec only | Hand-check all face corner mappings and turn direction tables. |
| General NxNxN model | [Demaine et al., On the nxnxn Rubik's Cube](https://arxiv.org/abs/1708.05598) | General NxNxN cubes require explicit treatment of slices and piece classes rather than assumptions tied to 3x3 fixed centers. | Define formulas for every N, including even/odd center distinctions and edge wings. | coordinate spec only | Verify counts sum to `N^3` and stickers to `6*N^2` for N=2,3,4,11,32. |
| Compatibility with current engine | Repository `rotateLayerSquare` and `RubikNotation` audit | Current axis-turn storage formulas are stable, but their X/Z face-token sign is not the same as canonical outside-view WCA clockwise under the chosen world axes. | Freeze internal coordinate-turn formulas separately from portable WCA face turns; do not silently reinterpret legacy history. A later implementation needs explicit conversion tests/versioning. | coordinate spec only | Table-test coordinate turn transforms and face-token conversion before changing parser or ABI. |

## P4L Phase 03 - Rubik File Boundaries

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Forward-compatible JSON | [Microsoft Learn: Handle unmapped members during deserialization](https://learn.microsoft.com/en-gb/dotnet/standard/serialization/system-text-json/missing-members) | `System.Text.Json` skips unmapped properties by default, while .NET 8 can explicitly disallow them. Silent skipping cannot satisfy round-trip preservation by itself. | Require known `format` and major `version`; preserve optional extension data only in an explicit `metadata` object and reject unknown required semantics. | file-format audit docs | Review each proposed document root and its unknown-member policy before adding schemas or serializers. |
| Durable file output | [Microsoft Learn: FileStream.Flush(Boolean)](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush) and [File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace) | `Flush(true)` requests that intermediate buffers reach disk; `File.Replace` can replace an existing file while retaining a backup. | Write a unique temporary sibling, flush it to disk, then replace/rename on the same volume; retain the previous destination if any step fails. | file-format audit docs | Later file-service tests inject write/replace failures and assert the previous file remains readable. |
| State versus provenance | Repository `Rubik_SetCells`, `Rubik_GetHistory`, `Rubik_SolveByReverseHistory`, and `RubikNotation` audit | Current integer import clears history and marks the state manual; reverse-history solving is valid only for a trusted move chain. | Make facelets the authority in `.rubik.json`, moves the authority in `.rubikmoves`, and trusted history plus UI data the concern of `.rubiksession.json`. Never infer solvability merely because a history array is present. | file-format audit docs | Cross-check format responsibilities against existing manual-state and notation behavior. |

## P4L Phase 04 - Append-only Rubik Facelet ABI

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Native ABI stability | [Microsoft Learn: Exporting C++ functions for C-language executables](https://learn.microsoft.com/en-us/cpp/build/exporting-cpp-functions-for-use-in-c-language-executables) | `extern "C"` preserves a callable C linkage boundary instead of C++ name decoration. | Append new exported C functions after the existing declarations; do not resize `RubikStateDto` or change existing signatures. | Rubik native header/engine | Build native DLL and call every new export from contract tests. |
| Managed array interop | [Microsoft Learn: Native interoperability best practices](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/best-practices) | P/Invoke signatures must match native types, and array direction should be explicit with `[In]`/`[Out]`. | Use compact `int` color IDs and explicit count/capacity arguments with matching managed declarations. | RubikApp native wrapper | Build RubikApp and exercise native arrays in C++ contract tests. |
| Legacy-state honesty | Repository integer-cell ABI and Phase 01 audit | A legacy cubie-ID import or rotation cannot reconstruct sticker orientation. | Preserve old operations, but reject facelet reads while sticker state is unsynchronized rather than return invented colors; Phase 05 supplies rotation synchronization. | Rubik engine/docs/tests | Assert solved states read correctly and a legacy-only turn reports unavailable facelets. |

## P4L Phase 05 - Rubik Facelet Rotation

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Portable turn semantics | [World Cube Association Regulations, Article 12](https://www.worldcubeassociation.org/regulations/full/) | Face, inner-slice, wide, and whole-cube tokens all describe rigid layer rotations; outer stickers rotate with their cubies rather than through independent color-strip rules. | Represent every facelet as a cubie coordinate plus outward unit normal, rotate both with the engine's existing discrete transform, then map back to face/row/column. | Rubik engine/tests/docs | Exact identity tests for turn/inverse, four quarter turns, and two half turns on multiple N. |
| General NxNxN slices | [Demaine et al., On the nxnxn Rubik's Cube](https://arxiv.org/abs/1708.05598) | Arbitrary N requires uniform slice treatment; special-casing only six outer faces does not cover wings, centers, or inner layers. | Use one coordinate algorithm for outer, inner, wide, and all-layer rotations for N=2..32. | Rubik engine/tests/docs | Exercise outer, inner, wide, and whole-cube sequences for N=2,3,4,5,8,11. |
| Transactional consistency | Repository `rotateLayerSquare`, facelet ABI, and reverse-history tests | Cubie permutation and facelet permutation must commit as one logical turn, preserving color counts and history behavior. | Compute facelets into a temporary vector before replacing storage; keep legacy unsynchronized state explicit and never synthesize missing orientation. | Rubik engine/tests | Compare facelets and cubie IDs before/after inverse cycles, validate color counts, and replay reverse history to solved state. |

## P4L Phase 06 - Discrete Cubie Orientation

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Physical piece identity | [Demaine et al., On the nxnxn Rubik's Cube](https://arxiv.org/abs/1708.05598) and Phase 02 piece classification | Corners, wings/edges, centers, and internals are distinguished by which solved boundary faces they touch; their sticker count is 3, 2, 1, or 0. | Derive a stable local sticker mask from each cubie ID's solved coordinate, independent of its current position. | Rubik native engine/tests/docs | Assert representative corner, edge, center, and internal masks. |
| Exact orientation | Repository integer layer transform and facelet normal transform | Every legal turn maps axis-aligned local basis vectors to signed world axes; floating-point quaternions would add unnecessary drift and ABI ambiguity. | Store three signed integer basis vectors per current cubie position and rotate them with the same discrete normal transform. | Rubik native engine/tests/docs | Verify exact basis after one turn and identity after four turns for X/Y/Z. |
| Append-only diagnostics | Existing C ABI/PInvoke boundary | Resizing `RubikStateDto` would break callers that allocate the old struct. | Add a new packed orientation DTO and two new exports; leave every prior function and struct byte-for-byte unchanged. | native header, managed wrapper | Build DLL/app, inspect exports, and call the APIs from native contracts. |

## P4L Phase 07 - Multi-color Rubik Rendering

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Per-surface materials | [Microsoft Learn: GeometryModel3D](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.media3d.geometrymodel3d?view=netframework-4.8.1) | One `GeometryModel3D` has one front material, so independently colored faces require separate geometry models. | Render each cubie as one dark plastic body plus separate, slightly offset sticker quads for exposed physical faces. | RubikApp renderer/docs | Build app and inspect solved/scrambled corner, edge, and center counts. |
| Scene cost | [Microsoft Learn: Maximize 3D Performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/maximize-wpf-3d-performance) | Material/model count affects WPF 3D performance; frozen resources and surface-only rendering reduce cost. | Keep surface-only as default, use simple two-triangle stickers, freeze brushes/materials, and avoid internal stickers. | RubikApp renderer/docs | Report rendered cubie/sticker counts and verify N=11 remains bounded to exposed surfaces. |
| Interaction ownership | Repository hit-test and layer animation audit | Hit testing currently maps the hit `GeometryModel3D` to one `CubeVisual`, while animation transforms one model per cubie. | Make the cubie a `Model3DGroup`, map body and every sticker child to the same logical visual, and animate the group once. | RubikApp renderer | Build and preserve selection/drag code paths without engine or input-contract changes. |
| Physical sticker orientation | Phase 06 append-only sticker-mask and integer-orientation ABI | Current boundary coordinates describe where a cubie is, not which physical stickers it owns; the exact basis maps each owned local face to one world normal. | Use the identity mask for sticker count, transform local normals through the basis, and read color from the matching authoritative world facelet. Use shell rendering only when imported facelets have no proven decomposition. | RubikApp renderer/docs | Build native/app, run Rubik contracts, and verify fallback status remains explicit. |

## P4L Phase 08 - Deterministic Visual Contracts

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Headless visual authority | Repository native facelet, sticker-mask, and orientation ABI | Renderer correctness can be described entirely by integer coordinates, masks, normals, and color IDs; no WPF window is required to test those invariants. | Extract a `net8.0` descriptor builder and make both RubikApp and a native-backed console contract test consume it. | RubikVisuals, RubikApp, RubikVisualContractTests | Check exact topology/color/normal invariants on N=2/3/8/11 and after all supported turn classes. |
| Deterministic fixtures | [Microsoft Learn: System.Text.Json overview](https://learn.microsoft.com/en-gb/dotnet/standard/serialization/system-text-json/overview) | Built-in UTF-8 JSON support is sufficient for small, stable, data-only fixtures without UI or polymorphic loading. | Keep a compact solved 3x3 representative fixture and parse it with `JsonDocument`; screenshots are supplementary evidence only. | RubikVisualContractTests fixture/docs | Parse fixture and compare representative corner, edge, and center descriptors. |
| WPF performance boundary | [Microsoft Learn: Maximize 3D Performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/maximize-wpf-3d-performance) | Reusing/freezeable geometry and materials reduces WPF 3D overhead, while descriptor generation can remain independent of rendering resources. | Keep Phase 08 data-only and defer resource caches/measurements to Phase 10. | RubikVisuals/docs | Ensure the builder has no WPF reference and app behavior remains unchanged. |

## P4L Phase 09 - Reproducible Visual Evidence

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| In-process WPF capture | [Microsoft Learn: RenderTargetBitmap.Render](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.imaging.rendertargetbitmap.render?view=windowsdesktop-9.0), [Encode a Visual to an Image File](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/how-to-encode-a-visual-to-an-image-file) | WPF can render its own `Visual` directly into a bitmap and encode it as PNG without foreground-window or desktop capture APIs. | Capture the named viewport host after layout, save five deterministic scenes, and keep all files under ignored `.tmp/rubik-visual-evidence`. | RubikApp/docs | Run the app with `--save-visual-evidence`, inspect exit code/manifest/files, and view representative PNGs. |
| Render synchronization | [Microsoft Learn: DispatcherPriority](https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcherpriority?view=netframework-4.8) | `DispatcherPriority.Render` schedules work at rendering priority after pending layout work. | Update layout and await a render-priority dispatcher turn before each capture. | RubikApp | Confirm every PNG has nonzero dimensions and content. |
| State restoration | Existing trusted-history contract | A non-manual state can be reconstructed from reset plus its complete native history; a manual integer state cannot restore physical orientation. | Preserve/replay trusted history after evidence generation and reject manual states without mutation. | RubikApp | Compare restored size, facelets, and history count. |

## P4L Phase 10 - 11x11 Rendering Performance

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| WPF 3D resource reuse | [Microsoft Learn: Maximize 3D Performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/maximize-wpf-3d-performance), [Freezable Objects Overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/freezable-objects-overview) | Meshes, brushes, and materials are multiparentable `Freezable` resources; freezing and reusing them reduces change tracking and working set. | Use one frozen unit body mesh, six frozen unit sticker meshes, and a bounded cache of frozen materials. Keep per-cubie transforms lightweight and retain one logical animation group. | RubikApp renderer | Build/capture, verify hit-map ownership, run Rubik contracts, and compare measured N=11 rebuild/allocation counts. |
| Performance evidence | Repository in-process capture and animation pipeline | Subjective interaction is insufficient; rebuild time, allocation, memory, model count, and rendered frame count can be recorded inside the UI process. | Add an opt-in `--measure-render-performance` probe that writes only an ignored JSON report and restores trusted state. | RubikApp/docs | Run through TestProcessWatchdog and record repeated N=11 surface/full refresh plus one animated layer turn. |

## P4L Phase 11 - Portable Rubik State Contract

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Versioned JSON schema | [JSON Schema Draft 2020-12](https://json-schema.org/draft/2020-12), [Microsoft Learn: unmapped JSON members](https://learn.microsoft.com/en-gb/dotnet/standard/serialization/system-text-json/missing-members) | Draft 2020-12 can close the root object, while .NET 8 can reject unmapped POCO members. Dynamic `N*N` array length still requires runtime validation. | Define a strict v1 root, fixed U/R/F/D/L/B keys, bounded face arrays, and an explicit extensible `metadata` object. | assets/rules/rubik, docs | Parse schema/examples; runtime Phase 12 checks size-dependent lengths and counts. |
| Canonical fingerprint | Repository facelet order and SHA-256 usage | A stable diagnostic hash must exclude formatting, timestamps, UI state, paths, and untrusted history. | Hash an ASCII v1 header containing size/color scheme followed by normalized numeric facelets in U/R/F/D/L/B row-major order. | docs, upcoming RubikStateHasher | Verify whitespace/metadata independence and state sensitivity. |

## P4L Phase 12 - Rubik State Serialization

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Strict JSON input | [Microsoft Learn: System.Text.Json unmapped members](https://learn.microsoft.com/en-gb/dotnet/standard/serialization/system-text-json/missing-members) and repository v1 schema | POCO deserialization can reject unknown members, but duplicate-member and mixed color-token handling need an explicit document pass. | Parse bounded UTF-8 with `JsonDocument`, recursively reject duplicate properties, enforce an exact root/face contract, and normalize names/IDs before producing a document. | RubikState, contract tests | Cover truncation, duplicates, unknown/missing/extra members, invalid values, unsupported versions, and oversized input. |
| Deterministic hashing | [Microsoft Learn: SHA256.HashData](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256.hashdata) and Phase 11 canonical input | Hashing normalized ASCII material avoids JSON whitespace/property-order instability. | Keep hashing independent of serialization and metadata; emit lowercase SHA-256 and verify supplied non-empty hashes before returning a load plan. | RubikState | Roundtrip N=2/3/4/8/11/32 and compare formatting/metadata-independent hashes. |
| Transaction boundary | Existing transactional `Rubik_SetFacelets` contract and Phase 11 state model | Parsing directly into the live native handle would expose partial-load risk and mix file concerns with engine ownership. | Produce an immutable validated load plan first; UI Phase 14 will populate a temporary native cube and swap only after native acceptance. | RubikState/docs | Invalid inputs never invoke an apply callback; valid plans preserve exact facelet order. |

## P4L Phase 13 - Atomic Rubik File Service

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Durable temporary write | [Microsoft Learn: FileStream.Flush(Boolean)](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush) | `Flush(true)` pushes intermediate file buffers to disk; ordinary disposal alone does not express the same durability intent. | Write a unique sibling temp with `CreateNew`, flush managed and OS buffers, then re-read through the strict parser before commit. | RubikState file service/tests | Inject failure after create/write/flush/validation and verify destination bytes remain unchanged. |
| Same-volume replacement | [Microsoft Learn: File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace) and [File.Move](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.move) | Replacement is a filesystem operation; keeping temp beside destination avoids cross-volume rename behavior. | Use `File.Replace` for an existing destination (optional `.bak`) and same-directory `File.Move` for first creation; never delete destination first. | RubikState file service | Verify successful replace, retained backup, parseable output, and no orphan temps. |
| Bounded shared read | Repository serializer byte limit | Checking only JSON after an unbounded `ReadAllBytes` still permits avoidable memory growth. | Open with read sharing, reject length above limit, then read at most the bounded payload before parsing. | RubikState file service/tests | Oversized files return `InputTooLarge`; malformed and hash errors retain parser categories. |

## P4L Phase 14 - Rubik State File UI

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Standard desktop file flow | [Microsoft Learn: OpenFileDialog](https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.openfiledialog) and [SaveFileDialog](https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.savefiledialog) | WPF can use platform dialogs with filters, extension defaults, and overwrite prompting without introducing another UI framework. | Add a compact File command group for state/move files and keep legacy text import explicitly labelled debug-only. | RubikApp XAML/code/docs | Build x64 WPF app; verify event bindings and file filters. |
| Transactional native ownership | Existing disposable `NativeRubikEngine` wrapper and Phase 12 load plan | Applying a valid document directly to the live handle still couples native acceptance to current state. | Create a candidate handle, set size/facelets, then swap `_engine` only after success; dispose the old handle after commit. | RubikApp | Invalid file leaves current hash/handle unchanged; loaded state refreshes only after swap. |
| Dirty/hash status | Canonical state hash contract | Move history and file timestamps are not reliable dirty markers; physical facelets are. | Compare current canonical physical hash with the last saved/loaded hash, display filename/hash/validation, and retain recent paths in memory only. | RubikApp | Save clears dirty, a turn changes hash, load establishes a new clean baseline. |

## P4L Phase 15 - 11x11 Save/Load Regression

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Physical roundtrip authority | Repository facelet ABI, state hash, atomic service, and visual descriptor fallback | A portable load can preserve visible physical facelets without claiming recovery of native cubie identity/orientation or trusted move history. | Compare authoritative facelets/hash and a world-face/color shell signature; require imported state to report manual/untrusted history and unavailable decomposition. | RubikState contract tests/docs | N=11 solved, scramble, inner, wide, and whole-cube scenarios save/read/apply and compare exactly. |
| Renderer continuity | Phase 08 descriptor builder fallback contract | Facelet-only imports intentionally render from world shell coordinates when cubie orientation is unavailable. | Assert 726 stickers, no invalid descriptors, fallback active, and equal world-face/color signatures before/after file roundtrip. | RubikState + RubikVisuals tests | Run managed Rubik suite alongside existing oriented visual contracts. |

## P4L Phase 16 - Physical Face Editor

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Draft isolation | [Microsoft Learn: WPF input overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/input-overview) and repository transactional load boundary | Mouse painting can generate many intermediate states; none should reach the native cube before explicit acceptance. | Keep six `N*N` managed matrices with bounded undo/redo; paint/drag/fill/rotate/clear/copy/paste operate only on the draft. | RubikState, RubikApp | Headless model contracts plus x64 WPF build/startup probe. |
| Odd/even orientation | WCA color-scheme conventions and physical cube topology | Odd cubes expose one center per face; even cubes have no fixed single center and cannot infer orientation the same way. | Offer odd-center scheme guidance only when six centers are complete/distinct; require explicit U/R/F/D/L/B labels for even N and never silently reorient. | editor UI/docs | N=11 center inference and N=4 explicit-orientation guidance tests. |
| Draft persistence | Phase 13 same-volume temp/replace policy | Incomplete drafts cannot satisfy `.rubik.json` physical-state counts but still need safe local persistence. | Define a separate bounded `rubik.editor-draft` JSON with IDs `0..6`; atomically replace only after the temp draft parses. | RubikState/editor/tests | Save/load an incomplete N=11 draft and compare every cell. |

## P4L Phase 17 - Structured Validation Diagnostics

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Actionable validation | Repository editor requirements and Phase 12 parser error paths | Aggregate count text cannot identify a cell, while emitting every empty cell on N=32 would overwhelm the UI. | Add severity/code/face/row/column/cubie-class/message/action issues, bounded cell detail, and complete aggregate underflow/overflow summaries. | RubikState/editor/tests | Assert exact first missing-cell location, stable reason codes, and valid solved report. |
| WPF issue navigation | [Microsoft Learn: ListBox](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/listbox) and WPF focus/input model | A selected validation item can drive the existing tab/grid controls without mutating the draft. | Bind a compact issue list; selection switches face tab and focuses the addressed cell. | RubikApp | Build/start UI and headless-test diagnostic coordinates. |
| Sanitized report | OWASP logging guidance and repository no-secret policy | Validation support data needs reason/location, not full facelet payload or local path. | Export only size, summary counts, and structured issues as UTF-8 JSON. | RubikApp/docs | Search report model for payload/path fields and parse test JSON. |

## P4L Phase 18 - Facelet-to-Cubie Decomposition

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Observable physical pieces | Repository U/R/F/D/L/B coordinate mapping and NxN topology | A surface coordinate exposes three, two, or one world faces and therefore describes a corner, wing, or center observation directly from facelets. | Enumerate boundary coordinates and read colors with the renderer's canonical mapping; classify without native cubie IDs. | RubikState/tests/docs | Solved N=2/3/4/5/8/11 and legal native scrambles match exact topology. |
| Duplicate NxN wings | [Demaine et al., On the nxnxn Rubik's Cube](https://arxiv.org/abs/1708.05598) | Multiple wings share a color pair; pair alone is not a stable physical identity. | Store current coordinate, free-axis index, and reflection-invariant orbit; compare inventory as pair+orbit multisets without inventing identity. | RubikState | Assert wing counts/orbits and reject missing/impossible pair inventory. |
| Center orbits | NxN legal slice geometry | Center stickers of one color occupy multiple rotation-invariant distance orbits on larger cubes. | Classify centers by sorted distances to face edges and compare color+orbit inventory against solved topology. | RubikState | Detect a count-preserving cross-orbit center corruption. |

## P4L Phase 19 - Solvability and Parity Validation

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Small-cube invariants | Standard cubie orientation/permutation invariants and repository legal turn generator | 3x3 requires corner twist sum, edge flip sum, and equal corner/edge permutation parity; 2x2 has corner orientation constraints but no independent edge parity match. | Implement a full declared small-cube validation kernel for N=2/3 and test impossible single-twist/flip/swap fixtures. | RubikState/tests | Legal scrambles pass; twisted corner, flipped edge, and swapped corners fail the expected invariant. |
| NxN proof boundary | Phase 18 duplicate wing/center observations | Facelets identify pair/orbit inventory but do not label interchangeable wings/centers strongly enough to prove full arbitrary-N permutation parity. | Report known-valid inventory/orientation separately from `parityProven`; keep `solverReady=false` and validation level partial for N>3. | RubikState/docs/UI status | Legal 4/5/8/11 states remain accepted as partial, never advertised fully solver-ready. |

## P4L Phase 20 - Physical 11x11 Workflow

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| End-to-end physical input | Existing editor draft, structured diagnostics, portable state, native import, visual descriptors, and atomic file APIs | Every boundary needed for physical 11x11 input already exists, but no single contract proves the complete user journey. | Add one headless workflow covering draft fill through save/reset/load/hash and an independently native-generated legal scramble copy. | RubikState contract tests/docs | Assert exact 726 stickers, exact facelets/hash after reload, and clean atomic copies. |
| Honest solver status | Phase 19 NxN validation boundary | Inventory-valid 11x11 input is not equivalent to a full arbitrary-state solvability proof. | Surface `CubieInventory`, `orientationProven=false`, `parityProven=false`, and `solverReady=false` as the expected successful input status. | Tests/docs | Both solved and legal-scramble workflows must report the same honest proof boundary. |

## P4L Phase 21 - Arbitrary Solver Architecture

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| 3x3 two-phase search | [Kociemba two-phase description](https://kociemba.org/math/twophase.htm), [reference implementation](https://github.com/hkociemba/RubiksCube-TwophaseSolver) | Two-phase search is practical but depends on substantial coordinate/pruning tables; the inspected reference implementation is GPL-3.0 and reports about 80 MB of generated tables. | Do not copy or embed that implementation. Keep a contract boundary for a future independently implemented or explicitly licensed backend. | Solver architecture docs | Contracts expose time/memory/cancellation limits and never imply a backend is installed. |
| Arbitrary NxN | [Demaine et al., Algorithms for Solving Rubik's Cubes](https://arxiv.org/abs/1106.5736), [Demaine et al., optimal solving complexity](https://arxiv.org/abs/1706.06708) | General-N solving has structured algorithms, while optimal arbitrary-N solving is computationally hard; product-grade reduction also needs centers, edge pairing, and parity handling. | Use an explicit reduction state machine with checkpoints; no optimal 11x11 claim. | Solver architecture docs | Later phases must report achieved reduction level and independently replay every returned move. |
| Backend ownership | Existing native reverse-history ABI and current repository boundaries | Reverse history is useful only for trusted in-session histories and is not an arbitrary-state solver. External tools isolate licensing/runtime risk but require strict input/output and timeout handling. | Ship reverse-history as a distinctly named implementation, add an owned bounded 2x2 kernel, and allow opt-in plugins/process adapters later. | Solver contracts/docs | Capability tests must distinguish trusted history, arbitrary small cube, and NxN reduction support. |

## P4L Phase 22 - Solver Contracts

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Portable solver boundary | Existing `RubikStateDocument`, state hash, native move DTO, and reverse-history ABI | A solver request must carry a validated immutable state plus explicit resource/cancellation bounds; native DTO layout must not leak into managed backends. | Add pure `net8.0` solver/move/progress/result contracts in RubikState with no native or WPF dependency. | RubikState/tests | Build on all current targets and test validation, cancellation, and capability fields. |
| Reverse history naming | Existing `Rubik_SolveByReverseHistory` behavior | Inverting trusted history is deterministic and useful, but cannot solve imported/manual arbitrary states. | Implement `ReverseHistorySolver` with `SupportsArbitraryState=false` and `RequiresTrustedHistory=true`; reject a missing trusted history. | RubikState/tests/docs | Assert exact inverse order and typed unsupported-state failure. |
| Verification hand-off | Phase 21 independent replay decision | A returned move list is not proof; Phase 22 has no independent move executor yet. | Result carries an explicit `NotRun` verification status and nullable final hash. Phase 23 supplies replay authority. | RubikState | Tests ensure reverse-history output never reports itself verified. |

## P4L Phase 23 - Solution Verification Authority

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Independent execution | Existing native create/set-facelets/rotate/get-facelets APIs and portable state validator | A fresh cube handle can replay solver output without sharing mutable solver state or trusted history. | Define a small executor factory boundary; verifier clones canonical facelets, validates each structured move and each intermediate state, then checks canonical solved facelets. | RubikState/tests | Native-backed tests cover valid, malformed, illegal, truncated, incorrect, and cancelled sequences. |
| Proof result | Existing canonical state hash contract | Applying all moves successfully is insufficient; solved state and final hash must both be computed from replay output. | Return `Verified` only for canonical solved output; return `Failed` with applied count/final hash for a legal but incomplete sequence. | RubikState/tests/docs | Reverse solution reaches solved hash; truncated/incorrect solutions fail. |
| Input isolation | Portable document arrays are mutable references | A verifier could accidentally alter caller-owned facelets while cloning or adapting. | Snapshot input before execution and test byte-for-byte facelet equality after every verification path. | Tests | Input mutation assertion runs after successful replay. |

## P4L Phase 24 - Bounded Arbitrary Small-Cube Solver

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Managed move model | Native `faceletToSticker`, layer-square rotation, normal rotation, and `stickerToFacelet` implementation | The native move semantics can be reproduced from geometry without copying solver logic or changing ABI. | Add a pure managed facelet simulator and prove all 18 outer moves against fresh native 2x2 and 3x3 handles. | RubikState/tests | Exact facelet equality for every axis/layer/quarter-turn combination. |
| Bounded 2x2 search | Phase 21 architecture and full N=2 solvability validation | Iterative deepening can solve arbitrary validated 2x2 states within explicit depth/time/node bounds without large precomputed tables. | Implement deterministic IDDFS with inverse/commuting move pruning; derive node cap conservatively from memory limit. | RubikState/tests | Known and seeded legal short scrambles solve and native replay-verify; impossible state fails validation; cancellation is typed. |
| 3x3 boundary | Kociemba two-phase research and Phase 21 licensing/table audit | A credible arbitrary 3x3 backend requires coordinate/pruning tables and an explicit implementation/license decision. | Return `UnsupportedSize` for 3x3 from this backend; do not label bounded 2x2 support as general small-cube completion. | RubikState/docs | Capability maximum remains 2 and a 3x3 request clean-fails. |

## P4L Phase 25 - NxN Reduction Framework

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Reduction state machine | Phase 21 architecture and Phase 18 corner/wing/center decomposition | Center solving, wing pairing, reduced-3x3 construction, parity, and final verification have distinct invariants and cannot be represented as one opaque search. | Add explicit ordered phase descriptors and a state/checkpoint model; generate guidance only, not fabricated moves. | RubikState/tests/docs | 4x4/5x5/7x7 produce deterministic phase plans with zero emitted moves and `Incomplete` status. |
| Checkpoint safety | Existing state hash/checkpoint contract | Resume is safe only when solver id, schema version, size, and input hash all match. | Add strict bounded JSON checkpoint parsing and mismatch validation before resume. | RubikState/tests | Deterministic roundtrip, wrong hash, wrong size, malformed JSON, and cancellation tests. |
| Progress/log bounds | Existing solver request resource contract | A long NxN phase can produce unbounded diagnostic output even before move algorithms exist. | Cap checkpoint log entries and messages; progress names the exact phase and never reports false completion. | RubikState | Tests verify bounded log and terminal `Incomplete` guidance status. |

## P4L Phase 26 - 11x11 Reduction Milestone

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Milestone level | Requested Level A/B/C acceptance criteria and Phase 25 framework | Current code can prove load, inventory/decomposition, phase guidance, and checkpointing, but cannot solve centers or pair wings. | Declare Level A only; do not use reverse history or a short generated scramble as arbitrary-solve evidence. | RubikState/tests/docs | Generated and imported N=11 states reach the same Level A plan with zero emitted moves. |
| Move artifact honesty | Existing portable state format and independent verifier | A partial reduction artifact must be distinguishable from a complete, replay-verified solution. | Add versioned `.rubikmoves` JSON with explicit `complete`, `verified`, and nullable final hash; enforce verified implies complete+final hash. | RubikState/tests | Atomic save/load preserves input hash and empty Level A move list; invalid status combinations fail. |
| Resume artifact | Phase 25 checkpoint schema | In-memory checkpoint support is insufficient for a user workflow. | Add atomic checkpoint file save/load with bounded reads and input hash validation. | RubikState/tests | Saved N=11 checkpoint resumes only against its exact imported state. |

## P4L Phase 27 - Rubik Solver Workflow UI

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Background solver work | [Microsoft Learn: WPF threading model](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model) and existing `IRubikSolver` async/cancellation contracts | Long-running work must stay off the UI thread; the Dispatcher should only publish short status updates. | Run the bounded 2x2 backend asynchronously with cooperative cancellation and throttled `IProgress`; cancel on window close. | RubikApp | Build x64 WPF app and run the managed Rubik contracts. |
| Capability-aware controls | Existing solver capabilities, 2x2 backend, and NxN reduction framework | Pause/resume is not implemented by current backends, while NxN currently produces guidance/checkpoints rather than moves. | Keep Pause/Resume visible but disabled, identify 2x2 as arbitrary/verified, 3x3 as deferred, and N>=4 as incomplete reduction guidance. | RubikApp/docs | Inspect status fields for each size boundary; no UI label may claim an arbitrary 3x3 or 11x11 solution. |
| Independent verification | Existing `RubikSolutionVerifier` executor boundary | Solver output is only a candidate until replayed from the exact input on a fresh native engine. | Add a small RubikApp-native executor factory, verify before enabling solution playback/save, and persist `.rubikmoves` only with explicit complete/verified/final hash fields. | RubikApp | 2x2 workflow uses fresh native replay; loaded move files must match the current input hash. |
| Playback state | Existing animated native layer-turn path | Previous Step cannot depend on mutable engine history because imported files intentionally have untrusted history. | Track the solution input hash and cursor; Step applies one move, Previous applies its inverse, and Play reuses the same animated commit path. | RubikApp | WPF build catches event bindings; Rubik regression keeps turn/replay semantics green. |

## P4L Phase 28 - Full Rubik Regression

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Test selection authority | Decomposed `tests/run-tests.ps1 -List` registry and [GitHub Actions workflow syntax](https://docs.github.com/actions/writing-workflows/workflow-syntax-for-github-actions) | The repository runner, not a hand-maintained executable list, is the current source of suite membership and timeout policy. | Enumerate first, run the exact `Rubik` suite with controlled `/m:1`, and record every selected executable and watchdog outcome. | Test/result docs | `-List`, `-Suite Rubik`, no timeout, exit 0. |
| Mixed native/WPF build | Visual Studio MSBuild project graph and existing RubikEngine/RubikApp project files | `dotnet build` cannot provide C++ `VCTargetsPath`; product verification must use Visual Studio MSBuild for the mixed graph. | Build RubikEngine and RubikApp separately with `/restore /m:1 /nr:false`, preserving the existing Visual Studio path. | Result docs | Both project builds exit 0 with x64 Release output. |
| Regression evidence | Phase 08-27 contracts and artifacts | Rendering, state files, physical input, decomposition, validation, replay verification, bounded 2x2, and Level A 11x11 are distributed across three contract executables. | Record evidence by capability and retain the explicit arbitrary-3x3/NxN limitations. | `P4L_RUBIK_FULL_VERIFY_RESULT.md` | Cross-check suite output against the Phase 28 checklist. |

## P4L Phase 29 - Repository Regression

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Cross-product gate | Decomposed test registry, `scripts/verify.ps1`, and current Windows Build workflow | The Rubik project shares solution, packaging, native build, and test infrastructure with Chess2D, Chess3D, GPU, and Online products. | Run the full contract registry without benchmark under controlled `/m:1`, then run the unchanged full verify/package gate. | Regression report | All selected tests and verify steps exit 0; no watchdog timeout. |
| Profile isolation | Existing Chess3D contract tests and packaged rules assets | Rubik work must not alter the five-profile Chess3D contract or server deployment state. | Treat exactly-five profile checks and absence of deployment edits as explicit acceptance criteria. | Regression report | Verify profile assets/tests and inspect tracked diff/status. |
| Generated artifact hygiene | Repository `.gitignore`, `.tmp` test logs, and production build scripts | Local logs/build/package outputs are evidence but must not become tracked source. | Keep `.tmp`, `bin`, `obj`, `ProductionOutput`, and server runtime material out of the phase commit. | Regression report | `git status --short`, tracked-artifact scan, and diff scope review. |

## P4L Phase 30 - Final Rubik Product Documentation

Date: 2026-07-16

| Topic | Sources checked | Key finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| User workflow | Current Rubik Studio UI, portable state/editor/solver contracts, and Phase 28-29 evidence | Users need one route from physical stickers to validated state and a separate, capability-aware solve route. | Publish concise state-file, physical-input, solver, and launch instructions without presenting deferred backends as available. | User guides/final report | Cross-check every control name and file extension against current XAML/code. |
| Product status language | Existing project map/status/roadmap and Level A acceptance boundary | Summary documents can easily turn partial NxN reduction into an accidental arbitrary-solve claim. | State arbitrary 2x2 as implemented, arbitrary 3x3 as deferred, and 11x11 as rendering/state/validation/reduction Level A only. | README and summary docs | Search final diff for unsupported `11x11 solved` or general-solver claims. |
| Release evidence | Phase 28 local Rubik gate, Phase 29 repository gate, and successful phase CI runs | Final claims should link to reproducible commands/results rather than raw generated logs. | Reference tracked result documents and keep `.tmp` evidence untracked. | Final report | `git diff --check`, targeted Rubik suite, final CI, clean tree. |

## P4M Phase 04 - Chess2D SAN Legal Context

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SAN legal context | [PGN Specification, SAN section](https://www.saremba.de/chessgml/standards/pgn/pgn-complete.htm) and current `generateLegalMoves`/`applyMove` paths | Correct SAN needs pre-move capture/piece facts, legal same-piece alternatives, and post-move check/mate status. The public move DTO alone cannot distinguish mate or expose disambiguation. | Add one append-only `Chess_GetMoveDescriptor` ABI that evaluates an exact legal move on a copied position and returns all SAN context without mutating board/history. | `ChessEngine.h/.cpp`, C# wrapper, native contracts | Descriptor fixtures cover ordinary moves, captures, en passant, promotion, castling, file/rank/both disambiguation, checkmate, invalid input, and FEN/undo no-mutation. |
| ABI stability | Existing packed DTOs and Cdecl P/Invoke declarations | Existing exports and DTO layouts are consumed by ChessApp and contract tests. | Append a new packed DTO and export; leave every existing field/function unchanged and mirror the layout in managed code. | Native header and `NativeChessEngine.cs` | Sequential native test and WPF builds catch C++ layout/export and P/Invoke compile regressions. |

## P4M Phase 05 - Canonical SAN Generator

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Canonical SAN | [PGN Specification, SAN export rules](https://www.saremba.de/chessgml/standards/pgn/pgn-complete.htm) and Phase 03 contract | SAN formatting is deterministic once exact legal context, ambiguity, capture, promotion, and post-move status are known. | Implement a pure `net8.0` generator in a WPF-free `ChessGameRecords` library. Reject inconsistent/illegal contexts instead of emitting fallback notation. | New library and managed contracts | More than 50 formatting fixtures plus invalid-context, determinism, and token-decomposition checks. |
| Test isolation | Decomposed repository test registry and existing console contract pattern | Native loading is unnecessary for formatter edge cases and would make the SAN unit surface slower and platform-specific. | Test the formatter independently; Phase 04 native contracts remain responsible for legal context truth and later integration tests join the two layers. | `tests/run-tests.ps1`, new test project | `-Only ChessGameRecordsContractTests` builds and runs under the C# watchdog. |

## P4M Phase 06 - Structured Chess2D History Integration

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Record authority | Phase 02 record design, Phase 04 descriptor ABI, Phase 05 SAN generator, current `MainWindow` move flow | WPF currently appends long-coordinate strings after live moves and separately removes strings on undo. It cannot prove a pre/post position chain. | Add immutable records and a small history controller in `ChessGameRecords`; create records transactionally from a temporary native engine loaded with pre-move FEN. | Game-record library, ChessApp, contracts | Record-chain, undo/redo branch, reset, SAN/UCI/FEN projection, native build, WPF build, and Chess2D targeted suites. |
| WPF history projection | [Microsoft WPF data binding overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/) and current code-behind architecture | A full MVVM rewrite is unnecessary, but the `ListBox` must stop being storage authority. | Render read-only full-move rows from record snapshots and expose selected-ply SAN/UCI/pre/post FEN controls. | `MainWindow.xaml(.cs)` | WPF compile validates bindings/handlers; managed contracts validate model behavior without UI automation. |

## P4M Phase 07 - SAN Workflow Regression Gate

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| End-to-end SAN history | Existing C# native wrapper, Phase 04 descriptor, Phase 05 generator, and Phase 06 history controller | Native and managed contracts are individually green, but a stage gate must prove their composed FEN/SAN/history behavior. | Add a Windows console integration contract with friend access to the internal wrapper; do not widen production API visibility. | ChessApp assembly metadata, new workflow contracts, test registry | Real DLL tests cover legal preview no-mutation, illegal no-record, three-ply SAN chain, undo/recommit, reset replay, Fool's mate, stalemate, and draw claim. |
| Search semantics | Current `Chess_MakeBestMoveEx` behavior | The current search ABI chooses and commits a best move; there is no separate non-committing search-preview API. | Require every successful AI move to create a record. Treat legal descriptor generation as the existing no-record preview path and document the distinction. | Verification report | Descriptor/GetLegalMoves preserve FEN and zero records; committed AI integration remains covered by WPF flow/build. |

## P4M Phase 08 - PGN Document Model

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| PGN document topology | [Portable Game Notation Specification and Implementation Guide](https://www.saremba.de/chessgml/standards/pgn/pgn-complete.htm), sections 8.1 and 8.2 | The structured game history can supply a main line, but it has no lossless representation for ordered tags, comments, NAGs, or recursive variations. | Add an immutable, WPF-independent PGN document tree. Keep duplicate/ordered tags representable so a later tolerant parser can diagnose them instead of losing input. | `ChessGameRecords`, managed contracts | Verify Seven Tag Roster order, result markers, defensive collection copies, SetUp/FEN and custom tag order, comments, NAG range, and nested RAV. |
| Result authority | PGN sections 8.1.1 and 8.2.6 | PGN repeats the result in the `Result` tag and the final movetext marker; mismatches must be detectable later. | Keep typed `PgnResult` separate from raw ordered tag pairs. Export strictness and import diagnostics remain Phase 09/11 responsibilities. | PGN model | Roundtrip all four termination markers while preserving raw tags. |

## P4M Phase 09 - Deterministic PGN Export

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Export format | [PGN Specification](https://www.saremba.de/chessgml/standards/pgn/pgn-complete.htm), sections 4.3, 8.1.1, 8.2, and 8.2.6 | Structured records contain canonical SAN and complete initial FEN, but no standards-compliant text writer exists. | Add a strict deterministic exporter with canonical roster order, escaped tag strings, move numbering, comments/NAG/RAV, result consistency, and bounded line width. | PGN exporter, contracts, export guide | Exact text assertions, deterministic repeat, escaping, result mismatch, missing roster, standard/nonstandard record adapters. |
| Nonstandard start | PGN `SetUp` and `FEN` supplemental tag rules | A session can begin from any valid six-field FEN, including Black to move at an arbitrary fullmove number. | Carry explicit move side/fullmove in PGN nodes and emit paired `SetUp "1"`/`FEN` for nonstandard starts. | PGN model and exporter | Export a Black-to-move fullmove-23 fixture without deriving side from local ply index. |

## P4M Phase 10 - PGN Tokenizer

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| PGN lexical grammar | [PGN Specification](https://www.saremba.de/chessgml/standards/pgn/pgn-complete.htm), sections 7 and 8 | The exporter produces valid text, but import needs bounded lexical diagnostics before any document or engine mutation. | Implement a single-pass scanner without regular expressions. Emit explicit tokens for tag delimiters/names/strings, integers/periods, SAN symbols, both comment forms, NAG, RAV, results, and EOF. | `PgnTokenizer`, managed contracts | Comprehensive token fixture plus malformed string/comment/NAG, source locations, input bound, token bound, and empty output on failure. |
| Import safety | PGN import-format tolerance and repository transaction boundary | A malformed or hostile file must not create a partial document that later code could mistake for a valid candidate game. | Bound input, tokens, token/comment lengths and return no tokens on the first lexical diagnostic. Preserve 1-based line/column for UI errors. | Tokenizer API | Fail-atomic assertions and deterministic linear scans; parser remains a separate phase. |

## P4M Phase 11 - PGN Parser

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| PGN grammar | [PGN Specification](https://www.saremba.de/chessgml/standards/pgn/pgn-complete.htm), sections 8.1-8.2.6 | Phase 10 supplies bounded tokens, but no semantic document construction or result consistency checks. | Build a pure in-memory parser with tolerant/strict modes, ordered tags, mainline, comments, NAG, bounded nested RAV, and mandatory movetext result marker. | `PgnParser`, managed contracts | Strict exporter roundtrip, tolerant no-roster input, duplicate roster, result mismatch, comments/NAG/RAV, and unterminated variation. |
| Transaction boundary | Current WPF/native game flow | Parsing must never apply SAN to the live engine; legality belongs to Phase 12/13 candidate replay. | Return either a complete immutable `PgnDocument` or one located diagnostic and no document. | Parser API | Every semantic failure asserts `Document == null`; no native dependency is introduced. |

## P4M Phase 12 - SAN to Legal Move Resolution

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| SAN resolution | PGN Specification section 8.2.3 and Phase 04 legal descriptor ABI | Current SAN parser tokens describe generated output, but import needs the inverse mapping against current legal moves. | Parse SAN constraints and filter only engine-supplied legal candidates by piece, destination, capture, disambiguation, promotion, castle, and check/mate. Never infer a coordinate move without exactly one legal match. | `ChessSanResolver`, managed and native workflow contracts | Normal/pawn/capture/en-passant/promotion/castle, ambiguity/disambiguation, mismatch categories, and no-mutation engine integration. |

## P4M Phase 13 - Transactional PGN Import

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Transactional replay | PGN import rules, existing native FEN/legal-move APIs, WPF file dialogs | Parser and resolver are pure, but live UI state must not be used as the replay candidate. | Replay on an isolated `NativeChessEngine`, build a validated immutable history, then load final FEN/history into WPF only after success. | ChessApp importer/UI, history load API, workflow tests | Fool's mate import/result, illegal SAN no live mutation, WPF build, save/export path. |

## P4M Phase 14 - PGN Interoperability Fixtures

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Interoperability corpus | PGN specification examples and repository legal-move contracts | External databases are unnecessary and introduce provenance risk. | Keep a compact authored fixture pack covering special legal moves, outcomes, setup FEN, annotations/RAV, and fail-closed malformed inputs. | `tests/fixtures/pgn`, workflow contracts, result doc | Copy fixtures to test output; run all legal files through native candidate replay and all invalid files through atomic rejection. |

## P4M Phase 15 - Chess2D Session Contract

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Versioned JSON contract | [System.Text.Json overview](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview) and existing structured game records | PGN deliberately omits application presentation and search settings, while ad-hoc UI persistence would leak paths and lose deterministic move state. | Define a closed, versioned `chess2d-session` v1 schema containing structured game, semantic presentation IDs, engine limits, dirty state, and optional recovery metadata. | Session JSON Schema and format guide | Parse the schema in managed contracts in Phase 16 and roundtrip representative documents. |
| Secret and path exclusion | [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html) | Session state does not need credentials or machine-specific absolute paths. | Exclude authentication material by design and permit only bounded semantic theme/model/backend identifiers. | Schema, format guide, validator | Reject rooted/path-shaped identifiers and scan tracked fixtures for credential fields. |

## P4M Phase 16 - Transactional Session Persistence

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Durable replacement | [FileStream.Flush(Boolean)](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush) and [File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace) | Direct overwrite could truncate the only recoverable session after a crash. | Write and flush a sibling `.tmp`, re-read and hash-validate it, then replace atomically with optional `.bak`. | Session serializer/file service | Inject failures before every replacement stage and prove the original remains byte-identical. |
| Deterministic serialization | [System.Text.Json serialization](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to) | Additional PGN tags are dictionary-backed and can otherwise inherit insertion order. | Normalize dictionaries with ordinal sorting, use explicit enum strings and reject unmapped JSON members. | Session document/serializer | Repeated serialization and save/load produce identical SHA-256 diagnostic fingerprints. |

## P4M Phase 17 - Chess2D Session UI

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Session workflow | [WPF dialogs overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/windows/how-to-open-common-system-dialog-box) and current PGN controls | PGN controls already occupy the game tab, but session state needs separate commands and status. | Add compact Save/Save As/Load/Recent controls and a distinct filename/dirty/hash/recovery status line. | `MainWindow.xaml(.cs)` | WPF build plus managed session contracts; no fragile UI automation. |
| Transactional UI apply | Current `NativeChessEngine.SetFen` and `ChessGameHistory.TryLoad` contracts | Applying a deserialized document directly could leave engine and history split on failure. | Validate against candidate engine/history first and retain a rollback snapshot around final apply. | MainWindow session handlers | Invalid file remains rejected by file service; WPF build verifies handler wiring. |

## P4M Phase 18 - Autosave and Recovery

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| User-local recovery | [.NET application data folders](https://learn.microsoft.com/en-us/dotnet/api/system.environment.specialfolder) and existing atomic session writer | Recovery is machine-local and must not enter the repository or overwrite an explicit save. | Store bounded autosaves under LocalApplicationData, debounce accepted changes, and open recovery as an unsaved copy. | Recovery service and WPF lifecycle | Ignore incomplete/corrupt files, bound retention, explicit-newer suppression, accepted/rejected scheduling tests. |
| Recovery choice | [WPF threading model](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model) | Startup scanning and prompts must occur after controls initialize, while writes remain short and deterministic. | Dispatch the startup prompt at application idle; expose restore/discard/retain choices and recovery state in the session status. | MainWindow | WPF compile plus headless recovery service contracts. |

## P4M Phase 19 - UCI Architecture Boundary

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| UCI process boundary | [UCI protocol specification mirror](https://backscattering.de/chess/uci/) and current ChessEngine C ABI | Native search currently commits its best move and has no public cancellation export. WPF is not an appropriate protocol host. | Build a console-only adapter with candidate engines for position/search and add one append-only cancellation export in Phase 22. | UCI architecture document; future console/native files | Subprocess transcripts must prove stdout cleanliness, responsiveness, timeout and process cleanup. |
| Honest options/telemetry | Current search options and stats DTO | Hash size, worker threads, seldepth and multi-move PV are not exposed by the engine. | Do not advertise or synthesize unsupported values; emit only native depth/score/nodes/time and the validated best move. | UCI design | Transcript assertions reject invented option/info lines. |

## P4M Phase 20 - Bounded UCI Command Parser

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Command grammar | [UCI protocol specification mirror](https://backscattering.de/chess/uci/) | Commands are line-oriented and unknown input must not terminate the process. | Parse a bounded 16 KiB/256-token command line into typed commands; keep diagnostics on stderr. | `ChessUci` parser and entrypoint | Build now; Phase 24 exercises malformed/oversized input through an external process. |
| Option honesty | Native search option/telemetry DTOs | Only adapter move overhead and opening-book toggle have real current behavior. | Advertise `MoveOverhead` and `OwnBook`; omit Hash and Threads until native contracts exist. | UCI handshake | Transcript checks expected options and absence of unsupported claims. |

## P4M Phase 21 - Transactional UCI Position

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Position replay | UCI `position` grammar and native FEN/move ABI | `Chess_TryMakeMove` validates coordinates but mutates its handle. | Parse startpos or exactly six FEN fields, replay all coordinate moves on a candidate handle, and commit only its final FEN. | UCI native adapter and position controller | External Phase 24 transcript checks startpos/FEN/promotion and illegal no-partial-commit behavior. |
| Process resilience | UCI protocol error handling | A malformed GUI command must not terminate an engine process. | Route errors to stderr, retain authority FEN, and continue the input loop. | UCI entrypoint | Send malformed then valid `isready` in one subprocess. |

## P4M Phase 22 - Cancellable UCI Search

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Cooperative stop | Current negamax stop checks and [UCI protocol](https://backscattering.de/chess/uci/) | Native search checks a local stop flag but exposed only elapsed-time cancellation. | Add append-only cancel/node-limit exports backed by an atomic flag and existing node checkpoints; retain all DTO layouts. | ChessEngine header/implementation and UCI adapter | Native tests plus subprocess `go infinite`/`stop` bounded completion. |
| Search authority | Existing `MakeBestMoveEx` commits its selected move | Running it on the command-reader handle would mutate the UCI position and block input. | Search on a FEN clone in a worker task; generation IDs suppress stale results and emit at most one bestmove. | UCI search controller | Repeated and interrupted subprocess searches; position remains independently assignable. |

## P4M Phase 23 - UCI Search Telemetry

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Info fields | UCI info grammar and native `ChessSearchInfoDto` | Engine exposes completed depth, nodes, elapsed time and best score, but not seldepth or a full PV. | Emit only real stats, derive NPS from nodes/time, and use the returned legal bestmove as a one-move PV. | UCI native/search adapter and guide | Subprocess parses numeric fields and verifies PV equals bestmove. |
| Mate reporting | Native post-move game state | Score encoding alone does not expose a reliable mate distance. | Emit `score mate 1` only when the committed search-clone result is engine-backed checkmate; otherwise emit native centipawns. | UCI telemetry | Mate-in-one transcript fixture; no claims for longer mate distances. |

## P4M Phase 24 - UCI Subprocess Interoperability

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Process-level evidence | [.NET Process redirected I/O](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardoutput) and repository C# watchdog policy | Direct class tests cannot detect stdout contamination, input-loop blocking, leaked child processes, or DLL packaging errors. | Run a real redirected `ChessUci.exe` with bounded waits and unconditional process-tree cleanup. | New subprocess contract test and test registry | Handshake, positions, search modes, stop, malformed/illegal input, telemetry, quit, and stdout whitelist. |

## P4M Phase 25 - Unified Asset Repository Audit

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Runtime inventory | Tracked model root, v1 catalog, OBJ/MTL loader, project copy items and release/verify scripts | One 25.6 MiB legacy OBJ set serves Chess2D/Chess3D; there are no GLB or textures and Rubik is procedural. | Preserve the working OBJ path while building a manifest-authoritative v2 layer and explicit procedural fallback. | Audit document | Re-run extension/size inventory and packaging assertions at Phase 40. |
| Provenance/private paths | Enabled-set README and every MTL/manifest | License evidence is incomplete and white MTLs contain absolute source texture paths. | Mark the set legacy/pending provenance, sanitize private paths before QA, and never infer rights from exporter metadata. | Audit and later manifest/QA files | Validator rejects absolute paths; final tracked scan contains none in runtime MTL. |

## P4M Phase 26 - Source and Runtime Asset Layout

Date: 2026-07-19

| Topic | Primary sources checked | Current repository finding | Decision | Files affected | Verification plan |
| --- | --- | --- | --- | --- | --- |
| Authoring/runtime separation | Existing project copy items and release packaging | The current tree had only a runtime piece root, so source archives could be copied accidentally if placed beside it. | Establish explicit `assets-source/models`, `assets/models`, and disposable `.tmp/assets-import` boundaries; runtime remains manifest-authoritative. | Layout marker files and layout guide | Confirm source files are absent from application and ProductionOutput copy inputs. |
| Profile isolation | Five profile catalog and current visual fallbacks | Special visuals need namespaces but must never create or enable rules. | Give Asgard, Rubik Convergence, and Hodge separate visual namespaces with mandatory existing fallbacks. | Runtime/source namespace markers | Profile count stays five; no rule JSON changes. |
| Local inbox safety | `.gitignore`, `scripts/verify.ps1` rude-resource probe, and current asset copy items | `rude-resource/` is already ignored and excluded from all package inputs. | Create the raw drop hierarchy there through an idempotent initializer; never place marker files inside the ignored tree. | Inbox initializer and source policy | Run initializer twice, use `git check-ignore`, and confirm `git status` shows no inbox content. |

## P4M Phase 27 - Model Asset Manifest v2

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| Closed JSON contract | [System.Text.Json unmapped-member handling](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members) and JSON Schema 2020-12 vocabulary | The v1 catalog is permissive and directory-oriented, but remains a working compatibility input. | Add a strict v2 managed model plus closed schema and exact major version; keep v1 through an explicit adapter. | Rewriting the legacy catalog in place would combine format migration with runtime behavior change. | `src/ModelAssets`, schema, contracts | Unknown members, versions, paths, SHA values, roles and duplicates are rejected. |
| Semantic role registry | Current Chess2D/Chess3D/Rubik renderers and five profile visual capabilities | Piece filenames cannot describe profile-specific optional visuals consistently. | Use stable semantic roles independent of source format and keep visual roles separate from rule profiles. | File-name heuristics remain only inside the v1 adapter. | Role registry and adapter | Adapter yields 14 hashed legacy assets; rule profile count remains five. |

## P4M Phase 28 - Large Model Asset Storage

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| Git/LFS boundary | [GitHub LFS](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-git-large-file-storage), [repository limits](https://docs.github.com/en/repositories/creating-and-managing-repositories/repository-limits), [billing](https://docs.github.com/en/billing/using-the-new-billing-platform/about-billing-for-git-large-file-storage), and [archive behavior](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/managing-repository-settings/managing-git-lfs-objects-in-archives-of-your-repository) | Runtime models total 25.6 MiB; largest legacy OBJ is 2.60 MiB. Git LFS is installed locally but no model pattern uses it. | Keep current stable assets in normal Git; make 5 MiB a review trigger and require explicit approval before LFS. | Automatic extension-wide LFS would make clones/archives/CI depend on LFS before any approved large asset exists. | Storage policy only | `.gitattributes` remains unchanged; inventory and tracked raw-archive scans are recorded. |

## P4M Phase 29 - Offline Model Import

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| Transactional local import | .NET `Path.GetFullPath`, `FileInfo.Attributes`, SHA-256 APIs, and the Phase 26 repository boundary | Raw assets are deliberately ignored and cannot be made runtime-ready by a blind copy. | Require local file, license and provenance; hash an isolated copy and promote a v2 draft only after bounded checks. | URL import, implicit archive extraction, source overwrite, and implicit FBX conversion expand the trust boundary. | Import script, contract script, operator note | OBJ dry run, GLB promotion, SHA, missing license, extension, traversal, duplicate, URL, and cleanup checks. |

## P4M Phase 30 - Blender Conversion Adapter

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| Optional authoring conversion | [Blender 4.2 glTF manual](https://docs.blender.org/manual/en/4.2/addons/import_export/scene_gltf2.html) and Blender background scripting behavior | Blender is not installed locally; runtime must not depend on it. | Provide an explicit offline adapter with factory/background mode, disabled autoexec, bounded process execution, deterministic normalization and report. | Installing Blender automatically or invoking files with auto-run scripts would cross the requested trust boundary. | PowerShell adapter, Blender Python script, operator note | Missing-Blender dry run must return `SKIPPED`; Python syntax compiles with the bundled Python parser when available. |

## P4M Phase 31 - Asset Validation Service

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| Mandatory internal validation | Khronos glTF 2.0 container rules, Khronos glTF Validator CLI model, and .NET bounded file/process APIs | Import currently proves copies and hashes but cannot establish usable mesh topology. | Add a managed package validator with strict manifest, containment, hashes, size/license/role gates and bounded OBJ/GLB inspection. | Depending only on an external validator would make CI and offline import nondeterministic. | `ModelAssetValidator`, contracts, validation guide | Valid OBJ, missing/SHA, NaN/index, required role, and optional-validator skip contracts. |
| External validator adapter | [Khronos glTF Validator repository](https://github.com/KhronosGroup/glTF-Validator) | The validator is not installed in the current environment. | Run only when an executable is explicitly configured; timeout and normalize the result, otherwise report `SKIPPED`. | Auto-download or network execution is outside the offline trust boundary. | Khronos adapter | Missing executable produces deterministic `SKIPPED`; internal checks still run. |

## P4M Phase 32 - GLB Loader Decision

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| WPF GLB runtime | [glTF 2.0 specification](https://github.com/KhronosGroup/glTF/tree/main/specification/2.0), [SharpGLTF](https://github.com/vpenades/SharpGLTF), [Unity glTFast](https://github.com/atteneder/glTFast), [UnityGLTF](https://github.com/KhronosGroup/UnityGLTF), and [Assimp](https://github.com/assimp/assimp) | WPF needs a small immutable static-mesh subset; no current application uses Unity or an extra native importer. | Implement a bounded no-library GLB subset, retain OBJ/procedural fallbacks, and repeat the audit if advanced features become required. | Unity libraries do not fit WPF; Assimp broadens native deployment; adding SharpGLTF now gives less control without a demonstrated feature need. | Loader decision document | Phase 34 fixtures cover every supported accessor/container/material path and every declared unsupported class. |

## P4M Phase 33 - Runtime Model Boundary

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| Parser/renderer isolation | glTF scene/node/accessor model and [WPF 3D overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/3-d-graphics-overview) | Existing OBJ loader builds WPF objects while parsing, preventing headless GLB validation and safe background loading. | Introduce immutable renderer-neutral nodes, primitives, materials, textures, bounds, diagnostics and explicit limits. | Exposing JSON/library objects to WPF would couple security and presentation concerns. | `RuntimeModelAsset` boundary and contracts | Collection copy, SHA identity, URI/traversal rejection and checked-overflow contracts. |

## P4M Phase 34 - Bounded GLB Loading

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| Static GLB parser | [Khronos glTF 2.0 specification](https://github.com/KhronosGroup/glTF/tree/main/specification/2.0), especially GLB, accessors, meshes, nodes and materials | Runtime needs static pieces/markers, not arbitrary animation or extension execution. | Parse a closed embedded GLB subset with checked offsets/counts and all-or-nothing immutable output. | Silent extension skipping, external URI fetching and broad generic deserialization violate the bounded loader decision. | Loader, generated contracts, runtime guide | Triangle/material/hierarchy, corrupt header, bad index, NaN, optional/required extension fixtures. |

## P4M Phase 35 - WPF Conversion and Fallback

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| WPF conversion/cache | [Microsoft WPF 3D performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/maximize-wpf-3d-performance) and [Freezable overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/freezable-objects-overview) | Existing OBJ meshes are cached, but GLB needs renderer conversion without parsing on the UI thread. | Add a small WPF library that pre-sizes, freezes and caches geometry/material/texture/model resources by validated content identity. | Mutable per-frame geometry and unconditional back materials increase WPF render cost. | `ModelAssets.Wpf`, WPF contracts | Frozen model/mesh, cache reuse, double-sided behavior and build. |
| Fallback authority | Existing OBJ/procedural paths | GLB failure must not make pieces disappear or hide the cause. | Resolve validated GLB, then validated OBJ, then procedural with an explicit reason. | Silent exception swallowing would make QA non-reproducible. | Pure resolver and contracts | GLB preference, unsupported-to-OBJ, missing-to-procedural cases. |

## P4M Phase 36 - Model Asset Preview

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| Isolated preview/evidence | [WPF 3D overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/3-d-graphics-overview), `RenderTargetBitmap`, and current large ChessApp code-behind | Asset diagnostics should not first land in game UI, and desktop screenshots alone are weak evidence. | Add a standalone preview with shared validator/loader/factory, camera/overlay controls, structured report and in-process evidence. | Embedding the first diagnostic surface into ChessApp would mix import QA with gameplay state. | `ModelAssetPreview` and guide | x64 WPF build through contract project; evidence output is `.tmp`-ignored. |

## P4M Phase 37 - User Model Onboarding

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| Repeatable author workflow | Phase 26 inbox, manifest role registry, import/conversion/preview gates | A directory policy alone does not tell a user which roles/evidence are missing. | Generate an ignored per-set draft with source/textures/license/metadata folders and one of six role templates. | Creating a runtime manifest immediately would bypass license, conversion and QA. | `New-ModelSet.ps1` and author/import guides | Generate under ignored `.tmp`, parse role draft, reject invalid/duplicate set, confirm no tracked output. |

## P4M Phase 38 - Chess2D Unified Model Integration

Date: 2026-07-25

| Topic | Primary sources | Repository finding | Decision | Rejected alternatives | Files affected | Verification |
| --- | --- | --- | --- | --- | --- | --- |
| Background model loading | [WPF threading model](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model) and [WPF 3D performance guidance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/maximize-wpf-3d-performance) | ChessApp synchronously read OBJ files from a selected directory and persisted the display label. | Discover semantic sets centrally, parse bounded GLB off the dispatcher path, cancel stale set loads, then assign frozen WPF models while preserving the existing square hit map and camera. | Replacing the board control or moving gameplay state into the asset layer would expand risk without improving model compatibility. | Shared catalog, ChessApp 3D mode, model contracts | Complete GLB and OBJ catalogs, incomplete/deleted set fallback, GLB/WPF contracts, and targeted x64 ChessApp build. |
