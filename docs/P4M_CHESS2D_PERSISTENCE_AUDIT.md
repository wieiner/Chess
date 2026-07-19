# P4M Chess2D Persistence Audit

## Purpose

Chess2D currently plays and edits positions reliably, but it does not have a durable game model. P4M must keep four different concerns separate:

| Concern | Meaning | Authority |
|---|---|---|
| FEN | One chess position | Native `ChessEngine` position state |
| PGN | One chess game and its move sequence | Structured game record plus SAN movetext |
| Session JSON | Restorable application workflow | App settings plus game record and current position |
| UCI | Process protocol between a GUI/controller and an engine | Standalone line-oriented executable |

FEN is not a game archive, PGN is not an application settings file, session JSON is not an interchange substitute for PGN, and UCI is not a persistence format.

## Sources checked

- Steven J. Edwards, [Portable Game Notation Specification and Implementation Guide](https://www.saremba.de/chessgml/standards/pgn/pgn-complete.htm), especially tag pairs, movetext, SAN, termination markers, and FEN.
- Microsoft Learn, [System.Text.Json overview](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview), as the baseline managed JSON API for the later session format.
- Current repository sources: `ChessEngine.cpp`, `ChessEngine.h`, `NativeChessEngine.cs`, `MainWindow.xaml(.cs)`, and `ChessNetworkEndpoint.cs`.

## Native position representation

The native `Position` already contains every field needed for FEN round-trip:

- 64-square board and side to move;
- white/black king-side and queen-side castling rights;
- en-passant target square;
- halfmove clock;
- fullmove number;
- last committed move;
- private undo snapshots.

`Chess_SetFen` parses these position fields and replaces the active position only after parsing succeeds. `Chess_GetFen` exports the current position. `ChessStateDto` exposes side, status, check, clocks, legal count, last move, repetition and draw-claim information.

The engine also owns draw-rule configuration, tablebase path/metadata, and search diagnostics. These are not encoded in standard FEN.

## Native move and undo representation

The internal `Move` stores source, target, promotion, flags, score, moved piece, and captured piece. Public `ChessMoveDto` omits moved/captured piece identity and pre/post positions. It is adequate for invoking a move and presenting basic status, but not for rebuilding a professional record independently.

Every committed move pushes a pre-move native `Snapshot`. It includes board, side, castling rights, en-passant, clocks, and last move. Search and legal-generation copies do not add history. `Chess_Undo` restores and removes the most recent snapshot.

Important limitations:

- native history is not enumerable through the public ABI;
- it stores position snapshots, not PGN headers or SAN records;
- loading FEN starts a position workflow and clears prior native history as part of the parsed position;
- redo is absent;
- the WPF list is not linked to an immutable record identity.

## ChessApp state today

### Move list

`MainWindow` appends display strings directly to `MoveList`. The current notation is long coordinate form with capture, promotion, and check decoration. It does not implement SAN disambiguation, castling notation, checkmate suffix, comments, variations, or PGN result markers.

New game, FEN load, setup apply, and incoming network FEN clear the UI list. Undo removes one UI row after native undo succeeds. Consequently, the display list is transient and must not become the persistence source of truth.

### Board and appearance

The following state is currently held only in controls or fields:

- board orientation from `PlayerSideBox`;
- 2D piece theme;
- 2D/Viewport3D mode;
- selected model-set path;
- 3D camera yaw, pitch, and distance;
- setup-mode board and selected setup side;
- selected square and transient legal targets.

The selected model set is discovered at runtime and is not saved. Absolute discovered paths must not be written into portable session files; a semantic set ID is required.

### Search and draw settings

Search configuration is read from WPF controls: depth, time limit, automatic depth, quiescence, transposition table, ordering, piece-square evaluation, king safety, optional GPU evaluation, endgame tables, opening randomness, and opening ply window. Draw-rule preferences and tablebase path are also UI-configured.

These settings do not belong in PGN movetext. A session may preserve them as optional app preferences. An exported PGN may use standard tags such as `TimeControl` when real game-clock data exists, but engine search limits are not a player time control.

### Clocks

Chess2D currently has no running white/black game clock model. The native halfmove clock is a draw-rule counter, not elapsed player time. P4M must not infer PGN clock annotations from it.

### TCP endpoint

The local endpoint stores listener/client/writer/cancellation runtime objects and exchanges JSON-lines `fen` and `move` messages. Host, port, connection status, sockets, and pending async work are runtime concerns.

A session may optionally remember a host/port preference, but it must never serialize live sockets, cancellation tokens, or peer state. PGN must contain none of this endpoint state.

## Format ownership

### FEN owns

- piece placement;
- active color;
- castling availability;
- en-passant target;
- halfmove clock;
- fullmove number.

It deliberately does not own move history, player names, event metadata, comments, search settings, model sets, UI orientation, or network state.

### PGN owns

- game headers, including the Seven Tag Roster;
- optional `SetUp` and `FEN` tags for a nonstandard start;
- ordered SAN movetext;
- comments/annotations when supported;
- one termination marker consistent with the game result.

PGN should not contain WPF layout, model paths, camera state, engine toggles, TCP configuration, tokens, or arbitrary autosave internals.

### Session JSON owns

- format/version metadata;
- the structured game record and current position;
- selected model-set ID and visual preferences;
- board orientation and optional camera state;
- search/draw preferences where useful;
- autosave/recovery metadata;
- optional non-secret endpoint preferences.

Transient selection, legal-target highlights, active tasks, handles, sockets, credentials, and generated model caches must be recomputed or omitted.

### UCI owns

- text commands and responses for engine process control;
- `position startpos` / `position fen ... moves ...` state reconstruction;
- search options and `go` limits;
- best-move and search-info output.

UCI must not depend on WPF, dialogs, application session JSON, or the local TCP endpoint.

## Safe P4M boundary

1. Introduce an immutable managed game-record model as the source for UI move rows, SAN, PGN, and replay.
2. Construct a record only after `Chess_TryMakeMove` or engine move succeeds, using pre/post FEN plus move DTO data.
3. Keep native FEN/legal/undo ABI unchanged. Add append-only getters only if a later phase proves required.
4. Make PGN import transactional by parsing and replaying on a temporary engine/session before replacing the visible game.
5. Keep autosave atomic and credential-free.
6. Build UCI as a separate console boundary over engine functionality, not as a mode inside ChessApp.
7. Refer to visual assets by catalog IDs and semantic roles, never absolute machine paths.
