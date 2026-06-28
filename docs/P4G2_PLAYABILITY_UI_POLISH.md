# P4G2 Playability UI Polish

Date: 2026-06-28

## Scope

This phase makes the playable online client easier to read without changing Chess3D rules, SignalR contracts, server deployment, or the five-profile catalog.

## Compact Status Line

`ChessOnlineApp` now shows a compact status line near the online match controls:

```text
Status: server=<state> auth=<anonymous/temp-user> match=<room/table or none> turn=<my-turn/waiting> preview=<count> realtime=<ok/resync-needed> accepted=<n> rejected=<n> lastSeq=<seq>
```

The line is intentionally high-level. It lets an operator see whether the client is connected, authenticated, matched, waiting for turn, holding legal-preview options, synchronized with realtime messages, and receiving accepted/rejected action results.

## Security Boundary

The compact status does not include:

- access tokens;
- refresh tokens;
- temporary passwords;
- authorization headers;
- raw session reports.

Authentication is displayed only as `anonymous` or `temp-user`.

## Gameplay Boundary

The change is UI-only:

- no new game mode;
- no new Chess3D RuleProfile;
- no rules changes;
- no native ABI changes;
- no server contract changes;
- no deployment change.

Rubik, Hodge, and reserve special actions remain guarded by their dedicated boundaries and are not submitted as generic `NormalMove`.

## Verification

- Build `ChessOnlineApp`.
- Confirm disconnected startup renders the compact status without null reference failures.
- Confirm accepted/rejected counters continue to update the legacy counter line and compact line.
- Confirm no token/password text is added to UI status strings.
