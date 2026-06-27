# P4G2 Phase 17 - Hetzner Legal Preview Deploy Result

Date: 2026-06-27

## Scope

The updated ChessOnlineServer was deployed to Hetzner HTTP 80 with the `RequestLegalPreview` hub method and diagnostics capability flags.

Only the ChessOnlineServer payload was changed:

- `/opt/chessonline/server` was replaced.
- `chessonline.service` was restarted.
- Nginx was not modified.
- UFW/firewall was not modified.
- TLS/443 was not touched.
- x-ui/Xray, Outline, Albatronix Docker, and Unreal SYServer were not touched.

## Backup

Rollback backup created before deploy:

```text
/opt/chessonline/backups/server-before-p4g2-20260627-203832.tar.gz
```

## Deployment Package

The first deploy used:

```text
ChessOnlineServer-P4G2-e12b3b56b.tar.gz
```

That package exposed `RequestLegalPreview`, but remote Asgard smoke uncovered a persistence restart issue: after restart, matchmaking reused `match-1-asgard/table-1` while the JSON store still had an old action with `serverSeq=1`.

The server rejected the new accepted action persistence with:

```text
Duplicate action server sequence.
```

The fix was committed as:

```text
562b594fc P4G2 phase 17: fix persisted online action log reuse
```

The final deployed package was rebuilt from that commit:

```text
ChessOnlineServer-P4G2-562b594fc.tar.gz
```

The package includes:

- `ChessOnlineServer.dll`
- `ChessOnlineProtocol.dll`
- `ChessOnlinePersistence.dll`
- `libChess3DEngine.so`
- all five Chess3D RuleProfile JSON files
- server rule/scenario assets

It does not include runtime stores, keyrings, certificates, private keys, tokens, passwords, or raw logs.

## Service Result

After deploy:

```text
chessonline.service active (running)
Kestrel: http://127.0.0.1:5077
Nginx public HTTP 80 unchanged
```

Loopback health:

```text
GET http://127.0.0.1:5077/healthz/live -> Healthy
GET http://127.0.0.1:5077/healthz/ready -> ready, profileCount=5
```

Public health:

```text
GET http://178.105.220.117/healthz/live -> Healthy
GET http://178.105.220.117/healthz/ready -> ready, profileCount=5
```

Diagnostics now includes legal-preview capabilities:

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
  ],
  "authorityPlatform": "Linux",
  "authorityNativeLibraryName": "libChess3DEngine.so",
  "profileCount": 5
}
```

## Remote Smoke Results

Asgard:

```text
ProfileId: asgard-convergence-3d-8x8x8-v0.1
action-source=server-preview
notation=#1 S1 MOVE R (2,2,0)->(1,2,0)
SMOKE PASS
```

Classic:

```text
ProfileId: classic-six-side-3d-8x8x8-v0.1
action-source=server-preview
notation=#1 S1 MOVE K (4,4,0)->(3,5,1)
SMOKE PASS
```

No compatibility fallback was used after the final deploy.

## Local Verification For The Fix

Targeted checks:

```text
dotnet build src\ChessOnlineServer\ChessOnlineServer.csproj -c Release -> PASS
tests\run-tests.ps1 -Only ChessOnlineContractTests ... -> PASS
tests\run-tests.ps1 -Only ChessOnlineSignalRContractTests ... -> PASS
```

The new regression check verifies that `JsonOnlineStore.ClearActionLogAsync(tableKey)` allows a reused matchmaking table key to accept a fresh `serverSeq=1` after restart.

GitHub Actions for the hotfix:

```text
28301523986 -> success
```

## Security Boundary

The smoke used temporary users only. Tokens and passwords were not printed. HTTP 80 remains diagnostic/dev-only; TLS/domain/443 are deferred and were not touched.
