# P4F Playable Online User Guide

Date: 2026-06-27

This guide describes the current diagnostic/dev online client flow over public HTTP 80.

Do not use real passwords over HTTP. TLS/domain/443 are deferred.

## Check Server

```powershell
curl.exe http://<HETZNER_HOST>/healthz/live
curl.exe http://<HETZNER_HOST>/healthz/ready
curl.exe http://<HETZNER_HOST>/chess3d/diagnostics
```

Expected:

- live: `Healthy`;
- ready: JSON with `profileCount: 5`;
- diagnostics: Linux native authority supported and `libChess3DEngine.so`.

## Run Command-Line Smoke

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://<HETZNER_HOST>" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -TimeoutSeconds 180 `
  -NoSecretLog
```

PASS means:

- health/ready/diagnostics passed;
- two temporary users were registered and logged in;
- both SignalR clients connected;
- Asgard matchmaking found a room/table;
- game started;
- one safe Asgard action was accepted;
- snapshot/action log were returned.

## Launch UI

Build:

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
```

Run:

```powershell
.\src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe
```

Visual Studio can also launch `ChessOnlineApp`.

## UI Click Path

1. Open `ChessOnlineApp`.
2. In `P3F hosted SignalR transport`, click `Use Hetzner HTTP`.
3. Click `Check Health`.
4. Click `Check Diagnostics`.
5. Click `Create Two Test Players`.
6. Select `asgard-convergence-3d-8x8x8-v0.1`.
7. Click `Create Test Match With Two Local Clients`.
8. Click `Ready Both`.
9. Click `Start Game`.
10. Click `Request Snapshot`.
11. Click `Submit Safe Asgard Test Action`.
12. Click `Request Action Log`.
13. Click `Save Session Report`.

Session reports are written to:

```text
.tmp/manual-smoke
```

They are runtime diagnostics and are not source files.

## Common Errors

`Health check failed`

- Server unreachable or base URL is still the placeholder.

`Register temp failed`

- Auth disabled, server not ready, or HTTP token policy rejected the request.

`SignalR connect failed`

- Hub URL unreachable, server down, or proxy WebSocket/long-polling issue.

`Matchmaking did not produce MatchFound`

- Only one client entered the queue, wrong profile id, or server-side matchmaking error.

`Safe test action is currently defined only for the Asgard profile`

- Select `asgard-convergence-3d-8x8x8-v0.1` for the P4F safe action helper.

## Security Boundary

- HTTP 80 is diagnostic/dev only.
- Do not use real accounts or passwords.
- Tokens are kept in memory and are not logged.
- TLS/domain/443 are deferred because 443 may be owned by a separate x-ui/Xray service.
- Do not modify Nginx, systemd, UFW, x-ui, Xray, Outline, Unreal, or Albatronix in P4F.

