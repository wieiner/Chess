# P4I Online Layer Navigation

Date: 2026-06-28

## Scope

This phase improves the existing compact `ChessOnlineApp` online board. It does not change the server, SignalR protocol, native engine, rule profiles, or action legality.

## Problem

The online board renders one 8x8 Z layer at a time. In Chess3D, a legal preview from a selected source can produce targets on another Z layer. Before this phase, the player could see a successful legal-preview count but no visible highlighted cells on the current slice.

## Changes

The board panel now shows:

- occupied cell count per Z layer;
- legal target count per Z layer;
- capture and special target counts per Z layer;
- quick `Legal Z...` buttons for layers that contain legal targets;
- automatic focus to the nearest legal-target layer when the current layer has no preview targets.

## Authority Boundary

Layer navigation is UI-only:

- occupied counts come from `OnlineChess3DBoardSnapshot`;
- legal/capture/special counts come from server-provided `LegalPreviewState.Targets`;
- no local pseudo-move generation is used;
- unsupported special actions are still not submitted as `NormalMove`.

## Verification

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 -GlobalTimeoutSeconds 300
```
