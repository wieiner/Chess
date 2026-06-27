# P4G2 Phase 14 - ChessOnline Server Capabilities

Date: 2026-06-27

## Purpose

The public Hetzner deployment can be healthy while still being older than the local client/hub contract. P4G2 adds append-only capability fields so operators and clients can tell whether a deployed server supports online legal preview and realtime resync behavior.

## Endpoint

Existing endpoint:

```text
GET /chess3d/diagnostics
```

The endpoint keeps all previous fields and now also reports:

- `serverCommit`
- `requestLegalPreview`
- `realtimeResync`
- `actionLog`
- `matchmaking`
- `supportedHubMethods`

The feature flags are mirrored in `OnlineDiagnostics`, so SignalR diagnostics responses carry the same support information.

## Current Flags

The current server build reports:

```json
{
  "requestLegalPreview": true,
  "realtimeResync": true,
  "actionLog": true,
  "matchmaking": true,
  "supportedHubMethods": [
    "Hello",
    "JoinMatchmaking",
    "CancelMatchmaking",
    "GetMatchmakingStatus",
    "Ready",
    "StartGame",
    "SubmitAction",
    "RequestSnapshot",
    "RequestActionLog",
    "RequestLegalPreview",
    "RequestDiagnostics",
    "Ping"
  ]
}
```

## Compatibility

This is append-only. Existing diagnostics fields such as `protocolId`, `protocolVersion`, `authorityNativeLibraryName`, `authEnabled`, `roomCount`, and action counters remain unchanged.

Older deployments do not expose these fields. The smoke tool therefore still treats missing capability fields as "unknown" and can use documented compatibility fallback actions for Classic and Asgard until the updated server is deployed.

## Verification

Local verification:

- `dotnet build src\ChessOnlineServer\ChessOnlineServer.csproj -c Release`
- `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 -GlobalTimeoutSeconds 300`

Contract tests verify:

- legal preview capability flag;
- realtime resync capability flag;
- action log capability flag;
- matchmaking capability flag;
- `RequestLegalPreview` is listed in `SupportedHubMethods`.
