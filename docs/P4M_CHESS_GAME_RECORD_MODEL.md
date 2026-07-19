# P4M Chess Game Record Model

## Goal

Chess2D needs one structured source of truth for move history. The WPF `MoveList` becomes a view of this model; SAN, PGN, replay, session recovery, and diagnostics consume the same committed records.

The model is managed, independent of WPF controls, and layered above the existing native legal-move engine. It records facts only after the engine accepts a move.

## Proposed types

```csharp
public sealed record ChessGameRecord(
    ChessGameHeaders Headers,
    ChessPositionRecord InitialPosition,
    IReadOnlyList<ChessMoveRecord> Moves,
    IReadOnlyList<ChessMoveRecord> RedoMoves,
    ChessGameResult Result,
    ChessTerminationReason Termination,
    DateTimeOffset CreatedUtc,
    DateTimeOffset ModifiedUtc);

public sealed record ChessMoveRecord(
    int PlyIndex,
    ChessSquare From,
    ChessSquare To,
    int MovedPiece,
    int CapturedPiece,
    int PromotionPiece,
    ChessCastleKind Castle,
    bool IsEnPassant,
    bool IsCapture,
    bool IsCheck,
    bool IsCheckmate,
    string PreMoveFen,
    string PostMoveFen,
    string Uci,
    string San,
    ChessClockSnapshot? Clock,
    string? Comment,
    ChessEvaluationMetadata? Evaluation);

public sealed record ChessPositionRecord(
    string Fen,
    int SideToMove,
    int HalfmoveClock,
    int FullmoveNumber);

public sealed record ChessGameHeaders(
    string Event,
    string Site,
    string Date,
    string Round,
    string White,
    string Black,
    IReadOnlyDictionary<string, string> AdditionalTags);

public sealed record ChessClockSnapshot(
    TimeSpan? WhiteRemaining,
    TimeSpan? BlackRemaining,
    TimeSpan? MoveElapsed);

public sealed record ChessEvaluationMetadata(
    int? Centipawns,
    int? MateIn,
    int? Depth,
    long? Nodes,
    string? PrincipalVariation);
```

Supporting value types:

```csharp
public readonly record struct ChessSquare(int File, int Rank);

public enum ChessCastleKind { None, KingSide, QueenSide }
public enum ChessGameResult { Ongoing, WhiteWin, BlackWin, Draw }
public enum ChessTerminationReason
{
    None,
    Checkmate,
    Stalemate,
    Repetition,
    FiftyMoveRule,
    SeventyFiveMoveRule,
    InsufficientMaterial,
    Agreement,
    Resignation,
    TimeForfeit,
    Abandoned,
    Unknown
}
```

Exact namespaces and file split may follow the existing project layout during implementation. Public serialized names will be versioned separately from CLR type names.

## Core invariants

### Committed moves only

A `ChessMoveRecord` is created only after a legal move has succeeded in the native engine. Selection, legal preview, search exploration, failed input, SAN probing, and temporary replay validation never create records.

The commit pipeline is:

1. capture pre-move FEN and legal context;
2. identify moved/captured pieces and candidate notation facts;
3. call the authoritative native move function;
4. on success, capture post-move FEN and resulting state;
5. produce canonical UCI and SAN;
6. append one immutable record;
7. update result/termination and UI projections.

If any postcondition fails, the application must restore the pre-move position or fail transactionally before exposing a partial record.

### Immutability

Committed records are immutable values. Editing a comment or evaluation creates a replacement record and a new aggregate snapshot; code must not mutate records already supplied to replay, export, autosave, or UI observers.

Collections are exposed as read-only snapshots. The implementation may use private mutable builders internally, but callers never receive a writable authority list.

### Position chain

For every active move at index `n`:

- `PlyIndex` equals its zero-based index in the committed line;
- its `PreMoveFen` equals the initial FEN for the first move or the previous move's `PostMoveFen`;
- applying its UCI coordinates legally to `PreMoveFen` yields `PostMoveFen`;
- the game current FEN equals the last `PostMoveFen`, or `InitialPosition.Fen` for an empty line.

