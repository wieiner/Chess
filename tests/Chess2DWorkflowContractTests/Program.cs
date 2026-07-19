using ChessApp;
using ChessGameRecords;

var checks = new ContractChecks();
using var engine = new NativeChessEngine();
var history = new ChessGameHistory(engine.GetFen());

var initialFen = engine.GetFen();
checks.Check(engine.GetLegalMoves().Length == 20, "legal preview exposes 20 start moves");
checks.Check(engine.TryGetMoveDescriptor(4, 1, 4, 3, 0, out var e4Descriptor), "legal preview describes e2e4");
checks.Check(e4Descriptor.MovedPiece == NativeChessEngine.Pawn && engine.GetFen() == initialFen,
    "legal preview preserves native FEN");
checks.Check(history.Moves.Count == 0, "legal preview creates no game record");

checks.Check(!engine.TryMakeMove(0, 0, 0, 3, 0, out _), "illegal blocked rook move is rejected");
checks.Check(engine.GetFen() == initialFen && history.Moves.Count == 0, "illegal move preserves FEN and history");

var line = new List<ChessMoveRecord>
{
    Commit(checks, engine, history, 4, 1, 4, 3, 0, "e4"),
    Commit(checks, engine, history, 4, 6, 4, 4, 0, "e5"),
    Commit(checks, engine, history, 6, 0, 5, 2, 0, "Nf3")
};
var lineFen = engine.GetFen();
checks.Check(history.Moves.Select(move => move.San).SequenceEqual(new[] { "e4", "e5", "Nf3" }),
    "three-ply structured SAN line is canonical");
checks.Check(history.Moves.Zip(history.Moves.Skip(1), (left, right) => left.PostMoveFen == right.PreMoveFen).All(value => value),
    "committed move records form a continuous FEN chain");

checks.Check(engine.Undo(), "native undo succeeds after recorded line");
checks.Check(history.TryUndo(engine.GetFen(), out var undone, out _) && undone?.San == "Nf3", "structured undo matches native restored FEN");
checks.Check(history.Moves.Count == 2 && history.RedoMoves.Count == 1, "undo updates active and redo lines");
var replayedKnight = Commit(checks, engine, history, 6, 0, 5, 2, 0, "Nf3");
checks.Check(replayedKnight.PostMoveFen == lineFen && history.RedoMoves.Count == 0, "recommitted move restores final FEN and clears redo");

engine.Reset();
history.Reset(engine.GetFen());
foreach (var record in line)
{
    _ = Commit(checks, engine, history, record.From.File, record.From.Rank, record.To.File, record.To.Rank,
        record.PromotionPiece, record.San);
}
checks.Check(engine.GetFen() == lineFen, "structured coordinate replay reproduces final FEN");
checks.Check(history.Moves.Count == line.Count, "structured replay reproduces action count");

engine.Reset();
history.Reset(engine.GetFen());
_ = Commit(checks, engine, history, 5, 1, 5, 2, 0, "f3");
_ = Commit(checks, engine, history, 4, 6, 4, 4, 0, "e5");
_ = Commit(checks, engine, history, 6, 1, 6, 3, 0, "g4");
var mate = Commit(checks, engine, history, 3, 7, 7, 3, 0, "Qh4#");
checks.Check(mate.IsCheckmate && history.Result == ChessGameResult.BlackWin && history.Termination == ChessTerminationReason.Checkmate,
    "Fool's mate records SAN mate and engine-backed result");

const string stalemateFen = "7k/5Q2/6K1/8/8/8/8/8 b - - 0 1";
checks.Check(engine.SetFen(stalemateFen), "stalemate FEN loads for history outcome");
history.Reset(engine.GetFen());
var stalemate = engine.GetState();
checks.Check(stalemate.Status == NativeChessEngine.StatusStalemate, "native engine reports stalemate");
history.UpdateOutcome(ChessGameResult.Draw, ChessTerminationReason.Stalemate);
checks.Check(history.Result == ChessGameResult.Draw && history.Moves.Count == 0, "stalemate outcome requires no fabricated move record");

