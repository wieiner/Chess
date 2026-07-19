using ChessGameRecords;

var checks = new ContractChecks();
var fixtures = new List<SanFixture>();

for (var file = 0; file < 8; file++)
{
    fixtures.Add(new($"pawn-{(char)('a' + file)}3", Context(file, 1, file, 2), $"{(char)('a' + file)}3"));
    fixtures.Add(new($"pawn-{(char)('a' + file)}4", Context(file, 1, file, 3), $"{(char)('a' + file)}4"));
}

for (var file = 1; file < 8; file++)
{
    fixtures.Add(new($"pawn-capture-{file}", Context(file, 3, file - 1, 4, captured: -1), $"{(char)('a' + file)}x{(char)('a' + file - 1)}5"));
}

fixtures.AddRange(new[]
{
    new SanFixture("knight-move", Context(6, 0, 5, 2, piece: 2), "Nf3"),
    new SanFixture("bishop-move", Context(5, 0, 1, 4, piece: 3), "Bb5"),
    new SanFixture("rook-move", Context(0, 0, 0, 2, piece: 4), "Ra3"),
    new SanFixture("queen-move", Context(3, 0, 3, 2, piece: 5), "Qd3"),
    new SanFixture("king-move", Context(4, 0, 4, 1, piece: 6), "Ke2"),
    new SanFixture("black-knight", Context(6, 7, 5, 5, piece: -2), "Nf6"),

    new SanFixture("knight-capture", Context(5, 2, 4, 4, piece: 2, captured: -1), "Nxe5"),
    new SanFixture("bishop-capture", Context(2, 0, 7, 5, piece: 3, captured: -4), "Bxh6"),
    new SanFixture("rook-capture", Context(0, 0, 0, 6, piece: 4, captured: -1), "Rxa7"),
    new SanFixture("queen-capture", Context(3, 0, 7, 4, piece: 5, captured: -1), "Qxh5"),
    new SanFixture("king-capture", Context(4, 0, 3, 1, piece: 6, captured: -1), "Kxd2"),
    new SanFixture("black-piece-capture", Context(2, 7, 6, 3, piece: -3, captured: 1), "Bxg4"),

    new SanFixture("file-disambiguation", Context(1, 0, 3, 1, piece: 2, disambiguation: ChessSanDisambiguation.File), "Nbd2"),
    new SanFixture("rank-disambiguation", Context(0, 0, 4, 0, piece: 4, disambiguation: ChessSanDisambiguation.Rank), "R1e1"),
    new SanFixture("both-disambiguation", Context(7, 3, 4, 0, piece: 5, disambiguation: ChessSanDisambiguation.File | ChessSanDisambiguation.Rank), "Qh4e1"),
    new SanFixture("file-capture", Context(0, 0, 0, 2, piece: 4, captured: -2, disambiguation: ChessSanDisambiguation.File), "Raxa3"),
    new SanFixture("rank-capture", Context(0, 0, 0, 2, piece: 4, captured: -2, disambiguation: ChessSanDisambiguation.Rank), "R1xa3"),
    new SanFixture("both-capture", Context(7, 3, 4, 0, piece: 5, captured: -4, disambiguation: ChessSanDisambiguation.File | ChessSanDisambiguation.Rank), "Qh4xe1"),

    new SanFixture("white-king-castle", Context(4, 0, 6, 0, piece: 6, castle: ChessCastleKind.KingSide), "O-O"),
    new SanFixture("white-queen-castle", Context(4, 0, 2, 0, piece: 6, castle: ChessCastleKind.QueenSide), "O-O-O"),
    new SanFixture("black-castle-check", Context(4, 7, 6, 7, piece: -6, castle: ChessCastleKind.KingSide, check: true), "O-O+"),
    new SanFixture("black-castle-mate", Context(4, 7, 2, 7, piece: -6, castle: ChessCastleKind.QueenSide, check: true, mate: true), "O-O-O#"),

    new SanFixture("promote-q", Context(4, 6, 4, 7, promotion: 5), "e8=Q"),
    new SanFixture("promote-r", Context(4, 6, 4, 7, promotion: 4), "e8=R"),
    new SanFixture("promote-b", Context(4, 6, 4, 7, promotion: 3), "e8=B"),
    new SanFixture("promote-n", Context(4, 6, 4, 7, promotion: 2), "e8=N"),
    new SanFixture("black-promote-q", Context(3, 1, 3, 0, piece: -1, promotion: 5), "d1=Q"),
    new SanFixture("black-promote-n", Context(3, 1, 3, 0, piece: -1, promotion: 2), "d1=N"),
    new SanFixture("capture-promote-q", Context(5, 6, 6, 7, captured: -4, promotion: 5), "fxg8=Q"),
    new SanFixture("capture-promote-r", Context(5, 6, 6, 7, captured: -4, promotion: 4), "fxg8=R"),
    new SanFixture("capture-promote-b-check", Context(5, 6, 6, 7, captured: -4, promotion: 3, check: true), "fxg8=B+"),
    new SanFixture("capture-promote-n-mate", Context(5, 6, 6, 7, captured: -4, promotion: 2, check: true, mate: true), "fxg8=N#"),
    new SanFixture("black-capture-promote", Context(1, 1, 0, 0, piece: -1, captured: 4, promotion: 5), "bxa1=Q"),
    new SanFixture("black-capture-promote-check", Context(1, 1, 0, 0, piece: -1, captured: 4, promotion: 2, check: true), "bxa1=N+"),

    new SanFixture("pawn-check", Context(4, 5, 4, 6, check: true), "e7+"),
    new SanFixture("knight-check", Context(5, 2, 6, 4, piece: 2, check: true), "Ng5+"),
    new SanFixture("discovered-check", Context(2, 3, 1, 4, piece: 3, check: true), "Bb5+"),
    new SanFixture("double-check", Context(3, 3, 5, 4, piece: 2, check: true), "Nf5+"),
    new SanFixture("queen-mate", Context(6, 5, 6, 6, piece: 5, check: true, mate: true), "Qg7#"),

    new SanFixture("en-passant-white", Context(4, 4, 3, 5, captured: -1, enPassant: true), "exd6"),
    new SanFixture("en-passant-black", Context(3, 3, 4, 2, piece: -1, captured: 1, enPassant: true), "dxe3")
});

