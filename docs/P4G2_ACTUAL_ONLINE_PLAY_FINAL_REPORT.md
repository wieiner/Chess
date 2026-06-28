# P4G2 Actual Online Play Final Report

Date: 2026-06-28

## Baseline

The P4G2 continuation started from the last pushed baseline:

```text
675d0484998a0d3e7a6239fc531302de67f886a6
P4G2 phase 11: harden realtime resync
```

During recovery, the interrupted Phase 21/12-style smoke work was found in the local tree, preserved, completed, tested, and committed.

## Phase Commits

| Phase | Commit | Summary |
| --- | --- | --- |
| 21 | `ec4dbb050` | Record five-profile online coverage. |
| 22 | `1f9445efe` | Audit special action UI boundary. |
| 23 | `b0ccc6d10` | Guard special online actions. |
| 24 | `ba58535b3` | Add Rubik special action UI boundary. |
| 25 | `b1aaf90fe` | Add Hodge projection UI boundary. |
| 26 | `2c4cfa270` | Document actual online play guide. |
| 27 | `93a005938` | Polish online playability UI. |
| 28 | `1da9e11d1` | Polish online play session reports. |
| 29 | `d72240c61` | Audit online play secret handling. |
| 30 | `804db6149` | Verify actual online play. |

This final report is Phase 31.

## What Is Playable Now

The current P4G2 path supports practical online play over the diagnostic HTTP 80 Hetzner deployment:

- connect `ChessOnlineApp` to the Hetzner HTTP endpoint;
- check health and diagnostics;
- create temporary users;
- create a one-app two-client test match;
- use two-window manual matchmaking;
- select from exactly five Chess3D RuleProfiles;
- request authoritative snapshots;
- see board cells from server snapshots;
- request legal preview;
- click legal targets for normal moves;
- submit accepted normal actions;
- see rejected action reasons;
- see action log and server sequence;
- save local session reports;
- copy sanitized session summaries.

## Profile Coverage

| Profile | Online startup | Snapshot | Legal preview/action | Current boundary |
| --- | --- | --- | --- | --- |
| Classic Six-Side | PASS | PASS | Normal move accepted through `server-preview`. | Playable online normal move path. |
| Single-Side Training | PASS | PASS | Snapshot-only in final operator smoke. | Training action UX remains lighter than Classic. |
| Asgard / Meru | PASS | PASS | Normal move accepted through `server-preview`. | Core/fusion/reserve special actions need richer dedicated UX. |
| Rubik Convergence | PASS | PASS | Startup/snapshot PASS. | Layer turns are not submitted as `NormalMove`; Rubik panel is visible but dispatch remains future. |
| Hodge Projection Duel | PASS | PASS | Startup/snapshot PASS. | Projected composite moves require dedicated Hodge panel submit flow. |

No sixth Chess3D RuleProfile was added.

## Remote Hetzner Verification

Remote operator smoke used:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 -BaseUrl "http://178.105.220.117" -ProfileId "<profile>" -TimeoutSeconds 180 -NoSecretLog
```

Final smoke results:

- Asgard full action: PASS, `action-source=server-preview`, notation `#1 S1 MOVE R (2,2,0)->(1,2,0)`, final hash `e8443902f01a9450`.
- Classic full action: PASS, `action-source=server-preview`, notation `#1 S1 MOVE K (4,4,0)->(3,5,1)`, final hash `679085fef5801b2a`.
- Single-side startup/snapshot/action-log: PASS, final hash `e3a0ccbe33ae47df`.
- Rubik startup/snapshot/action-log: PASS, final hash `df5cecc8f5e0c331`.
- Hodge startup/snapshot/action-log: PASS, final hash `668481062bf778bd`.

## UI Click Path

One-app test pair:

1. Run `src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe`.
2. Click `Use Hetzner HTTP`.
3. Click `Check Health`.
4. Click `Check Diagnostics`.
5. Click `Create Two Test Players`.
6. Select `Classic` or `Asgard`.
7. Click `Create Test Match With Two Local Clients`.
8. Click `Ready Both`.
9. Click `Start Game`.
10. Click `Request Snapshot`.
11. Click an occupied source cell.
12. Click a highlighted legal target.
13. Confirm action accepted, board refresh, action log update, compact status update.
14. Click `Save Session Report` or `Copy Sanitized Summary`.

Two-window manual path:

1. Launch two `ChessOnlineApp` instances.
2. In both windows, click `Use Hetzner HTTP`.
3. Create one temporary player per window.
4. Select the same profile.
5. Use `Two-Window Manual Player`.
6. Join matchmaking from both windows.
7. Ready/start.
8. Request snapshot in both.
9. Submit a legal-preview move in the side-to-move window.
10. Refresh/request action log in the peer window.

## UI Changes in This Closeout

- Compact online status line with server/auth/match/turn/preview/realtime/action counters.
- Rubik and Hodge special-action panels are profile-aware and visible only for their profiles.
- Generic board click-to-move rejects Rubik/Hodge/reserve special actions instead of downgrading them to `NormalMove`.
- Session reports include snapshot hash, selected cells, legal preview options, realtime state, action log tail, and UI status strings.
- Clipboard summaries are sanitized and token/password-free.

## Local Verification

Commands passed:

```powershell
git diff --check
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -SkipBenchmark -MSBuildMaxCpuCount 1
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

`scripts\verify.ps1` passed, including build, packaging, contract tests, SignalR tests, and quick benchmark.

## CI

Recent GitHub Actions runs:

- `28309338378` Phase 27: success.
- `28309538635` Phase 28: success.
- `28309655873` Phase 29: success.
- `28310161264` Phase 30: success.

Phase 31 CI is expected after this report commit.

## Security Boundary

- HTTP 80 is diagnostic/dev-only.
- Temporary users only.
- No real passwords should be entered over HTTP.
- Access tokens and refresh tokens are held in memory and not printed.
- Runtime reports/logs stay under ignored `.tmp`.
- No private keys, certificates, keyrings, runtime stores, or raw smoke logs were committed.

## Infrastructure Not Touched

P4G2 closeout did not touch:

- 443;
- TLS/domain;
- x-ui/Xray;
- Outline;
- Albatronix Docker;
- Unreal SYServer;
- Nginx;
- UFW/firewall;
- systemd configuration.

## Remaining Work

- P4I/P4G visual board polish: clearer online board readability, action history grouping, target highlighting UX.
- P4H reconnect/resume/spectator.
- P4E later: dedicated server or TLS/domain/443 plan after the x-ui decision.
- Rubik online layer-turn submit UX.
- Hodge projected composite online submit UX.
- Richer Chess3D board integration for full online 3D play.
- Chess2D PGN/UCI/Lichess path.
