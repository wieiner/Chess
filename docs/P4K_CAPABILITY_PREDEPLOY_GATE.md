# P4K Capability Predeploy Gate

Date: 2026-07-11

## Goal

Before deploying a fresh server package to Hetzner, the local repository must prove that the package it is about to build contains the P4J/P4K hub surface:

- `RequestResumeMatch`;
- `JoinSpectator`;
- `RequestLobbySnapshot`.

This is a local pre-deploy gate. It does not claim that the public HTTP 80 server already supports these methods until a package is deployed and public diagnostics/smoke prove it.

## Local Capability Source

`src/ChessOnlineProtocol/OnlineRoomRegistry.cs` reports:

```text
ResumeMatchSupported = true
SpectatorModeSupported = true
LobbySnapshotSupported = true
```

and includes the following in `SupportedHubMethods`:

```text
RequestResumeMatch
JoinSpectator
RequestLobbySnapshot
```

`src/ChessOnlineServer/Chess3DRelayHub.cs` implements public hub methods:

- `RequestResumeMatch(OnlineProtocolMessage message)`;
- `JoinSpectator(OnlineProtocolMessage message)`;
- `RequestLobbySnapshot(OnlineProtocolMessage message)`.

`src/ChessOnlineClient/ChessOnlineRelayClient.cs` exposes matching client calls:

- `RequestResumeMatchAsync`;
- `JoinSpectatorAsync`;
- `RequestLobbySnapshotAsync`.

## Contract Test Gate

`tests/ChessOnlineContractTests/Program.cs` checks that:

- `OnlineDiagnostics.ResumeMatchSupported` is true;
- `OnlineDiagnostics.SpectatorModeSupported` is true;
- `OnlineDiagnostics.LobbySnapshotSupported` is true;
- `SupportedHubMethods` contains all three method names;
- protocol DTOs for resume, spectator, and lobby serialize without token fields;
- active in-memory resume returns snapshot/action log without mutating state;
- spectator join returns snapshot/action log and spectator submit is rejected as read-only/no-seat;
- active lobby snapshot exposes safe table rows without secrets.

## Public Server Gap

The Phase 00 baseline showed the current public Hetzner package still lacks:

- `RequestResumeMatch`;
- `JoinSpectator`;
- `RequestLobbySnapshot`.

Therefore remote resume/spectator/lobby are blocked by package freshness until P4K deploy phases update `/opt/chessonline/server`.

## PASS Criteria For Later Deploy

After deployment, `/chess3d/diagnostics` must show:

```json
{
  "resumeMatch": true,
  "spectatorMode": true,
  "lobbySnapshot": true,
  "supportedHubMethods": [
    "...",
    "RequestResumeMatch",
    "JoinSpectator",
    "RequestLobbySnapshot",
    "..."
  ]
}
```

Remote operator smoke must then prove:

- resume current match;
- join spectator read-only;
- request lobby snapshot;
- Classic and Asgard legal-preview play still pass.

## Phase 03 Verification

Commands:

```powershell
rg -n "RequestResumeMatch|JoinSpectator|RequestLobbySnapshot|ResumeMatchSupported|SpectatorModeSupported|LobbySnapshotSupported" src tests
dotnet build src\ChessOnlineServer\ChessOnlineServer.csproj -c Release
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipBenchmark -MSBuildMaxCpuCount 1 -GlobalTimeoutSeconds 300
git diff --check
```

Expected status: local pre-deploy capability gate PASS, remote PASS deferred until server package deployment.
