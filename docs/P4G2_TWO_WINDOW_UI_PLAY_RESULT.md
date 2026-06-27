# P4G2 Phase 20 - Two-Window UI Play Result

Date: 2026-06-27

## Purpose

Verify that two independent `ChessOnlineApp` windows can connect to the Hetzner HTTP 80 server, join matchmaking as separate temporary players, start a match, and observe an accepted action through the peer action log path.

## Build

The app build from Phase 19 remained valid:

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
```

Result:

```text
PASS
```

## Automated Two-Window Click Path

Two separate `ChessOnlineApp.exe` processes were launched.

Window A:

1. Select `3D Relay`.
2. Click `Use Hetzner HTTP`.
3. Click `Register Temp`.
4. Click `Manual Join Matchmaking`.
5. Click `Ready This Window`.
6. Click `Start This Window`.
7. Click `Snapshot This Window`.
8. Click source cell `P4GCell_4_4_0`.
9. Click `Submit Selected Preview Action`.

Window B:

1. Select `3D Relay`.
2. Click `Use Hetzner HTTP`.
3. Click `Register Temp`.
4. Click `Manual Join Matchmaking`.
5. Click `Ready This Window`.
6. Click `Snapshot This Window`.
7. Click `Request Action Log`.

The default profile selector was left on:

```text
classic-six-side-3d-8x8x8-v0.1
```

Temporary automation log:

```text
.tmp\manual-smoke\p4g2-two-window-ui-automation-20260627.log
```

The raw log is ignored and is not committed.

## Server-Side Confirmation

After the two-window smoke, public diagnostics reported:

```json
{
  "requestLegalPreview": true,
  "roomCount": 6,
  "tableCount": 6,
  "activeConnections": 0,
  "connectionCount": 12,
  "lastServerSeq": 1,
  "acceptedActionCount": 6,
  "rejectedActionCount": 0,
  "resyncCount": 0,
  "protocolErrorCount": 0,
  "authorityPlatform": "Linux",
  "authorityNativeLibraryName": "libChess3DEngine.so",
  "profileCount": 5
}
```

This confirms that both app instances connected independently, the match flow reached the server, and the submitted legal-preview action was accepted without rejection.

## Boundary

This phase did not touch:

- 443/TLS/domain;
- x-ui/Xray;
- Outline;
- Albatronix Docker;
- Unreal SYServer;
- Nginx config;
- UFW/firewall.

Only temporary users were created. Tokens and generated passwords were not logged or committed.

## Known Limitations

- This is still HTTP 80 diagnostic/dev play, not production TLS play.
- The smoke verified Classic normal-move online flow. Asgard server-preview action is already covered by Phase 18 remote smoke.
- Rich two-window visual polish, reconnect/resume UX, spectator mode, and Rubik/Hodge special-action UI remain future work.
