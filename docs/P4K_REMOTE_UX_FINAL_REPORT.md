# P4K Remote Online UX Final Report

Date: 2026-07-13  
Branch: `main`

## Executive Result

P4K is complete for the current diagnostic/development boundary. The public
Hetzner HTTP 80 deployment supports authoritative play, legal preview, explicit
resume, spectator read-only mode, lobby discovery, realtime resync, action log,
and all five existing RuleProfiles. No sixth profile or game-rule change was
introduced.

This is not a production security claim. TLS/domain/443, ranked accounts,
multi-node authority, and exact match recovery across a server process restart
remain outside the proven boundary.

## Source And Recovery

- P4K continuation starting commit:
  `20a897b27b9bb362824a9587fa3aae032860c0c1`.
- Phase 15 dirty-tree recovery preserved:
  `tools/HetznerSignalRSmoke/Program.cs` and
  `scripts/deploy/Test-HetznerOnlineUx.ps1`.
- No reset, clean, destructive checkout, or unrelated file removal was used.
- Smoke tooling completion commit:
  `fae937a5f0` (`P4K phase 15: add remote online UX smoke tooling`).
- Phase 44 closeout commit: the commit containing this report, with subject
  `P4K phase 44: finalize remote online UX hardening`; its immutable hash and
  CI run are recorded in the final operator handoff after push.

## Deployed Build Identity

- deployed source commit:
  `810f8ff9a917191f420bb6eaa8ae36191ea607ba`;
- package id: `chessonline-linux-x64-810f8ff9a917`;
- package archive SHA-256:
  `144DF324F156FE1FE002ADA091A65D66DA999A69F23DE3796FC9A1BE6366BC4C`;
- Linux native library SHA-256:
  `A5B5E0B707D09B199D49FE62CA5B5F00895F28B1A78E4D082584776C9913694D`;
- backup:
  `/opt/chessonline/backups/server-before-p4k-hardening-20260713-141535.tar.gz`;
- backup SHA-256:
  `9166cef17e1819752eae72d87b711e9a1e8e5ebb55ddd64cfc77a72ea1dd07c6`;
- retained previous payload:
  `/opt/chessonline/server.prev.20260713-141554`.

The guarded deployment replaced only `/opt/chessonline/server` and restarted
only `chessonline.service`. Rollback was not needed. The explicit rollback
dry-run passed against the retained payload and backup.

## Remote UX Evidence

| Capability | Result | Evidence |
| --- | --- | --- |
| Asgard play | PASS | match/start, server legal preview, accepted action, snapshot and action log |
| Resume | PASS | same seat, hash, sequence, and action count after authenticated reconnect |
| Spectator | PASS | no player seat, mutation rejected, live authoritative update observed |
| Lobby | PASS | safe rows, active table discovery, spectator navigation, invalid resume rejected |
| Combined `all` | PASS | play, resume, lobby, spectator, and final diagnostics in one bounded run |
| WPF resume | PASS | 10.26 seconds; disconnected submit disabled; explicit resume restored authority |
| WPF spectator | PASS | 17.24 seconds; read-only controls and live action update verified |
| WPF lobby | PASS | 13.49 seconds; visible row selection and spectator navigation verified |
| Three-client WPF | PASS | two players plus lobby-discovered spectator converged on sequence 1 and one hash |

The final remote operator regression used bounded C# watchdog processes and
ignored unique logs. There were no process timeouts and no tracked raw reports.

## Five-profile Result

Exactly these five profiles started, produced snapshots and legal preview, and
remained isolated:

| Profile | Remote result | Action boundary |
| --- | --- | --- |
| Classic Six-Side | PASS | accepted normal knight move from server preview |
| Single-Side Training | PASS | accepted normal training move |
| Asgard Convergence | PASS | accepted normal move; core/reserve special UX remains separate |
| Rubik Convergence | PASS | accepted normal move; layer turn remains a distinct special action |
| Hodge Projection Duel | PASS | normal preview path passed; projection composite remains distinct |

Rubik layer turns, Hodge projection composites, and Asgard reserve/core actions
are never relabelled or submitted as `NormalMove` merely to make a smoke pass.

## Lifecycle And Restart Boundary

- spectator membership is tracked and removed on disconnect;
- disconnected transport records are cleaned without changing board, sequence,
  history, seats, or state hash;
- bounded room cleanup uses injected time, a batch limit, and conservative TTLs;
- active and disconnected-resumable games are never removed by Phase 31;
- persistent record deletion remains deferred because the store has no tested
  atomic room/table delete contract;
- exact resume after a server process restart is **not implemented**.

Restart rehydration was audited and designed, then deliberately left disabled.
Current persistence has enough ingredients for a future versioned checkpoint
protocol, but not a proven atomic snapshot/action continuity contract.

## Readiness, Logging, And Limits

`/healthz/ready` now checks native session creation, the exact five-profile
set, registry construction, writable persistence and keyring storage, and
normalized configuration. Public failure output contains only safe reason
codes. `/healthz/live` remains a process liveness probe.

Framework hosting/SignalR connection logs are filtered to avoid query-token
Info logging. Application logs and reports retain aggregate status without
tokens, passwords, authorization headers, connection IDs, or raw state.

Low-risk fixed-window HTTP policies are enabled:

- register: 5 requests / 10 minutes;
- login: 10 / minute;
- session: 30 / minute;
- diagnostics: 30 / minute;
- health endpoints: outside those restrictive budgets.

Hub commands retain defense-in-depth limits partitioned by stable authenticated
player identity across reconnect. The final regression respected real window
renewal; no limiter bypass or service restart was used.

## Verification

- all six contract executables without benchmark: PASS in 171.7 seconds;
- full `scripts/verify.ps1`: PASS in 495.7 seconds;
- quick Chess2D benchmark: PASS;
- optional CUDA absence remained non-fatal;
- Phase 43 final remote regression: PASS;
- Phase 43 CI run `29262514490`: success.

The Phase 44 commit receives a separate final GitHub Actions run after this
document is committed and pushed.

## Security And Isolation

HTTP 80 is diagnostic/development only. Only generated temporary users are
suitable. No real password, access/refresh token, private key, certificate,
keyring, runtime store, or raw smoke report is tracked or printed.

P4K did not change nginx, UFW/firewall, DNS, TLS/443, x-ui/Xray, Outline,
Albatronix Docker, Unreal SYServer, PostgreSQL, or the VPS boot state.

## Known Limitations And Next Front

1. Implement versioned atomic match checkpoints before claiming restart-resume.
2. Add dedicated TLS/domain/HTTPS deployment only after the 443 ownership plan.
3. Improve Rubik/Hodge/Asgard special-action online UX without weakening action kinds.
4. Add lobby paging/recent-first ordering and production account policy.
5. Continue Chess2D PGN/SAN, UCI, and token-safe portal integration separately.

