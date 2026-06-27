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