Replay validates this chain and rejects mismatches without replacing the current visible session.

### Result consistency

`ChessGameResult.Ongoing` pairs with `TerminationReason.None`. A finished result derives from engine state or an explicit user event. PGN termination markers are rendered from `ChessGameResult`; they are not embedded in SAN strings.

Check and checkmate are distinct move facts. `IsCheckmate` implies `IsCheck`, while stalemate changes game result without adding a SAN check suffix.

## Move facts

- `MovedPiece` and `CapturedPiece` use the existing signed piece codes. `CapturedPiece` is zero when no capture occurred.
- En-passant records the actually removed pawn as `CapturedPiece`, even though the destination square was empty before the move.
- Promotion stores the resulting piece type while `MovedPiece` remains the pawn that moved.
- Castling stores one king move with an explicit castle kind; rook movement is derived from the rule and is not a second ply.
- `Uci` is lower-case coordinate notation such as `e2e4` or `e7e8q`.
- `San` is canonical, locale-independent SAN and never uses localized or Unicode piece labels.
- Comments are plain record metadata. PGN escaping is the exporter's responsibility.
- Evaluation data is optional diagnostics and does not affect legality or game result.

## Initial position and headers

`InitialPosition` always contains a normalized FEN. A standard-start game may omit `SetUp`/`FEN` during PGN export; a nonstandard start emits both according to the PGN contract.

The Seven Tag Roster has explicit fields. Additional tags preserve insertion order in the eventual implementation, even if the first CLR sketch uses an `IReadOnlyDictionary`. Duplicate tag names are rejected or normalized by the PGN document layer, not silently retained in `ChessGameHeaders`.

Header text, player names, and event metadata never determine engine legality.

## Undo, redo, and branching

The chosen model keeps two lines:

- `Moves`: active committed main line;
- `RedoMoves`: records removed by undo, ordered for deterministic replay.

Undo first calls native `Chess_Undo`. Only after native success does it remove the active tail record and place it at the front of `RedoMoves`. Redo replays the next record transactionally and validates its pre/post FEN before recommitting it.

Committing a different legal move after undo clears `RedoMoves`; this creates a new main-line branch. P4M does not silently preserve discarded branches as PGN RAV. A later variation editor can explicitly promote discarded branches into `PgnVariation` nodes.

New game and accepted FEN/setup replacement start a new `ChessGameRecord`; they do not pretend the prior game was undone. The UI should prompt or autosave according to the later session policy.

## Replay contract

Replay creates a temporary native engine, loads `InitialPosition.Fen`, and applies structured UCI coordinates in ply order. Each step verifies legality and the expected post-move FEN. Only a fully successful replay may replace the visible engine/game aggregate.

SAN is validated output, not replay input authority. PGN import resolves SAN against legal moves, then creates structured records; subsequent internal replay uses the resolved coordinates and FEN chain.

## UI projection

The WPF move list renders records rather than storing strings. A row can expose move number, white SAN, black SAN, selection state, comments, UCI, and pre/post FEN without becoming authority itself.

Copy SAN, copy UCI, jump-to-ply, PGN export, session autosave, and diagnostics all read record snapshots. Clearing or virtualizing the visual list must not destroy the game record.

## Persistence boundary

The game-record schema is embedded in versioned session JSON and mapped into PGN. It never serializes:

- native handles or undo-vector memory;
- WPF controls, brushes, meshes, or selected objects;
- active search tasks or transient previews;
- sockets, streams, cancellation tokens, credentials, or access tokens;
- absolute model paths or generated asset caches.

Model set, orientation, search preferences, and camera state belong to an enclosing application session, not `ChessGameRecord`.

## Validation plan

Implementation tests must cover normal moves, captures, en passant, both castlings, all promotions, check, checkmate, stalemate, illegal-move no-op, search-preview no-op, undo/redo, branch replacement, FEN-chain validation, and replay transactionality. The existing FEN/legal/undo ABI remains intact; native additions, if required for SAN context, are append-only.
