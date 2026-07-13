# P4K WPF Spectator Smoke Result

Date: 2026-07-13

## Scope

Two independent `ChessOnlineApp` processes were driven through external WPF UI
Automation against the Hetzner HTTP 80 diagnostic deployment. The host process
created the temporary A/B match; the second process authenticated a separate
temporary C user and joined the displayed room/table as a spectator.

The transient driver, raw UI output, runtime room/table identifiers, and
spectator report remain under ignored `.tmp/manual-smoke`.

## Flow

Host A/B:

1. select Asgard and the Hetzner HTTP endpoint;
2. create two temporary players;
3. create the single-app test match;
4. ready both, start, and request the authoritative snapshot.

Spectator C:

1. select Asgard and the Hetzner HTTP endpoint;
2. register one independent temporary user;
3. select `Spectator` mode;
4. enter the room/table displayed by A;
5. join as spectator;
6. request snapshot and action log;
7. follow the last move.

Host A then selected occupied source `(2,2,0)` and clicked the legal-preview
target `(1,2,0)`. C observed the resulting realtime/action-log update and used
`Follow Last Move` to refresh the authoritative state.

## Result

Result: **PASS** in 17.24 seconds inside a 150-second external watchdog.

- C showed the explicit `SPECTATOR`/read-only status and no player seat;
- Ready Both, Ready This Window, Start Game, Start This Window, safe Asgard
  submit, normal move submit, and selected-preview submit were all disabled;
- C received a live sequence/hash update after A's accepted action;
- C action log contained the accepted move;
- selecting the history entry did not enable submit;
- initial spectator hash: `a0296f7e94a22346`;
- updated spectator hash: `e8443902f01a9450`;
- refreshed snapshot reported one action;
- no WPF process remained after cleanup.

## Report and Security

`Save Spectator Report` produced the expected v0.1 JSON under ignored `.tmp`.
The report identified itself as HTTP diagnostic-only, declared token/password
redaction, contained one action-log item, and had zero matches for token,
authorization, password-value, private-key, or SSH-key field patterns.

No real credentials were used or retained. No server/network/service setting was
changed. TLS/443 and neighboring Hetzner services remain outside this phase.
