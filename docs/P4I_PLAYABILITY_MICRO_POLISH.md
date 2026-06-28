# P4I Playability Micro Polish

Date: 2026-06-28

## Scope

This phase reduces repetitive manual steps in `ChessOnlineApp` while keeping all explicit refresh buttons available for diagnostics.

## Changes

The client already refreshed snapshots after successful start/action paths. This phase adds automatic action-log refresh after accepted actions:

- `Submit Safe Asgard Test Action`;
- manual `Submit Normal Move`;
- `Submit Selected Preview Action`.

The manual `Request Action Log` button remains available.

## Why

After a legal-preview action is accepted, the player needs to see:

- accepted notation;
- authoritative snapshot;
- action-log tail;
- latest server sequence;
- state hash.

Auto-refreshing the action log makes the online board feel more like a playable client and less like a diagnostic sequence of separate buttons.

## Safety

The change does not alter:

- server protocol;
- Chess3D rules;
- native ABI;
- save/replay formats;
- profile catalog;
- Hetzner deployment.

The action-log export remains sanitized and `.tmp` only.

## Verification

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 -GlobalTimeoutSeconds 300
```
