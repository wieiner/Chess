# P4J Online Match UX User Guide

Date: 2026-07-01

## Boundary

ChessOnline is still a diagnostic/dev deployment over HTTP 80. Use temporary users only. Do not enter real passwords until TLS/domain work is completed in a later phase.

P4J does not touch 443, TLS, x-ui/Xray, Outline, Albatronix, Unreal, nginx, systemd, UFW, or firewall state.

## Check Server

```powershell
curl.exe http://178.105.220.117/healthz/live
curl.exe http://178.105.220.117/healthz/ready
curl.exe http://178.105.220.117/chess3d/diagnostics
```

Expected today:

- live: `Healthy`
- ready: `profileCount=5`
- diagnostics: Linux native authority OK, `RequestLegalPreview` supported

Current public server limitation:

- `JoinSpectator` is not deployed yet.
- `RequestLobbySnapshot` is not deployed yet.
- `RequestResumeMatch` is not deployed yet.

Those methods exist in the repository and pass local/CI tests, but require a later Hetzner server package deployment.

## Build and Launch

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
.\src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe
```

## Play Through Public HTTP 80

1. Click `Use Hetzner HTTP`.
2. Click `Check Health`.
3. Click `Check Diagnostics`.
4. Click `Create Two Test Players`.
5. Select a profile in the matchmaking selector.
6. For current public Hetzner play, use Classic or Asgard first.
7. Click `Create Test Match With Two Local Clients`.
8. Click `Ready Both`.
9. Click `Start Game`.
10. Click `Request Snapshot`.
11. Click a source cell on the board.
12. Legal targets appear.
13. Click a legal target or choose a preview action.
14. Confirm accepted/rejected counts and action log update.

## Two-Window Manual Player Flow

1. Launch two `ChessOnlineApp` windows.
2. In both windows, click `Use Hetzner HTTP`, `Check Health`, and `Check Diagnostics`.
3. In each window, register a temporary user.
4. Set `Play Mode` to `Two-Window Manual Player`.
5. Join matchmaking with the same profile.
6. Ready/start once a match is found.
7. Request snapshots in both windows.
8. Move in the active-turn window.
9. Confirm the other window receives realtime snapshot/action-log updates.

## Resume

The repository now contains client and server resume support plus UI controls:

- `Disconnect Primary Relay`
- `Reconnect Primary Relay`
- `Resume Current Match`
- `Resume Selected`

Remote public Hetzner resume needs the updated server package that exposes `RequestResumeMatch`.

## Spectator

The repository now contains spectator server/client/UI support:

- `Spectator` play mode
- `Join as Spectator`
- `Request Snapshot`
- `Request Action Log`
- `Follow Last Move`
- `Save Spectator Report`

Spectator is read-only: Ready/Start/Submit buttons are disabled in spectator mode, and the server contract rejects mutation because no seat is assigned.

Remote public Hetzner spectator needs the updated server package that exposes `JoinSpectator`.

## Lobby

The repository now contains lobby snapshot server/client/UI support:

- ruleset filter with exactly five Chess3D profiles
- `Refresh Lobby`
- `Use Selected For Spectator`
- `Spectate Selected`
- `Resume Selected`
- safe table row display without tokens or connection ids

Remote public Hetzner lobby needs the updated server package that exposes `RequestLobbySnapshot`.

## Network Bug Reports

Use:

- `Save Network Bug Report`
- `Copy Network Summary`

Reports are saved under:

```text
.tmp/manual-smoke/p4j-network-report-YYYYMMDD-HHMMSS.json
```

The report includes reconnect, resume, spectator, lobby, legal-preview, action-log, and server capability summaries. Tokens/passwords/Authorization/private-key-like log lines are redacted.

Do not commit raw reports.

## Known Limitations

- Public Hetzner currently supports legal-preview play, matchmaking, snapshot, action log, and action submit.
- Public Hetzner does not yet expose resume/spectator/lobby hub methods.
- Rubik/Hodge special online action UX remains bounded and not mapped to normal moves.
- HTTP 80 is diagnostic/dev only.
- TLS/domain/443 remain future work.
