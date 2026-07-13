# P4K Remote Three-Client End-to-End Result

Date: 2026-07-13  
Profile: `asgard-convergence-3d-8x8x8-v0.1`  
Endpoint boundary: public diagnostic HTTP 80; TLS/443 was not used or changed.

## Result

PASS. A bounded UI Automation run drove three independent Release builds of
`ChessOnlineApp.exe` against the deployed Hetzner authority:

- client A: temporary authenticated manual player;
- client B: a second temporary authenticated manual player;
- client C: a separately authenticated, lobby-discovered spectator.

The accepted action changed the authoritative state fingerprint from
`a0296f7e94a22346` to `e8443902f01a9450` at server sequence `1`. All three
clients converged on the latter state.

## Proven Flow

1. A and B registered independent temporary users and joined Asgard
   matchmaking from separate WPF processes.
2. Both seats became ready, A started the game, and A/B initial snapshots
   matched.
3. C refreshed the Asgard lobby, selected the exact new room/table row, joined
   as spectator, and received the same initial snapshot.
4. A selected `(2,2,0)`, received server legal preview, and clicked legal target
   `(1,2,0)`.
5. The authority accepted the move. B and C both observed realtime sequence
   `1`; an authoritative snapshot supplied the full final hash.
6. A manually disconnected. Its submit control became disabled.
7. While A was offline, B could still request the latest snapshot and C could
   still follow the latest move; both retained the final authoritative hash.
8. A reconnected and resumed the retained match context. Its snapshot/action
   count and hash matched B/C.
9. C's ready, start, normal-move, and selected-preview submission controls
   remained disabled throughout the spectator flow.

## Bounded Evidence

- C# watchdog limit: 180 seconds.
- Successful child-process duration: about 35 seconds including process
  cleanup; scenario duration recorded by the driver: 31.737 seconds.
- Watchdog result: `TimedOut=False`, exit code `0`.
- No `ChessOnlineApp` process remained after cleanup.
- Per-client sanitized reports and the aggregate result were written only under
  ignored `.tmp/manual-smoke/`.
- Reports contain explicit redaction booleans, state fingerprints, role, and
  sequence only. They contain no access token, refresh token, password,
  authorization-header value, or private-key material.

## Observations

An intentionally interrupted diagnostic attempt left one matchmaking entry
long enough for the next first player to consume it. The bounded driver now
distinguishes queued from immediate `MatchFound` and refuses to call an
unrelated seat pair a pass. A lobby refresh also needed to tolerate short
eventual-consistency delay. These observations feed the following spectator
and matchmaking lifecycle audit; no server configuration or game rule was
changed in this phase.

## Isolation

The run did not change nginx, systemd, UFW, firewall rules, TLS/443, x-ui/Xray,
Outline, Albatronix, Unreal SYServer, PostgreSQL containers, native ABI, or any
of the five rule profiles.
