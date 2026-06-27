# P4G2 Phase 18 - Remote Legal Preview Smoke Result

Date: 2026-06-27

## Purpose

Verify that the public Hetzner HTTP 80 ChessOnlineServer now supports `RequestLegalPreview` and that the operator smoke tool submits actions selected from server legal preview, not compatibility fallback.

## Server

Base URL:

```text
http://178.105.220.117
```

Diagnostics confirmed:

```text
requestLegalPreview=true
supportedHubMethods includes RequestLegalPreview
authorityPlatform=Linux
authorityNativeLibraryName=libChess3DEngine.so
profileCount=5
```

TLS/443, x-ui/Xray, Outline, Albatronix, Unreal, Nginx config, and firewall were not touched.

## Asgard Smoke

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://178.105.220.117" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -TimeoutSeconds 180 `
  -NoSecretLog `
  -BuildSmokeTool
```

Result:

```text
STEP PASS matchmaking room=match-3-asgard table=table-3
STEP PASS game start hash=a0296f7e94a22346
STEP PASS action-source=server-preview source=(2,2,0) side=1 kind=NormalMove from=(2,2,0) to=(1,2,0)
STEP PASS profile action notation=#1 S1 MOVE R (2,2,0)->(1,2,0)
STEP PASS snapshot/actionlog finalHash=e8443902f01a9450
SMOKE PASS
```

## Classic Smoke

The first Classic attempt was blocked locally by a parallel build file lock in `VBCSCompiler` while Asgard smoke was building the same client dependency. That was not a server failure. The smoke was rerun without `-BuildSmokeTool` using the already built tool.

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://178.105.220.117" `
  -ProfileId "classic-six-side-3d-8x8x8-v0.1" `
  -TimeoutSeconds 180 `
  -NoSecretLog
```

Result:

```text
STEP PASS matchmaking room=match-4-classic table=table-4
STEP PASS game start hash=525757d8c254b919
STEP PASS action-source=server-preview source=(4,4,0) side=1 kind=NormalMove from=(4,4,0) to=(3,5,1)
STEP PASS profile action notation=#1 S1 MOVE K (4,4,0)->(3,5,1)
STEP PASS snapshot/actionlog finalHash=679085fef5801b2a
SMOKE PASS
```

## Conclusion

Remote legal preview is live on Hetzner for the deployed server. Asgard and Classic both pass matchmaking, start, server legal preview, action submit, snapshot refresh, and action log retrieval through public HTTP 80.

Compatibility fallback was not used in the passing post-deploy smokes.

Temporary users were used and no tokens/passwords were committed.
