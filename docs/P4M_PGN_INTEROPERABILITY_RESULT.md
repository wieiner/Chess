# P4M PGN Interoperability Result

The repository contains short, authored fixtures under `tests/fixtures/pgn`. They are purpose-built contract positions rather than copied game databases.

Legal native replay fixtures cover a standard checkmate, castling by both sides, en passant, promotion from `SetUp/FEN`, a zero-move stalemate, and annotated mainline input. Parser coverage preserves brace comments, `$1`, and a simple RAV while the first UI importer intentionally replays only the main line.

Negative fixtures cover malformed tags, illegal SAN, ambiguous SAN from a two-knight setup, and a mismatched result marker. Every negative fixture returns no candidate game and leaves the live native engine unchanged.

The fixture directory is copied into `Chess2DWorkflowContractTests` output and executed headlessly through the existing bounded test runner.
