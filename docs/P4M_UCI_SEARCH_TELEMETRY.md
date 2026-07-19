# Chess2D UCI Search Telemetry

`ChessUci` emits one final `info` line immediately before the matching `bestmove`.

The line contains only values backed by `Chess_GetLastSearchStats`: completed `depth`, `score cp`, `nodes`, elapsed `time`, and NPS calculated from the reported nodes/time. The one-move `pv` is the legal best move that the search clone actually committed.

When the native post-move state is checkmate, the adapter can truthfully emit `score mate 1`. It does not infer longer mate distances. The current native ABI does not expose selective depth or a multi-move principal variation, so `seldepth` and longer PV fields are intentionally absent.

A terminal position emits `info string no legal move` followed by `bestmove 0000`. A generation guard prevents telemetry or bestmove from an obsolete search from appearing after a replacement search/position command.
