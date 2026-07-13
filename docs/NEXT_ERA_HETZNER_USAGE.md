# Next Era Hetzner Usage

Date: 2026-06-27

Host placeholder: `<HETZNER_HOST>`.

This is the short operator note for the current single-server ChessOnlineServer deployment. It is not a production security runbook.

## Current state

- `chessonline.service` is active.
- Nginx is active.
- Kestrel listens on `127.0.0.1:5077`.
- Nginx listens on `0.0.0.0:80`.
- `ufw` allows `80/tcp`.
- External HTTP health works.
- Linux native authority works with `/opt/chessonline/server/libChess3DEngine.so`.
- Exactly five Chess3D RuleProfiles are loaded.
- P4K play, resume, spectator, lobby, realtime resync, and legal preview are deployed and remotely verified.
- Dependency-aware readiness and conservative request limits are active.
- 443/TLS is deferred and must not be changed in this smoke phase.

## Health checks

```powershell
curl.exe http://<HETZNER_HOST>/healthz/live
curl.exe http://<HETZNER_HOST>/healthz/ready
curl.exe http://<HETZNER_HOST>/chess3d/diagnostics
```

Expected:

- live returns `Healthy`;
- ready returns JSON with `profileCount: 5`;
- diagnostics reports `authorityPlatform: Linux`, `authorityIsSupported: true`, and `authorityNativeLibraryName: libChess3DEngine.so`.

## SignalR Asgard smoke

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://<HETZNER_HOST>" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -TimeoutSeconds 180 `
  -NoSecretLog
```

The script writes ignored logs under:

```text
.tmp/test-logs
```

It generates temporary users and passwords at runtime and does not print access or refresh tokens.

## Playable Windows Client

P4F adds a hands-on Windows client path in:

```text
src/ChessOnlineApp
```

Build and run:

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
.\src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe
```

In the app:

1. Click `Use Hetzner HTTP`.
2. Click `Check Health`.
3. Click `Check Diagnostics`.
4. Click `Create Two Test Players`.
5. Select `asgard-convergence-3d-8x8x8-v0.1`.
6. Click `Create Test Match With Two Local Clients`.
7. Click `Ready Both`.
8. Click `Start Game`.
9. Click `Request Snapshot`.
10. Click an occupied current-side source and one highlighted legal target.
11. Confirm the authoritative action log/sequence/hash update.
12. Click `Save Session Report` only for an ignored local diagnostic report.

For explicit resume use `Disconnect Primary Relay`, `Reconnect Primary Relay`,
then `Resume Current Match`. Spectators use `Spectator`, `Join as Spectator`,
and `Follow Last Move`. Lobby discovery uses `Refresh Lobby`,
`Use Selected For Spectator`, and `Spectate Selected`.

See `docs/P4K_REMOTE_UX_USER_GUIDE.md` for the complete user flow and
`docs/P4K_HETZNER_OPERATOR_GUIDE.md` for deploy/rollback operations.

## Dry run

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://<HETZNER_HOST>" `
  -DryRun
```

## Security boundary

Public HTTP is diagnostic-only. Do not use real accounts or long-lived credentials until a domain, TLS, HTTPS-only token policy, renewal, backup, and rollback plan are complete.
