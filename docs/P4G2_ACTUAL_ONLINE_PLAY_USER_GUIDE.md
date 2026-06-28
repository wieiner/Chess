# P4G2 Actual Online Play User Guide

Date: 2026-06-28

## Boundary

ChessOnline is currently a diagnostic/dev deployment over public HTTP 80. Do not use real passwords. Use only temporary users created by the app or smoke tool.

This guide does not use or change 443, TLS, x-ui/Xray, Outline, Albatronix, Unreal, Nginx, UFW, or systemd.

## Server Health

```powershell
curl.exe http://178.105.220.117/healthz/live
curl.exe http://178.105.220.117/healthz/ready
curl.exe http://178.105.220.117/chess3d/diagnostics
```

Expected:

- live returns `Healthy`;
- ready returns JSON with `profileCount=5`;
- diagnostics reports Linux native authority, `libChess3DEngine.so`, auth enabled, and `requestLegalPreview=true`.

## Build The UI

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
```

## Run The UI

```powershell
.\src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe
```

Open the `3D Relay` tab.

## One-App Test Pair

This flow creates two temporary users inside one app instance. The primary user is controlled by the visible board.

1. Click `Use Hetzner HTTP`.
2. Click `Check Health`.
3. Click `Check Diagnostics`.
4. Click `Create Two Test Players`.
5. Select `asgard-convergence-3d-8x8x8-v0.1` or `classic-six-side-3d-8x8x8-v0.1`.
6. Click `Create Test Match With Two Local Clients`.
7. Click `Ready Both`.
8. Click `Start Game`.
9. Click `Request Snapshot`.
10. Click an occupied source cell.
11. Wait for legal highlights and legal preview options.
12. Click a highlighted legal target, or choose an option and click `Submit Selected Preview Action`.
13. Click `Request Action Log`.
14. Optionally click `Save Session Report`.

PASS means the move is accepted, the board refreshes, `Accepted` increments, and the action log shows server notation.

## Two-Window Manual Play

Start two `ChessOnlineApp.exe` instances.

In both windows:

1. Open `3D Relay`.
2. Click `Use Hetzner HTTP`.
3. Click `Register Temp`.
4. Select the same profile.
5. Click `Manual Join Matchmaking`.

Then:

1. When both players are matched, click `Ready This Window` in both windows.
2. Click `Start This Window` in either window.
3. Click `Snapshot This Window` in both windows.
4. In Window A, click an occupied source and legal target.
5. In Window B, click `Request Action Log` or wait for realtime refresh.

PASS means both windows see the same room/table, Window A gets an accepted action, and Window B can observe the authoritative action log.

## Profile Coverage

| Profile | Current operator status |
| --- | --- |
| Classic Six-Side | Match, snapshot, server legal preview, and normal move submit pass. |
| Single-Side Training | One-player match/start/snapshot pass. Action submit is intentionally not claimed in the matrix smoke. |
| Asgard Convergence | Match, snapshot, server legal preview, and normal move submit pass. Core/fusion/reserve remain profile-specific UI work. |
| Rubik Convergence | Match/start/snapshot pass. Layer-turn actions have a dedicated disabled boundary panel and are not sent as normal moves. |
| Hodge Projection Duel | Match/start/snapshot pass. Projection actions have a dedicated disabled boundary panel and are not sent as normal moves. |

## Remote Smoke Command

Asgard full action:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 -BaseUrl "http://178.105.220.117" -ProfileId "asgard-convergence-3d-8x8x8-v0.1" -TimeoutSeconds 180 -NoSecretLog
```

Classic full action:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 -BaseUrl "http://178.105.220.117" -ProfileId "classic-six-side-3d-8x8x8-v0.1" -TimeoutSeconds 180 -NoSecretLog
```

Snapshot-only profiles:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 -BaseUrl "http://178.105.220.117" -ProfileId "single-side-3d-8x8x8-v0.1" -TimeoutSeconds 180 -NoSecretLog -SkipActionSubmit
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 -BaseUrl "http://178.105.220.117" -ProfileId "rubik-convergence-3d-8x8x8-v0.1" -TimeoutSeconds 180 -NoSecretLog -SkipActionSubmit
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 -BaseUrl "http://178.105.220.117" -ProfileId "hodge-projection-duel-3d-8x8x8-v0.1" -TimeoutSeconds 180 -NoSecretLog -SkipActionSubmit
```

## Common Errors

- `HTTP diagnostic-only`: expected for this phase; do not use real credentials.
- `Legal preview stale`: request a fresh snapshot.
- `Primary player cannot submit now`: it is not your turn or seating is not established.
- `Rubik layer action requires...`: use the Rubik boundary panel; generic board submit will not send layer turns.
- `Hodge projection action requires...`: use the Hodge boundary panel; generic board submit will not send projection composites.

