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
10. Click `Submit Safe Asgard Test Action`.
11. Click `Request Action Log`.
12. Click `Save Session Report`.

See `docs/P4F_PLAYABLE_ONLINE_USER_GUIDE.md` for the full operator flow.

## Dry run

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://<HETZNER_HOST>" `
  -DryRun
```

## Security boundary

Public HTTP is diagnostic-only. Do not use real accounts or long-lived credentials until a domain, TLS, HTTPS-only token policy, renewal, backup, and rollback plan are complete.
