# P4I Board Readability

Date: 2026-06-28

## Scope

This phase improves the existing online snapshot grid in `ChessOnlineApp`. It does not embed the local Chess3D renderer and does not change the online protocol, server, native engine, or Chess3D rules.

## Changes

The online board grid now shows:

- coordinate headers for the current Z layer;
- a stable 9x9 layout with X/Y labels;
- compact cell markers:
  - `F` = selected source/from cell;
  - `T` = selected target/to cell;
  - `L` = legal target;
  - `X` = capture target;
  - `*` = special legal-preview target;
  - `S` = selected cell;
- piece labels next to markers;
- stronger border and color contrast for selected/legal cells;
- board status with dimensions, current side, current macro-player, server sequence, occupied count, legal target count, and state hash.

## Authority Boundary

The board remains authoritative-server-driven:

- cells come from `OnlineChess3DBoardSnapshot`;
- legal targets come from `LegalPreviewState`;
- no local pseudo-move generation is used;
- unsupported special actions are not converted into normal moves.

## Verification

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 -GlobalTimeoutSeconds 300
```
