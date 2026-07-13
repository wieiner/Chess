# Next Era Final Report

Date: 2026-06-21

Reachability refresh: 2026-06-28

This report records the Next Era hardening sequence and the later P4G2 actual online play pass. It records what is actually proven, what is still only planned, and which work should come next.

## Executive status

- Repository branch: `main`.
- Phase 14 started from `18dde71e NextEra phase 13: add future 3D mode incubator`.
- Local bounded contract tests passed with `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark -GlobalTimeoutSeconds 1200 -MSBuildMaxCpuCount 1`.
- Local full verify passed with `pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1`.
- The real Chess3D RuleProfile count is still exactly five. Scenario, playthrough, regression, and incubator documents are not game modes.

## Phase commits and CI

| Phase | Commit | Result |
| --- | --- | --- |
| Phase 00 | `a4cf47d5 NextEra phase 00: record current baseline` | GitHub Actions `27904977918` succeeded. |
| Phase 01 | `ca8c7d8e NextEra phase 01: harden test runner operations` | GitHub Actions `27905630260` succeeded. |
| Phase 02 | `69b717ee NextEra phase 02: record Hetzner server reality check` | GitHub Actions `27905798973` succeeded. |
| Phase 03 | `23267cbe NextEra phase 03: complete Linux server package` | GitHub Actions `27906887152` succeeded. |
| Phase 04 | `0ca76929 NextEra phase 04: run Hetzner Kestrel smoke` | GitHub Actions `27907084609` succeeded. |
| Phase 05 | `a3579798 NextEra phase 05: verify remote SignalR Asgard smoke` | GitHub Actions `27907488069` succeeded. |
| Phase 06 | `dcfeb526 NextEra phase 06: install Linux package to production layout` | GitHub Actions `27907701973` succeeded. |
| Phase 07 | `3a0f4af5 NextEra phase 07: install ChessOnlineServer systemd service` | GitHub Actions `27907880715` succeeded. |
| Phase 08 | `1733403a NextEra phase 08: expose ChessOnlineServer through Nginx HTTP` | GitHub Actions `27909021965` succeeded. |
| Phase 09 | `fe30ecde NextEra phase 09: document TLS and domain status` | GitHub Actions `27909209829` succeeded. |
| Phase 10 | `4b2d03cd NextEra phase 10: audit Chess2D portal integration path` | GitHub Actions `27909542201` succeeded. |
| Phase 11 | `27c457f0 NextEra phase 11: audit stalled work areas` | GitHub Actions `27909739182` succeeded. |
| Phase 12 | `3253beca NextEra phase 12: clean roadmap and project status` | GitHub Actions `27909987789` succeeded. |
| Phase 13 | `18dde71e NextEra phase 13: add future 3D mode incubator` | GitHub Actions `27910186262` succeeded. |

## Linux server status

The Hetzner server has a real Linux-native ChessOnlineServer dry-run path:

- Linux-native `libChess3DEngine.so` was built on Hetzner with CMake/Ninja/clang++.
- ABI parity was checked between Windows `Chess3DEngine.dll` and Linux `libChess3DEngine.so`.
- The server-side projects are portable `net8.0`: `ChessOnlineProtocol`, `ChessOnlinePersistence`, and `ChessOnlineServer`.
- WPF applications remain Windows-only `net8.0-windows`.
- A Linux `linux-x64` ChessOnlineServer package was produced with the native library, RuleProfiles, scenarios, schemas, and deploy templates.
- The production-like layout exists under `/opt/chessonline/server`.
- Mutable runtime state belongs under `/var/lib/chessonline`.
- Runtime logs belong under `/var/log/chessonline`.

This is not yet a hardened production deployment. It is a working single-server authority rehearsal with loopback health, systemd, Nginx, and external HTTP diagnostics.

## Public HTTP health result

Phase 14 public HTTP probes against `http://<HETZNER_HOST>` returned:

- `GET /healthz/live`: HTTP 200, `Healthy`.
- `GET /healthz/ready`: HTTP 200, ready JSON with `profileCount: 5`, `authEnabled: true`, and `persistenceProvider: json`.
- `GET /chess3d/diagnostics`: HTTP 200, diagnostics JSON reporting:
  - `authorityPlatform: Linux`
  - `authorityProcessArchitecture: X64`
  - `authorityNativeLibraryName: libChess3DEngine.so`
  - `authorityNativeLibraryPath: /opt/chessonline/server/libChess3DEngine.so`
  - `authorityIsSupported: true`
  - `authEnabled: true`
  - `persistenceProvider: json`

Refresh on 2026-06-27 after the firewall opening:

- Loopback health through SSH still works.
- `chessonline.service` is still active/enabled.
- Nginx is still active and `nginx -t` succeeds.
- Nginx listens on `0.0.0.0:80`.
- `ufw` now allows `80/tcp`.
- External TCP connect to `<HETZNER_HOST>:80` succeeds from the local workstation.
- External `/healthz/live`, `/healthz/ready`, and `/chess3d/diagnostics` pass.
- `<HETZNER_HOST>:443` is reachable, but it is owned by an existing non-Chess process, not ChessOnlineServer.

