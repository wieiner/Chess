# Next Era Project Map

Date: 2026-06-21

This is the current operator-facing map after the Next Era Linux dry-run, Chess2D portal audit, and stalled-area audit. Historical phase documents remain useful, but this page is the concise current-state entry point.

## Products

| Product | Status | Notes |
| --- | --- | --- |
| Chess2D / Chess Advisor | Playable Windows WPF app | Ordinary 8x8 chess engine with legal moves, FEN, draw status, search, and 3D model view. Portal integration is audited but not implemented. |
| Chess3DApp | Playable experimental Windows WPF app | Exactly five Chess3D RuleProfiles: Classic, Single-Side, Asgard, Rubik, Hodge. No sixth runtime mode exists. |
| RubikApp | Playable Windows WPF app | Separate Rubik state/rotation product; not a Chess3D RuleProfile. |
| ChessOnlineApp | Windows WPF online/integration hub | Account/portal/relay UI shell and local integration clients. |
| ChessOnlineServer | Linux-capable ASP.NET Core authority | `net8.0` server package can run with Linux `libChess3DEngine.so`; Hetzner systemd + Nginx HTTP smoke passed. TLS/domain hardening is still missing. |
| Chess2DBenchmark | Native benchmark executable | Measures ordinary 2D legal move/search/evaluation hot paths. |

## Executables

| Executable | Platform target | Source |
| --- | --- | --- |
| `ChessApp.exe` | Windows / WPF / `net8.0-windows` | `src/ChessApp` |
| `Chess3DApp.exe` | Windows / WPF / `net8.0-windows` | `src/Chess3DApp` |
| `RubikApp.exe` | Windows / WPF / `net8.0-windows` | `src/RubikApp` |
| `ChessOnlineApp.exe` | Windows / WPF / `net8.0-windows` | `src/ChessOnlineApp` |
| `ChessOnlineServer.dll` / `.exe` | Server / `net8.0` | `src/ChessOnlineServer` |
| `Chess2DBenchmark.exe` | Windows native console today | `src/Chess2DBenchmark` |
| `TestProcessWatchdog.exe` | .NET test utility | `tools/TestProcessWatchdog` |
| `HetznerSignalRSmoke` | .NET smoke utility | `tools/HetznerSignalRSmoke` |

## Libraries

| Library | Status | Notes |
| --- | --- | --- |
| `ChessEngine.dll` | Windows native | Ordinary 2D chess rules/search. |
| `Chess3DEngine.dll` | Windows native | Authoritative Chess3D engine for Windows apps/tests. |
| `libChess3DEngine.so` | Linux native, proven by Hetzner build | Not committed; copied into Linux server packages from a separately built/tested artifact. |
| `RubikEngine.dll` | Windows native | Rubik state/rotation engine. |
| `ChessGpuBackend.dll` | Windows native | CPU/Direct3D fallback and optional CUDA dynamic loading. |
| `ChessCudaBackend.dll` | Optional CUDA | Not required for normal build/test/CI. |
| `ChessOnlineProtocol` | Portable `net8.0` | Online authority protocol/domain layer. |
| `ChessOnlinePersistence` | Portable `net8.0` | JSON persistence baseline for accounts/sessions/rooms/tables/actions. |

## Test Suites

| Suite | Runner | Status |
| --- | --- | --- |
| Native | `tests/run-tests.ps1 -Suite Native` | C++ contract tests for ChessEngine, Chess3DEngine, RubikEngine, GPU backend. |
| Chess2D | `tests/run-tests.ps1 -Suite Chess2D` | 2D engine contract tests; benchmark optional unless `-SkipBenchmark`. |
| Chess3D | `tests/run-tests.ps1 -Suite Chess3D` | Full Chess3D contract and scenario fixture layer. |
| Online | `tests/run-tests.ps1 -Suite Online` | Managed protocol and SignalR tests through C# watchdog timeouts. |
| Full verify | `scripts/verify.ps1` | Windows Build gate used by CI. |

The decomposed runner uses controlled `/m:N`, bounded test executable timeouts, and logs under `.tmp/test-logs`.

## Server Deploy State

Current proven state:

- Linux `libChess3DEngine.so` was built on Hetzner in a temporary build workspace.
- Linux server package was published locally for `linux-x64`.
- Package was installed under `/opt/chessonline/server`.
- Runtime data/keyring/log directories exist under `/var/lib/chessonline` and `/var/log/chessonline`.
- `chessonline.service` runs Kestrel on `127.0.0.1:5077`.
- Nginx proxies public HTTP port 80 to Kestrel.
- Public `/healthz/live`, `/healthz/ready`, and `/chess3d/diagnostics` probes passed.

Still not production-complete:

- no confirmed domain;
- no TLS certificate;
- no HTTPS-only auth/token policy;
- no public authenticated SignalR smoke over HTTPS;
- no rate limiting, log rotation, backup/restore rehearsal, or rollback package flow.

## Game Modes

Runtime Chess3D RuleProfiles are exactly five:

1. `classic-six-side-3d-8x8x8-v0.1`
2. `single-side-3d-8x8x8-v0.1`
3. `asgard-convergence-3d-8x8x8-v0.1`
4. `rubik-convergence-3d-8x8x8-v0.1`
5. `hodge-projection-duel-3d-8x8x8-v0.1`

Scenario, smoke, playthrough, regression, deployment, identity, persistence, and SignalR JSON files are not modes.

## Asset Pipeline

- Canonical local model catalog: `assets/models/chess/pieces`.
- Runtime model manifest: `piece_sets.json`.
- Chess2D and Chess3D copy the same OBJ/MTL catalog into `Assets/Models`.
- Diffuse textures and MTL metadata are best-effort; missing/PBR maps fall back to readable WPF materials.
- Generated-piece manifest is descriptor-only and disabled; GLB/glTF remains future.

## Online Architecture

- `ChessOnlineServer` is the authoritative server for Chess3D online play.
- Clients submit commands; server validates through existing Chess3D engine actions.
- SignalR is transport, not rules authority.
- The current deployment is single-server.
- Redis/Azure SignalR/backplane, ranked matchmaking, and multi-node authority remain future work.
- Chess2D portal work is separate and should start with PGN/SAN, UCI, and safe Lichess token policy.

## Known Blockers

1. TLS/domain and HTTPS auth enforcement.
2. Public SignalR smoke over HTTPS.
3. Deployment rollback, backup/restore, and log rotation.
4. Documentation reconciliation for old historical `draft`/`blocked` text.
5. Chess2D PGN/SAN and UCI adapter.
6. Visual QA automation.
7. AI/search strength and anti-cheat policy.

## Non-Source Runtime Artifacts

Do not commit:

- private keys;
- tokens/passwords/cookies;
- TLS certificates or private keys;
- Data Protection keyrings;
- runtime stores/databases;
- raw SSH logs;
- remote build outputs such as `.so` binaries;
- large generated model assets without an explicit asset audit.
