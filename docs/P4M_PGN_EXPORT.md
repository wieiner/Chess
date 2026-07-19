# P4M PGN Export

## Contract

`ChessGameRecords.PgnExporter` writes deterministic UTF-8 PGN from either a `PgnGame` or a structured `ChessGameRecord`. It has no WPF dependency and does not include application-only state.

Strict export requires:

- one instance of every Seven Tag Roster entry in canonical order;
- agreement between the `Result` tag and movetext termination marker;
- paired `SetUp "1"` and `FEN` tags for a nonstandard initial position;
- valid tag names, comments, NAG values, and SAN supplied by the structured history;
- unique tag names.

Custom tags from `ChessGameRecord` are sorted with ordinal comparison for reproducible output. A nonstandard initial FEN automatically adds `SetUp` and `FEN`. Tag quotes and backslashes are escaped. Files written through `WriteUtf8` use UTF-8 without a BOM.

## Movetext

The exporter writes move numbers, canonical SAN, optional brace comments, `$N` annotations, recursive annotation variations, and exactly one final result marker. Line wrapping defaults to 80 columns and can be configured from 40 to 240 columns.

The exporter does not infer SAN or validate move legality itself. Those are engine/SAN-history responsibilities established in P4M phases 04-07. PGN parsing and transactional legal replay are separate phases 10-13.

## Safety

Export fails without producing partial text when required tags are missing, tag names are duplicated, result values disagree, `SetUp/FEN` are inconsistent, or a brace comment contains `}`. Application sessions, credentials, local network state, model paths, and search caches are never part of PGN.
