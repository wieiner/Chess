# Chess2D UCI Interoperability Result

The P4M UCI gate launches `ChessUci.exe` as an external redirected subprocess. It does not call parser, position, or search classes directly.

Covered transcripts:

- `uci` identity/options/`uciok` and `isready`;
- start position and six-field FEN;
- legal coordinate move lists and illegal-move diagnostics;
- `go depth`, `go movetime`, `go nodes`, and `go infinite` plus `stop`;
- repeated search with exactly one `bestmove` per completed generation;
- real final telemetry whose one-move PV matches `bestmove`;
- malformed command recovery;
- terminal `bestmove 0000`;
- clean `quit`, bounded per-step waits, and process-tree cleanup;
- stdout restricted to UCI protocol prefixes while diagnostics remain on stderr.

The executable advertises only `MoveOverhead` and `OwnBook`. Hash sizing, thread count, seldepth, pondering, and long principal variations are not claimed by the current backend.

Run the focused gate with:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Only ChessUciSubprocessTests -SkipSolutionBuild -SkipBenchmark
```
