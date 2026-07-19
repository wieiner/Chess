# P4M PGN Import and Replay

PGN import is transactional. `NativeChessPgnImporter` parses one game, creates a separate native engine, applies `SetUp/FEN`, resolves every main-line SAN against current legal moves, executes it, and builds a continuous `ChessGameRecord`. The live WPF engine/history are replaced only after the complete candidate succeeds.

The ChessApp game panel provides `Open PGN`, `Save PGN`, and `Save As`. Imported comments are retained on move records; NAG and RAV remain in the parsed PGN document model, while the first UI import release replays only the main line. A failed parse, illegal/ambiguous SAN, invalid FEN, or inconsistent checkmate/stalemate result leaves the current game unchanged and displays a located error.

PGN is a game interchange format, not an application session. Engine settings, themes, models, network state, and credentials are intentionally excluded.
