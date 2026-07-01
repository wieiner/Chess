# P4J Phase 16 - Spectator Manual Smoke Result

Date: 2026-07-01

## Summary

Spectator UI is implemented locally, but the remote Hetzner deployment currently does not expose the `JoinSpectator` hub method. Therefore the remote spectator smoke is **blocked**, not passed.

This is an expected deployment alignment gap: local source contains Phase 13 server spectator support and Phase 15 UI controls, while the public HTTP 80 server is still an older package.

## Local Build

Command:

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
```

Result: PASS, 0 warnings, 0 errors.

## Hetzner Health

Commands:

```powershell
curl.exe --connect-timeout 10 http://178.105.220.117/healthz/live
curl.exe --connect-timeout 10 http://178.105.220.117/healthz/ready
curl.exe --connect-timeout 10 http://178.105.220.117/chess3d/diagnostics
```

Observed:

- `/healthz/live`: `Healthy`;
- `/healthz/ready`: ready JSON with `profileCount=5`;
- `/chess3d/diagnostics`: Linux native authority is supported;
- `requestLegalPreview=true`;
- `supportedHubMethods` does **not** include `JoinSpectator`;
- diagnostics does **not** expose `spectatorMode` yet.

## Manual Smoke Plan

Once Hetzner is deployed with Phase 13+ server code:

1. Launch `ChessOnlineApp`.
2. Use Hetzner HTTP.
3. Create/start a one-app or two-window match.
4. Copy room/table IDs.
5. Launch another `ChessOnlineApp`.
6. Use Hetzner HTTP.
7. Create/login a temporary user.
8. Select `Spectator`.
9. Paste room/table IDs.
10. Click `Join as Spectator`.
11. Request snapshot and action log.
12. Make a player action in the player window.
13. Confirm spectator can refresh/follow latest move and cannot Ready/Start/Submit.

## Security Boundary

No access tokens, refresh tokens, passwords, Authorization headers, private keys, keyrings, stores, certificates, or raw runtime logs are included in this result.

HTTP 80 remains diagnostic/dev-only.

## Status

- Local spectator UI: ready.
- Local spectator server/client contracts: covered by contract tests from prior phases.
- Remote spectator smoke: blocked until Hetzner server package includes `JoinSpectator`.
