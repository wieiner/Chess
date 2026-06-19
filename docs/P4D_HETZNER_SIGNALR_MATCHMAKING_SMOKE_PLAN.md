# P4D Hetzner SignalR Matchmaking Smoke Plan

P4D phase 11 defines the next remote smoke once the Linux-native server package and Kestrel-only smoke are real.

This phase does not start a public service and does not run SignalR against Hetzner.

## Prerequisites

Before running this plan:

1. `libChess3DEngine.so` must build on Linux.
2. `ChessOnlineServer` must publish as a valid linux-x64 package.
3. Kestrel-only local-host smoke on the VPS must pass:
   - `/healthz/live`;
   - `/healthz/ready`;
   - `/chess3d/diagnostics`.
4. Auth test users must be ephemeral.
5. No secrets may be committed.

## Smoke sequence

Target profile: Asgard convergence.

1. Register player A.
2. Register player B.
3. Login or use returned protected tokens.
4. Open SignalR connection for player A.
5. Open SignalR connection for player B.
6. Player A enters Asgard matchmaking.
7. Player B enters Asgard matchmaking.
8. Assert match-found room/table exists.
9. Both players ready/start if required by current protocol flow.
10. Request authoritative snapshot.
11. Build or submit one legal Asgard action.
12. Verify:
    - accepted action response;
    - action log has one new event;
    - state hash changes or remains consistent according to the action;
    - no credential leakage in diagnostics.

## Expected APIs/components

- HTTP auth endpoints from the existing identity flow.
- `/chess3d/relay` SignalR hub.
- `OnlineRoomRegistry` / matchmaking flow already covered by local contract tests.
- Native authority diagnostics from P4D phase 04.

## Non-goals

- No nginx/TLS/public exposure in this smoke.
- No Redis/backplane.
- No Kubernetes/Docker orchestration.
- No production persistence paths.
- No long-running systemd service.

## Failure reporting

A failure report should include:

- sanitized base URL;
- profile id;
- step index;
- HTTP status or SignalR error;
- server diagnostic summary;
- no passwords, tokens, private keys, cookies, certificates, or raw keyring/store data.