foreach (var fixture in fixtures)
{
    var result = ChessSanGenerator.Generate(fixture.Context);
    checks.Check(result.Success && result.San == fixture.Expected, $"SAN fixture {fixture.Name}: {fixture.Expected}");
}
checks.Check(fixtures.Count >= 50, $"SAN fixture count is at least 50 (actual {fixtures.Count})");

var deterministicContext = Context(1, 0, 3, 1, piece: 2, disambiguation: ChessSanDisambiguation.File, check: true);
var first = ChessSanGenerator.Generate(deterministicContext);
var second = ChessSanGenerator.Generate(deterministicContext);
checks.Check(first == second && first.San == "Nbd2+", "SAN generation is deterministic");
checks.Check(first.Token is { PieceDesignator: "N", OriginQualifier: "b", Destination: "d2", CheckSuffix: "+" },
    "SAN generation exposes a structured parser token");

CheckFailure(checks, Context(4, 1, 4, 3) with { IsLegal = false }, ChessSanError.IllegalMove, "illegal move");
CheckFailure(checks, Context(-1, 1, 4, 3), ChessSanError.InvalidCoordinates, "bad source coordinate");
CheckFailure(checks, Context(4, 1, 8, 3), ChessSanError.InvalidCoordinates, "bad target coordinate");
CheckFailure(checks, Context(4, 1, 4, 3, piece: 0), ChessSanError.InvalidPiece, "empty moved piece");
CheckFailure(checks, Context(4, 1, 4, 3) with { IsCapture = true }, ChessSanError.InvalidCapture, "capture without captured piece");
CheckFailure(checks, Context(4, 1, 4, 3, disambiguation: ChessSanDisambiguation.File), ChessSanError.InvalidDisambiguation, "pawn disambiguation");
CheckFailure(checks, Context(4, 0, 6, 0, piece: 5, castle: ChessCastleKind.KingSide), ChessSanError.InvalidCastle, "castle with queen");
CheckFailure(checks, Context(4, 6, 4, 7), ChessSanError.InvalidPromotion, "last-rank pawn without promotion");
CheckFailure(checks, Context(4, 5, 4, 6, promotion: 5), ChessSanError.InvalidPromotion, "promotion before last rank");
CheckFailure(checks, Context(6, 5, 6, 6, piece: 5, mate: true), ChessSanError.InvalidCheckState, "mate without check");

