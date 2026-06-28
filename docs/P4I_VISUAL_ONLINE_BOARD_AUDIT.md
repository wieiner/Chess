# P4I Visual Online Board Audit

Date: 2026-06-28

## Current State

`ChessOnlineApp` currently renders an authoritative online Chess3D snapshot as a compact `UniformGrid` layer view. The grid is intentionally simpler than the local `Chess3DWindow` renderer:

- it reads server snapshots through `OnlineChess3DBoardSnapshotParser`;
- it displays one Z layer at a time;
- it marks selected source/target cells;
- it highlights legal-preview targets;
- it submits only server-approved normal legal-preview actions through the generic board path;
- it keeps Rubik/Hodge/reserve special actions behind explicit boundaries.

This is reliable, but it still feels more like a diagnostic inspector than a polished game board.

## Existing Visual Sources

| Source | Current use | Notes |
| --- | --- | --- |
| `OnlineChess3DBoardSnapshot` | Board cells, ruleset, state hash, current side/macro-player | Must remain source of truth for online board visuals. |
| `LegalPreviewState` | Legal target highlights and option list | Authoritative server preview. |
| `P4FActionLogList` | Accepted action notation | Useful for move-history UI and last-move highlighting. |
| `OnlineSeatTurnState` | My turn / opponent turn status | Should stay visible near the board. |
| `OnlineRealtimeSyncState` | Duplicate/gap/resync status | Should remain compact and warning-colored when resync is needed. |

## Local Chess3D Renderer Boundary

The local Chess3D WPF renderer is richer, but embedding or coupling it directly into `ChessOnlineApp` is risky in this phase:

- local renderer assumes local engine/UI lifecycle;
- online board must not invent state not present in the server snapshot;
- profile-specific actions have different online dispatch semantics;
- richer 3D animation should not break the already-working snapshot/legal-preview path.

Decision: keep the grid as the product fallback and improve it incrementally before attempting richer 3D embedding.

## P4I Recommended Steps

1. Better board readability:
   - coordinate headers;
   - side-colored cells/pieces;
   - stronger selected/from/to/target/capture markers;
   - current layer and occupied count near board;
   - current turn near board.

2. Action history UI:
   - action notation list;
   - selected action status;
   - copy/export sanitized notation;
   - source/target hints where available.

3. Playability micro-polish:
   - automatic snapshot refresh after start;
   - automatic action-log refresh after accepted action;
   - automatic preview on source click;
   - clear preview after accepted action;
   - keep manual buttons as fallback.

4. Future richer visuals:
   - 3D board renderer or embedded Chess3D view;
   - layer navigation;
   - animated legal action hints;
   - Rubik/Hodge special action visuals.

## Non-Goals

- no new Chess3D RuleProfile;
- no server protocol rewrite;
- no native ABI changes;
- no local Chess3D rule changes;
- no changes to Hetzner deployment, Nginx, firewall, TLS, or 443.

## Verification Plan

Phase 32 is docs-only. Later P4I phases should run:

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 -GlobalTimeoutSeconds 300
```
