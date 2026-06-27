# P4F Manual Hetzner UI Smoke Result

Date: 2026-06-27

## Command-Line Smoke

The public HTTP smoke was run against the current Hetzner deployment:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://<HETZNER_HOST>" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -TimeoutSeconds 180 `
  -NoSecretLog
```

Result:

```text
STEP PASS health
STEP PASS register
STEP PASS login
STEP PASS SignalR connect
STEP PASS matchmaking room=match-2-asgard table=table-2
STEP PASS Asgard start hash=a0296f7e94a22346
STEP PASS Asgard action notation=#1 S1 MOVE P (2,3,0)->(2,3,1)
STEP PASS snapshot/actionlog finalHash=1116b19374131cc4
SMOKE PASS
```

The smoke used temporary users and did not print tokens or passwords.

## UI Smoke Click Path

The P4F UI path is now available in `ChessOnlineApp`:

1. Launch `ChessOnlineApp`.
2. Open the hosted SignalR/P3F area.
3. Click `Use Hetzner HTTP`.
4. Click `Check Health`.
5. Click `Check Diagnostics`.
6. Click `Create Two Test Players`.
7. Select `asgard-convergence-3d-8x8x8-v0.1`.
8. Click `Create Test Match With Two Local Clients`.
9. Click `Ready Both`.
10. Click `Start Game`.
11. Click `Request Snapshot`.
12. Click `Submit Safe Asgard Test Action`.
13. Click `Request Action Log`.
14. Click `Save Session Report`.

The UI code was build-verified locally. This document records the command-line remote smoke as executed and the UI click path as ready for operator manual validation.

## Known Limitations

- The P4F UI smoke was not auto-clicked by a WPF automation test in this phase.
- Session reports are written under ignored `.tmp/manual-smoke`.
- HTTP 80 remains diagnostic/dev only.
- TLS/domain/443 remain deferred.
- Full realtime 3D board integration is future P4G.

