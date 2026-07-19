# P4M SAN Verification Result

## Gate scope

The SAN stage gate verifies the composed Chess2D workflow, not only formatter examples:

- native legal move and move descriptor;
- canonical managed SAN generation;
- immutable move record commit;
- continuous pre/post FEN chain;
- native undo aligned with structured undo;
- coordinate replay from a fresh start;
- engine-backed checkmate, stalemate, and draw-claim outcomes.

## Local result

The Phase 04 native contracts pass descriptor fixtures for ordinary moves, capture, en passant, promotion, castling, all disambiguation modes, checkmate, invalid input, and no mutation.

The Phase 05 managed contracts pass 64 SAN formatting fixtures plus fail-closed validation. They cover pawn/piece moves, captures, file/rank/both disambiguation, both castlings, all promotions, check, mate, discovered/double-check notation, and en passant.

The Phase 07 `Chess2DWorkflowContractTests` uses the real `ChessEngine.dll` through the existing ChessApp wrapper. It verifies:

- `GetLegalMoves` and `Chess_GetMoveDescriptor` do not change FEN or add records;
- an illegal blocked rook move changes neither FEN nor history;
- `e4 e5 Nf3` produces a continuous canonical SAN/FEN record line;
- native undo and structured undo agree;
- recommit and full reset/replay reproduce the final FEN;
- Fool's mate ends with `Qh4#`, `BlackWin`, and `Checkmate`;
- stalemate creates a draw outcome without a fabricated move;
- a fifty-move claim remains ongoing before claim and becomes a draw only after `ClaimDraw`.

## Search boundary

The current `Chess_MakeBestMoveEx` API searches and commits the selected best move. It is not a non-mutating search-preview API, so every successful AI move is intentionally recorded. The existing non-committing preview paths are legal move enumeration and the new move descriptor; both are covered by no-mutation/no-record tests.

## Commands

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 `
  -Only Chess2DWorkflowContractTests -SkipSolutionBuild -SkipBenchmark `
  -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -GlobalTimeoutSeconds 300

pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 `
  -Suite Chess2D -SkipSolutionBuild -SkipBenchmark `
  -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -GlobalTimeoutSeconds 420
```

Generated logs remain under `.tmp\test-logs` and are not tracked.
