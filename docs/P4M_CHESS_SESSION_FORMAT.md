# Chess2D Session Format v1

`*.chesssession.json` is the application session format for restoring a Chess2D work state. It complements PGN; it does not replace PGN as the portable game-notation format.

## Identity and limits

- `format` is `chess2d-session`.
- `version` is `1.0`.
- UTF-8 JSON is bounded to 4 MiB by the runtime reader.
- The normative structural contract is `assets/rules/chess2d/chess-session-v1.schema.json`.
- Unknown top-level and nested properties are rejected in v1.

## State carried by a session

The document contains a session UUID, creation/update timestamps, starting and current six-field FEN, the structured main-line move records, PGN headers, result and termination, optional clock snapshots, board orientation, the selected 2D piece theme, a semantic 3D model-set ID, UI mode, engine/search options, dirty state, and optional autosave metadata.

Move records retain pre/post FEN, UCI and canonical SAN. A loader validates the complete chain before it may replace live state. Redo history is intentionally not persisted in v1.

## Presentation references

`modelSetId` is a catalog identifier such as `procedural`; it is never an absolute path. A missing model set cleanly falls back to the procedural renderer. `pieceTheme` and engine backend are bounded semantic names, not executable or asset paths.

## Security boundary

The format must never contain access tokens, refresh tokens, passwords, connection tokens, server credentials, private keys, certificates, runtime keyrings, or absolute local paths. The validator rejects path-shaped presentation identifiers. Session files are local game artifacts and are not an authentication store.

## Persistence contract

Phase 16 writes a temporary sibling file, flushes it to durable storage, re-reads and validates it, and then atomically replaces the destination. Existing files may be retained as `.bak`. Invalid or unsupported input is fail-closed and leaves the current game untouched.

## PGN relationship

Use PGN for exchanging a chess game and annotations. Use a session file when the Chess application should also restore presentation and engine settings. Exporting PGN from a loaded session remains deterministic because the game authority is the same structured move record.