const string claimFen = "8/8/8/8/8/8/4K3/7k w - - 100 1";
checks.Check(engine.SetFen(claimFen), "fifty-move claim FEN loads");
history.Reset(engine.GetFen());
var claimState = engine.GetState();
checks.Check(claimState.Status == NativeChessEngine.StatusFiftyMoveClaim && history.Result == ChessGameResult.Ongoing,
    "claimable fifty-move state remains ongoing before claim");
checks.Check(engine.ClaimDraw(), "fifty-move draw claim succeeds");
history.UpdateOutcome(ChessGameResult.Draw, ChessTerminationReason.FiftyMoveRule);
checks.Check(history.Result == ChessGameResult.Draw && history.Moves.Count == 0, "draw claim updates outcome without a move record");

return checks.Finish("Chess2DWorkflowContractTests");

static ChessMoveRecord Commit(
    ContractChecks checks,
    NativeChessEngine engine,
    ChessGameHistory history,
    int fromFile,
    int fromRank,
    int toFile,
    int toRank,
    int promotion,
    string expectedSan)
{
    var before = engine.GetState();
    var preFen = engine.GetFen();
    checks.Check(engine.TryGetMoveDescriptor(fromFile, fromRank, toFile, toRank, promotion, out var descriptor),
        $"descriptor is available for {expectedSan}");
    checks.Check(engine.TryMakeMove(fromFile, fromRank, toFile, toRank, promotion, out var played),
        $"native move succeeds for {expectedSan}");

    var san = ChessSanGenerator.Generate(new ChessSanMoveContext(
        true,
        fromFile,
        fromRank,
        toFile,
        toRank,
        descriptor.MovedPiece,
        descriptor.CapturedPiece,
        played.Promotion,
        (played.Flags & NativeChessEngine.MoveCapture) != 0,
        (played.Flags & NativeChessEngine.MoveEnPassant) != 0,
        (ChessCastleKind)descriptor.CastleKind,
        (ChessSanDisambiguation)descriptor.Disambiguation,
        descriptor.ResultingIsCheck != 0,
        descriptor.ResultingStatus == NativeChessEngine.StatusCheckmate));
    checks.Check(san.Success && san.San == expectedSan, $"generated SAN is {expectedSan}");

    var state = engine.GetState();
    var result = state.Status == NativeChessEngine.StatusCheckmate
        ? state.SideToMove == NativeChessEngine.White ? ChessGameResult.BlackWin : ChessGameResult.WhiteWin
        : ChessGameResult.Ongoing;
    var termination = result == ChessGameResult.Ongoing ? ChessTerminationReason.None : ChessTerminationReason.Checkmate;
    var record = new ChessMoveRecord(
        history.Moves.Count,
        before.FullmoveNumber,
        before.SideToMove,
        new ChessSquare(fromFile, fromRank),
        new ChessSquare(toFile, toRank),
        descriptor.MovedPiece,
        descriptor.CapturedPiece,
        played.Promotion,
        (ChessCastleKind)descriptor.CastleKind,
        (played.Flags & NativeChessEngine.MoveEnPassant) != 0,
        (played.Flags & NativeChessEngine.MoveCapture) != 0,
        descriptor.ResultingIsCheck != 0,
        descriptor.ResultingStatus == NativeChessEngine.StatusCheckmate,
        preFen,
        engine.GetFen(),
        Uci(played),
        san.San,
        null,
        null,
        null);
    checks.Check(history.TryCommit(record, result, termination, out _), $"structured record commits for {expectedSan}");
    return record;
}

static string Uci(ChessMoveDto move)
{
    var promotion = move.Promotion switch
    {
        NativeChessEngine.Queen => "q",
        NativeChessEngine.Rook => "r",
        NativeChessEngine.Bishop => "b",
        NativeChessEngine.Knight => "n",
        _ => string.Empty
    };
    return $"{(char)('a' + move.FromFile)}{move.FromRank + 1}{(char)('a' + move.ToFile)}{move.ToRank + 1}{promotion}";
}

internal sealed class ContractChecks
{
    private int _failed;

    public void Check(bool condition, string name)
    {
        Console.WriteLine($"{(condition ? "PASS" : "FAIL")} {name}");
        if (!condition)
        {
            _failed++;
        }
    }

    public int Finish(string suite)
    {
        Console.WriteLine($"{suite}: {(_failed == 0 ? "PASS" : $"FAIL ({_failed})")}");
        return _failed == 0 ? 0 : 1;
    }
}
