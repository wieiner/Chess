using System.Collections.ObjectModel;

namespace ChessGameRecords;

public enum ChessGameResult
{
    Ongoing = 0,
    WhiteWin,
    BlackWin,
    Draw
}

public enum ChessTerminationReason
{
    None = 0,
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

public readonly record struct ChessSquare(int File, int Rank)
{
    public bool IsValid => File is >= 0 and < 8 && Rank is >= 0 and < 8;
    public override string ToString() => IsValid ? $"{(char)('a' + File)}{Rank + 1}" : "??";
}

public sealed record ChessPositionRecord(string Fen, int SideToMove, int HalfmoveClock, int FullmoveNumber)
{
    public static ChessPositionRecord FromFen(string fen)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fen);
        var fields = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 6 || fields[1] is not ("w" or "b") ||
            !int.TryParse(fields[4], out var halfmove) || halfmove < 0 ||
            !int.TryParse(fields[5], out var fullmove) || fullmove < 1)
        {
            throw new ArgumentException("A complete six-field FEN is required.", nameof(fen));
        }
        return new ChessPositionRecord(fen, fields[1] == "w" ? 1 : -1, halfmove, fullmove);
    }
}

public sealed record ChessGameHeaders(
    string Event,
    string Site,
    string Date,
    string Round,
    string White,
    string Black,
    IReadOnlyDictionary<string, string> AdditionalTags)
{
    public static ChessGameHeaders CreateDefault() => new(
        "Casual Game", "?", "????.??.??", "?", "White", "Black",
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()));
}

public sealed record ChessClockSnapshot(TimeSpan? WhiteRemaining, TimeSpan? BlackRemaining, TimeSpan? MoveElapsed);

public sealed record ChessEvaluationMetadata(
    int? Centipawns,
    int? MateIn,
    int? Depth,
    long? Nodes,
    string? PrincipalVariation);

public sealed record ChessMoveRecord(
    int PlyIndex,
    int FullmoveNumber,
    int Side,
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

public sealed record ChessGameRecord(
    ChessGameHeaders Headers,
    ChessPositionRecord InitialPosition,
    IReadOnlyList<ChessMoveRecord> Moves,
    IReadOnlyList<ChessMoveRecord> RedoMoves,
    ChessGameResult Result,
    ChessTerminationReason Termination,
    DateTimeOffset CreatedUtc,
    DateTimeOffset ModifiedUtc);

public sealed class ChessGameHistory
{
    public const string StandardInitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private readonly List<ChessMoveRecord> _moves = new();
    private readonly List<ChessMoveRecord> _redoMoves = new();
    private DateTimeOffset _createdUtc;

    public ChessGameHistory(string initialFen, ChessGameHeaders? headers = null)
    {
        Headers = headers ?? ChessGameHeaders.CreateDefault();
        Reset(initialFen);
    }

    public ChessGameHeaders Headers { get; private set; }
    public ChessPositionRecord InitialPosition { get; private set; } = null!;
    public IReadOnlyList<ChessMoveRecord> Moves => _moves.AsReadOnly();
    public IReadOnlyList<ChessMoveRecord> RedoMoves => _redoMoves.AsReadOnly();
    public ChessGameResult Result { get; private set; }
    public ChessTerminationReason Termination { get; private set; }
    public DateTimeOffset ModifiedUtc { get; private set; }

    public void Reset(string initialFen, ChessGameHeaders? headers = null)
    {
        InitialPosition = ChessPositionRecord.FromFen(initialFen);
        if (headers is not null)
        {
            Headers = headers;
        }
        _moves.Clear();
        _redoMoves.Clear();
        Result = ChessGameResult.Ongoing;
        Termination = ChessTerminationReason.None;
        _createdUtc = DateTimeOffset.UtcNow;
        ModifiedUtc = _createdUtc;
    }

    public bool TryCommit(
        ChessMoveRecord record,
        ChessGameResult result,
        ChessTerminationReason termination,
        out string error)
    {
        var expectedFen = _moves.Count == 0 ? InitialPosition.Fen : _moves[^1].PostMoveFen;
        if (record.PlyIndex != _moves.Count || record.PreMoveFen != expectedFen ||
            !record.From.IsValid || !record.To.IsValid || string.IsNullOrWhiteSpace(record.San) ||
            string.IsNullOrWhiteSpace(record.Uci) || string.IsNullOrWhiteSpace(record.PostMoveFen))
        {
            error = "Move record does not continue the active legal position chain.";
            return false;
        }
        if (record.FullmoveNumber < 1 || record.Side is not (1 or -1))
        {
            error = "Move number or side is invalid.";
            return false;
        }
        if ((result == ChessGameResult.Ongoing) != (termination == ChessTerminationReason.None))
        {
            error = "Game result and termination reason are inconsistent.";
            return false;
        }

        _moves.Add(record);
        _redoMoves.Clear();
        Result = result;
        Termination = termination;
        ModifiedUtc = DateTimeOffset.UtcNow;
        error = string.Empty;
        return true;
    }

    public bool TryUndo(string restoredFen, out ChessMoveRecord? record, out string error)
    {
        record = null;
        if (_moves.Count == 0)
        {
            error = "There is no committed move to undo.";
            return false;
        }
        var candidate = _moves[^1];
        if (candidate.PreMoveFen != restoredFen)
        {
            error = "Native undo position does not match the move record pre-position.";
            return false;
        }
        _moves.RemoveAt(_moves.Count - 1);
        _redoMoves.Insert(0, candidate);
        Result = ChessGameResult.Ongoing;
        Termination = ChessTerminationReason.None;
        ModifiedUtc = DateTimeOffset.UtcNow;
        record = candidate;
        error = string.Empty;
        return true;
    }

    public void UpdateOutcome(ChessGameResult result, ChessTerminationReason termination)
    {
        if ((result == ChessGameResult.Ongoing) != (termination == ChessTerminationReason.None))
        {
            throw new ArgumentException("Game result and termination reason are inconsistent.");
        }
        Result = result;
        Termination = termination;
        ModifiedUtc = DateTimeOffset.UtcNow;
    }

    public bool TryLoad(ChessGameRecord record, out string error)
    {
        ArgumentNullException.ThrowIfNull(record);
        var expectedFen = record.InitialPosition.Fen;
        for (var index = 0; index < record.Moves.Count; index++)
        {
            var move = record.Moves[index];
            if (move.PlyIndex != index || move.PreMoveFen != expectedFen || !move.From.IsValid || !move.To.IsValid)
            {
                error = $"Move record {index + 1} breaks the imported FEN chain.";
                return false;
            }
            expectedFen = move.PostMoveFen;
        }
        Headers = record.Headers;
        InitialPosition = record.InitialPosition;
        _moves.Clear();
        _moves.AddRange(record.Moves);
        _redoMoves.Clear();
        _redoMoves.AddRange(record.RedoMoves);
        Result = record.Result;
        Termination = record.Termination;
        _createdUtc = record.CreatedUtc;
        ModifiedUtc = record.ModifiedUtc;
        error = string.Empty;
        return true;
    }

    public ChessGameRecord Snapshot() => new(
        Headers,
        InitialPosition,
        Array.AsReadOnly(_moves.ToArray()),
        Array.AsReadOnly(_redoMoves.ToArray()),
        Result,
        Termination,
        _createdUtc,
        ModifiedUtc);
}
