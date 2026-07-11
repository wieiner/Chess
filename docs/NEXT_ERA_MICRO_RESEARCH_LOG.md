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
