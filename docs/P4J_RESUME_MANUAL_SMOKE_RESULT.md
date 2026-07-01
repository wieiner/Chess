# P4J Phase 10 - Resume Manual Smoke Result

Date: 2026-07-01

## Summary

`Resume Current Match` is implemented in the local client and builds successfully, but the real Hetzner HTTP 80 deployment cannot yet pass the resume manual smoke because the deployed server does not expose `RequestResumeMatch`.

Result: **blocked by deployed server version gap**.

This is not a ChessOnlineApp compile failure and not a local DTO/client failure. It is a deployment alignment issue: the local repository includes P4J Phase 08/09 resume code, while the currently running Hetzner service still reports an older hub method set.

## Local Build

Command:

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
```

Result: PASS.

The app contains:

- `Disconnect Primary Relay`;
- `Reconnect Primary Relay`;
- `Resume Current Match`;
- non-secret resume context in sanitized session reports.

## Hetzner Diagnostics

Command:

```powershell
curl.exe --max-time 15 http://178.105.220.117/chess3d/diagnostics
```

Relevant result:

- `requestLegalPreview=true`
- `realtimeResync=true`
- `actionLog=true`
- `matchmaking=true`
- `authorityIsSupported=true`
- `authorityPlatform=Linux`
- `authorityNativeLibraryName=libChess3DEngine.so`
- `authEnabled=true`
- `profileCount=5` via `/healthz/ready`

Missing for resume smoke:

- no `resumeMatch=true`;
- `supportedHubMethods` does not include `RequestResumeMatch`.

## Manual Flow Prepared

Once Hetzner is redeployed with P4J Phase 08+ server code, the intended one-app resume smoke is:

1. Launch `ChessOnlineApp`.
2. Click `Use Hetzner HTTP`.
3. Click `Check Health`.
4. Click `Check Diagnostics`.
5. Click `Create Two Test Players`.
6. Select `asgard-convergence-3d-8x8x8-v0.1` or `classic-six-side-3d-8x8x8-v0.1`.
7. Click `Create Test Match With Two Local Clients`.
8. Click `Ready Both`.
9. Click `Start Game`.
10. Click `Request Snapshot`.
11. Select an occupied source cell and submit one legal preview action.
12. Confirm action accepted and action log refreshes.
13. Click `Disconnect Primary Relay`.
14. Confirm reconnect status changes.
15. Click `Reconnect Primary Relay`.
16. Click `Resume Current Match`.

Expected after server redeploy:

- `Resume succeeded`;
- authoritative snapshot returned;
- action-log tail returned;
- no action submitted during resume;
- board state remains server-authoritative.

## Two-Window Flow

Two-window resume smoke was not executed in this phase because the deployed server lacks the resume hub method. It should be repeated after the server advertises `resumeMatch=true`.

## Security Boundary

This result document contains no access tokens, refresh tokens, temporary passwords, runtime stores, keyrings, private keys, or raw manual smoke logs.

HTTP 80 remains diagnostic/dev only. Use temporary users only.

## Next Required Step

Deploy a ChessOnlineServer package that includes:

- `OnlineRoomRegistry.RequestResumeMatch`;
- `Chess3DRelayHub.RequestResumeMatch`;
- diagnostics `resumeMatch=true`;
- `supportedHubMethods` containing `RequestResumeMatch`.

After that deployment, rerun this manual smoke and replace the status with PASS/FAIL evidence.