var history = new ChessGameHistory(ChessGameHistory.StandardInitialFen);
var e4 = Record(0, 1, 1, "e2e4", "e4", ChessGameHistory.StandardInitialFen,
    "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1");
checks.Check(history.TryCommit(e4, ChessGameResult.Ongoing, ChessTerminationReason.None, out _),
    "structured history commits a valid first record");
checks.Check(history.Moves.Count == 1 && history.Moves[0].San == "e4", "structured history exposes committed SAN");

var invalidChain = Record(1, 1, -1, "e7e5", "e5", ChessGameHistory.StandardInitialFen,
    "rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 2");
checks.Check(!history.TryCommit(invalidChain, ChessGameResult.Ongoing, ChessTerminationReason.None, out _),
    "structured history rejects a broken FEN chain");
checks.Check(history.Moves.Count == 1, "rejected history commit does not mutate the active line");

checks.Check(history.TryUndo(ChessGameHistory.StandardInitialFen, out var undone, out _) && undone?.Uci == "e2e4",
    "structured history undo validates the restored native FEN");
checks.Check(history.Moves.Count == 0 && history.RedoMoves.Count == 1, "undo moves the record to deterministic redo storage");
checks.Check(!history.TryUndo(ChessGameHistory.StandardInitialFen, out _, out _), "empty structured history undo clean-fails");

checks.Check(history.TryCommit(e4, ChessGameResult.Ongoing, ChessTerminationReason.None, out _),
    "new branch can recommit after undo");
checks.Check(history.RedoMoves.Count == 0, "new branch clears redo records");
var snapshot = history.Snapshot();
history.Reset("8/8/8/8/8/8/4K3/7k w - - 0 1");
checks.Check(snapshot.Moves.Count == 1 && snapshot.Moves[0].San == "e4", "history snapshot remains immutable after reset");
checks.Check(history.Moves.Count == 0 && history.InitialPosition.FullmoveNumber == 1, "history reset starts a new empty game");

var badFenRejected = false;
try
{
    _ = ChessPositionRecord.FromFen("not-fen");
}
catch (ArgumentException)
{
    badFenRejected = true;
}
checks.Check(badFenRejected, "position record rejects incomplete FEN");

checks.Check(PgnSevenTagRoster.Names.SequenceEqual(new[] { "Event", "Site", "Date", "Round", "White", "Black", "Result" }),
    "PGN Seven Tag Roster has canonical order");
checks.Check(PgnResult.WhiteWin.ToMarker() == "1-0" && PgnResult.Draw.ToMarker() == "1/2-1/2" &&
    PgnResultExtensions.TryParseMarker("*", out var ongoingPgn) && ongoingPgn == PgnResult.Ongoing,
    "PGN result markers roundtrip");

