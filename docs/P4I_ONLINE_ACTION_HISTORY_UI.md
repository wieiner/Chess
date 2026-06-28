# P4I Online Action History UI

Date: 2026-06-28

## Scope

This phase improves the existing `ChessOnlineApp` action-log surface. It does not change server action history, replay format, native ABI, or Chess3D rules.

## UI Additions

The online tab now provides:

- selected action status text;
- `Copy Selected Action`;
- `Export Action Log`;
- existing `Save Session Report` remains available.

## Export Format

`Export Action Log` writes a sanitized JSON file under:

```text
.tmp/manual-smoke/p4i-online-action-log-YYYYMMDD-HHMMSS.json
```

The export contains:

- format/version;
- created UTC timestamp;
- ruleset id;
- room/table id;
- shortened player ids;
- snapshot hash;
- last server sequence;
- accepted/rejected counters;
- action log items as displayed in the UI;
- explicit secret-redaction markers.

## Secret Boundary

The export does not include:

- access tokens;
- refresh tokens;
- passwords;
- authorization headers;
- keyrings;
- runtime stores.

The `.tmp` output directory is ignored by Git.

## Verification

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 -GlobalTimeoutSeconds 300
```