Current conclusion: the server is alive internally and externally over HTTP port 80. It is still not production-ready because TLS/domain/443 are not configured for ChessOnlineServer.

## systemd status

The remote read-only probe reported:

- `chessonline.service`: `active`.
- `chessonline.service`: `enabled`.

The installed service path on the server is:

- `/etc/systemd/system/chessonline.service`

The tracked template is:

- `deploy/linux/chessonline-server.service.template`

The service runs Kestrel on loopback:

- `http://127.0.0.1:5077`

Useful operator checks:

```powershell
ssh -i "$env:USERPROFILE\.ssh\id_ed25519_hetzner" root@<HETZNER_HOST> "systemctl status chessonline.service --no-pager"
ssh -i "$env:USERPROFILE\.ssh\id_ed25519_hetzner" root@<HETZNER_HOST> "journalctl -u chessonline.service -n 100 --no-pager"
```

## Nginx status

The remote read-only probe reported:

- `nginx`: `active`.
- `nginx -t`: configuration syntax is OK and test is successful.
- Local proxy health through Nginx returned `Healthy`.
- Local proxy diagnostics returned Chess3D diagnostics JSON.

The expected installed Nginx config location is:

- `/etc/nginx/sites-available/chessonline`
- `/etc/nginx/sites-enabled/chessonline`

The tracked template is:

- `deploy/linux/nginx-chessonline.conf.template`

## TLS status

TLS is not configured.

Reason:

- No real domain/DNS target has been confirmed for the server.
- No Let's Encrypt/Certbot run was performed.
- No real certificates, private keys, tokens, passwords, runtime store, or Data Protection keyring files were committed.

Current public HTTP must be treated as diagnostic-only. It is externally reachable on port 80, but do not use real user credentials over public HTTP. The correct next step is P4E: domain, DNS, firewall policy, Certbot, HTTPS, renewal, backup/restore, and rollback rehearsal.

## SignalR / Asgard smoke status

Remote authenticated SignalR/Asgard smoke passed in Phase 05 through a controlled SSH local-forwarded path. On 2026-06-27, the public HTTP smoke tooling was fixed and the same Asgard chain also passed directly through `http://<HETZNER_HOST>`.

The smoke covered:

- Health/readiness/diagnostics preflight.
- Two ephemeral authenticated users.
- SignalR connection.
- Matchmaking into an Asgard-style Chess3D room.
- Snapshot request.
- Legal Asgard action submission.
- Action log verification.

Public HTTPS SignalR has not been proven because TLS/domain is not configured yet.

Current public HTTP command shape:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://<HETZNER_HOST>" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -TimeoutSeconds 180 `
  -NoSecretLog
