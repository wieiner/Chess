# P4K Final Remote Regression

Date: 2026-07-13

## Result

Status: **PASS**

The final operator regression ran sequentially against deployed package
`chessonline-linux-x64-810f8ff9a917` over public diagnostic HTTP 80. Raw logs
remain under ignored `.tmp/remote-ux-smoke`; this document contains no runtime
IDs, credentials, or tokens.

## Health And Identity

- live: `Healthy`;
- ready: HTTP 200 / `ready`;
- RuleProfiles: exactly 5;
- deployed commit: `810f8ff9a917191f420bb6eaa8ae36191ea607ba`;
- authority: Linux X64, `libChess3DEngine.so`, supported;
- legal preview, realtime resync, action log, matchmaking, resume, spectator,
  and lobby capabilities: enabled.

## Scenario Results

| Scenario | Run ID | Result | Key proof |
| --- | --- | --- | --- |
| play / Asgard | `p4k-phase43-play-20260713` | PASS | match/start, server preview, accepted action, snapshot/log |
| spectator / Asgard | `p4k-phase43-spectator-20260713` | PASS | no seat, read-only rejects, live state update |
| resume / Asgard | `p4k-phase43-resume-20260713` | PASS | same seat/hash/seq after authenticated reconnect |
| lobby / Asgard | `p4k-phase43-lobby-20260713` | PASS | initial and active safe rows locate exact table |
| combined all / Asgard | `p4k-phase43-all-20260713` | PASS | play, resume, lobby, spectator, final diagnostics |

All callbacks, connects, invokes, and disconnects completed inside the bounded
C# watchdog. No scenario process timed out.

## Five-profile Matrix

| Profile | Run ID | Start/snapshot | Server preview | Accepted action | Limitation |
| --- | --- | --- | --- | --- | --- |
| Classic | `p4k-phase43-classic-pass-20260713` | PASS | PASS | normal knight move | full UI polish remains separate |
| Single-Side | `p4k-phase43-single-20260713` | PASS | PASS | normal move | training semantics retained |
| Asgard | `p4k-phase43-play-20260713` | PASS | PASS | normal convergence-profile move | advanced core actions remain mode UI work |
| Rubik | `p4k-phase43-rubik-20260713` | PASS | PASS | normal move | layer turn is a separate special action |
| Hodge | `p4k-phase43-hodge-20260713` | PASS | PASS | normal preview option | projection composite is a separate special action |

No sixth profile was introduced. Rubik/Hodge/Asgard special actions were not
misrepresented or dispatched as an ordinary move.

## Rate-limit Evidence

The register policy is `5 requests / 10 minutes` per public IP. Separate smoke
processes share that partition. Two deliberately overpacked Classic attempts
received fixed `429 rateLimited` before player/match creation. The operator
waited for real fixed-window renewal; no limiter bypass, service restart, or
configuration change was used. The clean retry then passed.

Operational consequence: run `Scenario all` alone in one auth window. Batch at
most two two-player profile scenarios in another window, and allow a genuine
renewal before the next batch. This is expected application protection, not a
gameplay rejection.

## Logs And Runtime Diagnostics

- unique Phase 43 stdout/stderr files: 22;
- successful scenario processes: 9;
- expected rate-limited operator attempts: 2;
- local raw-log secret marker lines: 0;
- deployed journal lines inspected since Phase 41: 857;
- journal crash/native/persistence/permission/fatal markers: 0;
- journal access-token/refresh-token/Authorization/password markers: 0;
- service: active;
- accepted authoritative actions: 13;
- protocol rejections: 9 (expected spectator/read-only and negative authority
  checks included);
- cleanup runs: 14;
- last cleanup removed count: 0;
- resumable test tables retained: 10.

Framework request logs no longer expose query-string tokens. The fixed 429 body
also avoids logging the partition key or credentials.

## Rollback And Neighbor Gate

- backup present:
  `/opt/chessonline/backups/server-before-p4k-hardening-20260713-141535.tar.gz`;
- previous payload present:
  `/opt/chessonline/server.prev.20260713-141554`;
- rollback dry-run: PASS;
- actual rollback: not needed.

Listener ownership remains unchanged for ChessOnline Kestrel 5077, nginx 80,
Xray 443, Outline 22527, Docker proxy 3000, and SSH 22. Nginx, UFW, DNS, TLS,
443, x-ui/Xray, Outline, Albatronix, Unreal, and PostgreSQL were not modified.

## Security Boundary

This is still diagnostic/development HTTP. Only generated temporary accounts
were used. No real password, token, keyring, private key, certificate, runtime
store, or raw report was tracked or printed.