var sourceTags = new List<PgnTagPair>
{
    new("Event", "Model Contract"), new("Site", "?"), new("Date", "2026.07.19"), new("Round", "1"),
    new("White", "White"), new("Black", "Black"), new("Result", "1-0"),
    new("SetUp", "1"), new("FEN", ChessGameHistory.StandardInitialFen), new("Annotator", "P4M")
};
var variationMoves = new List<PgnMoveNode> { new(1, "c5", trailingComments: new[] { new PgnComment("Sicilian") }) };
var mainMoves = new List<PgnMoveNode>
{
    new(0, "e4", nags: new[] { new PgnNag(1) }, trailingComments: new[] { new PgnComment("Main line") },
        variations: new[] { new PgnVariation(variationMoves) }),
    new(1, "e5", leadingComments: new[] { new PgnComment("Symmetric reply", PgnCommentStyle.Semicolon) })
};
var pgnGame = new PgnGame(sourceTags, new PgnVariation(mainMoves), PgnResult.WhiteWin);
var pgnDocument = new PgnDocument(new[] { pgnGame });
sourceTags.Clear();
variationMoves.Clear();
mainMoves.Clear();

checks.Check(pgnDocument.Games.Count == 1 && pgnGame.Tags.Count == 10 && pgnGame.MainLine.Moves.Count == 2,
    "PGN document model defensively freezes source collections");
checks.Check(pgnGame.Tags[7].Name == "SetUp" && pgnGame.Tags[8].Name == "FEN" && pgnGame.FindTag("Annotator") == "P4M",
    "PGN model preserves ordered custom and setup tags");
var firstPgnMove = pgnGame.MainLine.Moves[0];
checks.Check(firstPgnMove.FullmoveNumber == 1 && !firstPgnMove.IsBlackMove && firstPgnMove.Nags[0].ToString() == "$1",
    "PGN move node exposes move number, side, and NAG");
checks.Check(firstPgnMove.Variations.Count == 1 && firstPgnMove.Variations[0].Moves[0].San == "c5" &&
    firstPgnMove.TrailingComments[0].Text == "Main line",
    "PGN model represents comments and recursive annotation variations");
checks.Check(pgnGame.MainLine.Moves[1].IsBlackMove && pgnGame.MainLine.Moves[1].LeadingComments[0].Style == PgnCommentStyle.Semicolon,
    "PGN model preserves semicolon comment style");

var invalidTagRejected = false;
try { _ = new PgnTagPair("Bad-Tag", "value"); } catch (ArgumentException) { invalidTagRejected = true; }
checks.Check(invalidTagRejected, "PGN model rejects invalid tag names");
var invalidNagRejected = false;
try { _ = new PgnNag(256); } catch (ArgumentOutOfRangeException) { invalidNagRejected = true; }
checks.Check(invalidNagRejected, "PGN model rejects out-of-range NAG values");

return checks.Finish("ChessGameRecordsContractTests");

static ChessSanMoveContext Context(
    int fromFile,
    int fromRank,
    int toFile,
    int toRank,
    int piece = 1,
    int captured = 0,
    int promotion = 0,
    ChessCastleKind castle = ChessCastleKind.None,
    ChessSanDisambiguation disambiguation = ChessSanDisambiguation.None,
    bool check = false,
    bool mate = false,
    bool enPassant = false) =>
    new(true, fromFile, fromRank, toFile, toRank, piece, captured, promotion,
        captured != 0 || enPassant, enPassant, castle, disambiguation, check, mate);

static void CheckFailure(ContractChecks checks, ChessSanMoveContext context, ChessSanError expected, string name)
{
    var result = ChessSanGenerator.Generate(context);
    checks.Check(!result.Success && result.Error == expected && string.IsNullOrEmpty(result.San), $"SAN rejects {name}");
}

static ChessMoveRecord Record(int ply, int fullmove, int side, string uci, string san, string preFen, string postFen) => new(
    ply,
    fullmove,
    side,
    new ChessSquare(uci[0] - 'a', uci[1] - '1'),
    new ChessSquare(uci[2] - 'a', uci[3] - '1'),
    side,
    0,
    0,
    ChessCastleKind.None,
    false,
    false,
    false,
    false,
    preFen,
    postFen,
    uci,
    san,
    null,
    null,
    null);

internal sealed record SanFixture(string Name, ChessSanMoveContext Context, string Expected);

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