```

## P4F playable online client

P4F adds a practical Windows client path in `ChessOnlineApp` for the same diagnostic HTTP 80 deployment.

The UI now supports:

- selecting the Hetzner HTTP endpoint;
- checking `/healthz/live`, `/healthz/ready`, and `/chess3d/diagnostics`;
- creating temporary test users without printing tokens or passwords;
- connecting SignalR clients;
- selecting one of the five real Chess3D RuleProfiles;
- creating a two-local-client test match;
- ready/start/snapshot/action-log flow;
- submitting the known safe Asgard test action;
- saving a sanitized session report under ignored `.tmp/manual-smoke`.

Operator guide:

- `docs/P4F_PLAYABLE_ONLINE_USER_GUIDE.md`

This is still a diagnostic/dev client path. Do not use real credentials over HTTP; TLS/domain/443 remain deferred.

## Test runner status

The old failure mode was an unbounded PowerShell/process runner path where a requested short timeout still allowed commands to hang for hours. That path is no longer trusted.

Current status:

- The C# `tools/TestProcessWatchdog` executable is the authoritative per-process timeout wrapper.
- `tests/run-tests.ps1` supports decomposed suites, bounded test execution, controlled MSBuild parallelism, and `.tmp/test-logs` output.
- Phase 14 bounded `run-tests.ps1 -SkipBenchmark` passed.
- Phase 14 full `scripts/verify.ps1` passed.

Recommended day-to-day commands:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Native -SkipBenchmark -MSBuildMaxCpuCount 1
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Online -SkipBenchmark -OnlineTestTimeoutSeconds 180
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark -GlobalTimeoutSeconds 1200 -MSBuildMaxCpuCount 1
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

## Historical debt status

The central project status and roadmap have been cleaned so they no longer describe the Linux server as merely blocked or Classic Chess3D as draft-only. Some historical documents intentionally remain in the repo because they explain prior decisions, blocked paths, or old phase results.

The most important remaining technical debt groups are:

- Linux CI/smoke coverage is not yet part of GitHub Actions.
- TLS/domain/firewall/backup/restore/rollback are not complete.
- Public HTTP is reachable, but it is not a secure production auth boundary without TLS.
- Public HTTPS SignalR is not proven.
- Online reconnect/resume/spectator UX is future work.
- Asgard destructive fusion/implosion and richer reserve/core UI are future work.
- Full public portal integration for Chess2D is future work.
- Visual QA automation and richer generated piece asset work remain future work.

See also:

- `docs/NEXT_ERA_STALLED_AREAS_AUDIT.md`
- `docs/NEXT_ERA_PROJECT_MAP.md`

## Chess2D portal integration status

Chess2D is a strong local/engine foundation, but it is not yet a portal client.

Current state:

- 8x8 engine and contract tests exist.
- Legal moves, checkmate/stalemate/draw status, FEN, AI/search, and benchmark coverage exist.
- Full PGN/SAN import/export is not complete.
- A UCI-compatible process adapter is not complete.
- Lichess/Chess.com integration is not implemented.

Portal direction:

- Lichess is the realistic first live-play target, but it needs safe token storage, PGN/SAN/FEN, clock handling, and a clear distinction between human Board API use and bot/engine play.
- Chess.com PubAPI is read-only and suitable for archive/profile/current-game import only unless an approved interactive API is separately available.

See:

- `docs/NEXT_ERA_CHESS2D_PORTAL_INTEGRATION_AUDIT.md`

## Future modes status

Future 3D modes are documented only in:

- `docs/NEXT_ERA_MODE_INCUBATOR.md`

No new RuleProfile JSON files, schema enum values, runtime hooks, tests, or game modes were added.

The five incubator ideas are:

- Timefold 3D Chess.
- Portal/Gate Chess.
- Gravity Well Chess.
- Orbit Chess.
- Team Cathedral Chess.
- Shadow Mirror Chess.

They are design candidates, not product features.

## Five Chess3D modes

The real Chess3D RuleProfiles remain exactly five:

1. `classic-six-side-3d-8x8x8-v0.1`
2. `single-side-3d-8x8x8-v0.1`
3. `asgard-convergence-3d-8x8x8-v0.1`
4. `rubik-convergence-3d-8x8x8-v0.1`
5. `hodge-projection-duel-3d-8x8x8-v0.1`

No sixth mode was added.

## Final local verification

Phase 14 local gates:

- `git diff --check`: passed.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark -GlobalTimeoutSeconds 1200 -MSBuildMaxCpuCount 1`: passed.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1`: passed.

The full verify built Release x64 outputs, checked assets/profiles/scenarios/model packaging, built ProductionOutput, and ran contract tests plus the quick Chess2D benchmark. CUDA remained optional and unavailable in the local environment, which is expected.

## Recommended next phases

1. P4K - Deploy updated online UX server package:
   - Deploy the current `ChessOnlineServer` package that exposes `RequestResumeMatch`, `JoinSpectator`, and `RequestLobbySnapshot`.
   - Keep HTTP 80 diagnostic deployment unchanged at the network layer.
   - Do not touch 443/TLS/x-ui/Xray during this deploy.
   - Re-run remote resume, spectator, lobby, Classic, and Asgard smoke.

2. P4E - TLS/domain/public deployment hardening:
   - DNS/domain confirmation.
   - Keep port 80 available for ACME/HTTP redirect as needed.
   - Certbot/HTTPS.
   - Firewall.
   - Backup/restore.
   - Rollback rehearsal.
   - Public HTTPS health and SignalR smoke.

3. P4L - Public online hardening:
   - Rate limiting and user-facing online errors.
   - Reconnect/spectator persistence across server restart.
   - Operator-facing lobby health and room cleanup.

4. P4G - Asgard gameplay deepening:
   - Core/fusion/reserve UX.
   - Destructive implosion rules if selected.
   - More complete Asgard playthroughs and balance passes.

5. P5 - Real generated 3D pieces / glTF pipeline:
   - GLB/glTF import/export direction.
   - Better material/animation fidelity.
   - Visual QA screenshots.

6. P6 - Chess2D Lichess/UCI/PGN integration:
   - PGN/SAN import/export.
   - UCI process adapter.
   - Lichess token-safe client.
   - Chess.com read-only import path.

## P4K Remote UX Closeout (2026-07-13)

P4K supersedes the earlier deployment-pending statements in this report. The
current Hetzner payload is source commit
`810f8ff9a917191f420bb6eaa8ae36191ea607ba`, package
`chessonline-linux-x64-810f8ff9a917`. Public HTTP 80 remote scenarios pass for
play, resume, spectator, lobby, and their combined flow. WPF UI Automation also
passed resume, spectator, lobby, and an independent three-client flow.

All five RuleProfiles pass remote start/snapshot/legal-preview coverage. Normal
actions pass for all five in the final regression, while Rubik layer turns,
Hodge projection composites, and Asgard special actions remain distinct UI
work rather than fabricated normal moves.

The deployed server includes bounded disconnected/spectator cleanup,
conservative room lifecycle cleanup, dependency-aware readiness, query-token
logging protection, and fixed-window request limits. Active and resumable games
are retained. Exact match recovery across a server process restart remains
audit/design only and is not claimed.

Full local contract tests and `scripts/verify.ps1` passed. The deploy retained a
protected backup and previous server directory; rollback dry-run passed and
actual rollback was unnecessary. See `P4K_REMOTE_UX_FINAL_REPORT.md`,
`P4K_HETZNER_OPERATOR_GUIDE.md`, and `P4K_REMOTE_UX_USER_GUIDE.md`.
