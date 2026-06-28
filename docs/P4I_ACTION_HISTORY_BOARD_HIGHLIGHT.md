# P4I Action History Board Highlight

Date: 2026-06-28

## Scope

This phase makes the online action history easier to inspect in `ChessOnlineApp`. It is UI-only and does not change the server, SignalR protocol, native engine, rule profiles, save/replay, or action legality.

## Changes

When the player selects an action-log row whose notation contains coordinate pairs like `(x,y,z)->(x,y,z)`, the board now:

- highlights the selected action source with `f`;
- highlights the selected action target with `t`;
- switches to the target Z layer so the destination is visible;
- keeps current manual `F/T` submit cells unchanged.

## Safety

The history highlight is read-only:

- it does not submit actions;
- it does not change `_p4gMoveFrom` or `_p4gMoveTo`;
- it is cleared when the session or action log is cleared;
- it does not parse or store tokens/passwords.

## Verification

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 -GlobalTimeoutSeconds 300
```
