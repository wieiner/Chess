# P4J Lobby Manual Smoke Result

Date: 2026-07-01

## Scope

Phase 21 verifies whether the new online lobby UI can be used against the current Hetzner HTTP 80 deployment.

This smoke does not touch TLS, 443, x-ui/Xray, Outline, Albatronix, Unreal, nginx, systemd, UFW, or firewall settings.

## Local Build

Command:

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
```

Result: PASS.

## Remote Health

Commands:

```powershell
curl.exe --connect-timeout 10 http://178.105.220.117/healthz/live
curl.exe --connect-timeout 10 http://178.105.220.117/healthz/ready
curl.exe --connect-timeout 10 http://178.105.220.117/chess3d/diagnostics
```

Observed result:

- `/healthz/live`: `Healthy`
- `/healthz/ready`: ready JSON, `profileCount=5`
- `/chess3d/diagnostics`: Linux native authority is supported and `requestLegalPreview=true`

## Lobby Capability Check

The deployed Hetzner server currently reports these supported hub methods:

- `Hello`
- `JoinMatchmaking`
- `CancelMatchmaking`
- `GetMatchmakingStatus`
- `Ready`
- `StartGame`
- `SubmitAction`
- `RequestSnapshot`
- `RequestActionLog`
- `RequestLegalPreview`
- `RequestDiagnostics`
- `Ping`

It does not yet report:

- `RequestLobbySnapshot`
- `JoinSpectator`
- `RequestResumeMatch`

Therefore the remote lobby manual smoke is BLOCKED by the deployed server package version. The local repository already contains the server/client/UI implementation for lobby snapshot, but Hetzner still needs a server package deployment that includes Phase 18+.

## Local UI Readiness

The ChessOnlineApp lobby UI is available locally:

- ruleset filter with the five real Chess3D profiles;
- Refresh Lobby;
- Use Selected For Spectator;
- Spectate Selected;
- Resume Selected;
- Join Player Selected;
- active table list and selected-row status.

The UI is capability-compatible with the Phase 18 server contract. It should not claim a remote PASS until `/chess3d/diagnostics` exposes `RequestLobbySnapshot`.

## Manual Click Path After Server Deployment

Once Hetzner is updated with Phase 18+ server code:

1. Launch `ChessOnlineApp`.
2. Click `Use Hetzner HTTP`.
3. Click `Check Health`.
4. Click `Check Diagnostics`.
5. Create or resume a test match in one app/window.
6. Open a second app/window.
7. Click `Refresh Lobby`.
8. Select the active table.
9. Click `Spectate Selected`.
10. Click `Request Snapshot`.
11. Click `Request Action Log`.
12. Save a sanitized session report under `.tmp/manual-smoke`.

## Result

Status: BLOCKED for remote lobby flow.

Reason: Current Hetzner deployment does not expose `RequestLobbySnapshot`.

No credentials, tokens, runtime stores, keyrings, or raw session logs were committed.
