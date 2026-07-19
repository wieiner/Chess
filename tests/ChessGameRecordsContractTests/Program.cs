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

var exported = PgnExporter.Export(pgnGame);
checks.Check(exported.Success, "PGN exporter accepts a complete strict game");
checks.Check(exported.Text.StartsWith("[Event \"Model Contract\"]\n[Site \"?\"]", StringComparison.Ordinal),
    "PGN exporter emits canonical Seven Tag Roster order");
var normalizedExport = string.Join(' ', exported.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
checks.Check(exported.Text.Contains("[SetUp \"1\"]\n[FEN \"", StringComparison.Ordinal) &&
    normalizedExport.Contains("1. e4 $1 {Main line} ( 1... c5 {Sicilian} ) {Symmetric reply} e5 1-0", StringComparison.Ordinal),
    "PGN exporter emits SetUp/FEN, comments, NAG, RAV, move numbers, and result");
checks.Check(PgnExporter.Export(pgnGame).Text == exported.Text, "PGN export is deterministic");

var escapedTags = pgnGame.Tags.Select(tag => tag.Name == "Event" ? new PgnTagPair("Event", "A \\\"quoted\\\" path") : tag);
var escapedExport = PgnExporter.Export(new PgnGame(escapedTags, pgnGame.MainLine, pgnGame.Result));
checks.Check(escapedExport.Success && escapedExport.Text.Contains("[Event \"A \\\\\\\"quoted\\\\\\\" path\"]", StringComparison.Ordinal),
    "PGN exporter escapes tag quotes and backslashes");

var mismatchedTags = pgnGame.Tags.Select(tag => tag.Name == "Result" ? new PgnTagPair("Result", "0-1") : tag);
checks.Check(!PgnExporter.Export(new PgnGame(mismatchedTags, pgnGame.MainLine, PgnResult.WhiteWin)).Success,
    "PGN exporter rejects inconsistent result tag and marker");
var missingRoster = new PgnGame(pgnGame.Tags.Where(tag => tag.Name != "Round"), pgnGame.MainLine, pgnGame.Result);
checks.Check(!PgnExporter.Export(missingRoster).Success, "PGN exporter rejects an incomplete Seven Tag Roster");

var recordExportHistory = new ChessGameHistory(ChessGameHistory.StandardInitialFen,
    new ChessGameHeaders("Record Export", "Local", "2026.07.19", "1", "Alice", "Bob",
        new Dictionary<string, string> { ["Zeta"] = "last", ["Alpha"] = "first" }));
checks.Check(recordExportHistory.TryCommit(e4 with { Comment = "King pawn" }, ChessGameResult.Ongoing,
    ChessTerminationReason.None, out _), "record exporter fixture commits");
var recordExport = PgnExporter.Export(recordExportHistory.Snapshot());
checks.Check(recordExport.Success && recordExport.Text.Contains("[Alpha \"first\"]\n[Zeta \"last\"]", StringComparison.Ordinal) &&
    recordExport.Text.EndsWith("1. e4 {King pawn} *\n", StringComparison.Ordinal),
    "structured game record exports deterministic custom tags and main line");

var nonstandardHistory = new ChessGameHistory("7k/5Q2/6K1/8/8/8/8/8 b - - 0 23");
var nonstandardExport = PgnExporter.Export(nonstandardHistory.Snapshot());
checks.Check(nonstandardExport.Success && nonstandardExport.Text.Contains("[SetUp \"1\"]", StringComparison.Ordinal) &&
    nonstandardExport.Text.Contains("[FEN \"7k/5Q2/6K1/8/8/8/8/8 b - - 0 23\"]", StringComparison.Ordinal),
    "structured nonstandard start exports matching SetUp and FEN tags");

const string tokenFixture = "[Event \"A \\\"quoted\\\" event\"]\n[Result \"1-0\"]\n\n1. e4 $1 {main\ncomment} (1... c5 ;reply\n2. Nf3) e5 1-0";
var tokenized = PgnTokenizer.Tokenize(tokenFixture);
checks.Check(tokenized.Success && tokenized.Diagnostics.Count == 0, "PGN tokenizer accepts tags and annotated movetext");
checks.Check(tokenized.Tokens.Select(token => token.Kind).Contains(PgnTokenKind.TagName) &&
    tokenized.Tokens.Select(token => token.Kind).Contains(PgnTokenKind.QuotedString) &&
    tokenized.Tokens.Select(token => token.Kind).Contains(PgnTokenKind.Nag) &&
    tokenized.Tokens.Select(token => token.Kind).Contains(PgnTokenKind.Comment) &&
    tokenized.Tokens.Select(token => token.Kind).Contains(PgnTokenKind.SemicolonComment) &&
    tokenized.Tokens.Select(token => token.Kind).Contains(PgnTokenKind.LeftParenthesis) &&
    tokenized.Tokens[^1].Kind == PgnTokenKind.EndOfFile,
    "PGN tokenizer emits the complete lexical token family");
checks.Check(tokenized.Tokens.First(token => token.Kind == PgnTokenKind.QuotedString).Value == "A \"quoted\" event",
    "PGN tokenizer decodes quoted tag escapes");
var c5Token = tokenized.Tokens.First(token => token.Value == "c5");
checks.Check(c5Token.Location.Line == 5 && c5Token.Location.Column > 1,
    "PGN tokenizer records one-based line and column locations");
checks.Check(tokenized.Tokens.Count(token => token.Kind == PgnTokenKind.Result) == 1 &&
    tokenized.Tokens.Count(token => token.Kind == PgnTokenKind.Integer) == 3 &&
    tokenized.Tokens.Count(token => token.Kind == PgnTokenKind.Period) == 5,
    "PGN tokenizer distinguishes results, move integers, and periods");

var unterminatedString = PgnTokenizer.Tokenize("[Event \"broken]");
checks.Check(!unterminatedString.Success && unterminatedString.Tokens.Count == 0 &&
    unterminatedString.Diagnostics.Single().Code == "unterminatedString",
    "PGN tokenizer fails atomically on unterminated string");
var unterminatedComment = PgnTokenizer.Tokenize("1. e4 {broken");
checks.Check(!unterminatedComment.Success && unterminatedComment.Tokens.Count == 0 &&
    unterminatedComment.Diagnostics.Single().Code == "unterminatedComment",
    "PGN tokenizer fails atomically on unterminated comment");
var badNag = PgnTokenizer.Tokenize("1. e4 $256 *");
checks.Check(!badNag.Success && badNag.Diagnostics.Single().Code == "invalidNag",
    "PGN tokenizer rejects out-of-range NAG");
var boundedInput = PgnTokenizer.Tokenize("12345", new PgnTokenizerOptions(MaxInputLength: 4));
checks.Check(!boundedInput.Success && boundedInput.Diagnostics.Single().Code == "inputTooLarge",
    "PGN tokenizer enforces input bound before scanning");
var boundedTokens = PgnTokenizer.Tokenize("1. e4 e5 *", new PgnTokenizerOptions(MaxTokens: 3));
checks.Check(!boundedTokens.Success && boundedTokens.Tokens.Count == 0 && boundedTokens.Diagnostics.Single().Code == "tooManyTokens",
    "PGN tokenizer enforces token bound without partial output");

var parsedExport = PgnParser.Parse(exported.Text, new PgnParserOptions(PgnParserMode.ExportStrict));
checks.Check(parsedExport.Success && parsedExport.Document?.Games.Count == 1, "PGN parser reads strict exported document");
var parsedGame = parsedExport.Document!.Games[0];
checks.Check(parsedGame.Tags.Count == pgnGame.Tags.Count && parsedGame.Result == PgnResult.WhiteWin &&
    parsedGame.MainLine.Moves.Select(move => move.San).SequenceEqual(new[] { "e4", "e5" }),
    "PGN parser preserves tags, result, and main line");
checks.Check(parsedGame.MainLine.Moves[0].Nags.Single().Value == 1 &&
    parsedGame.MainLine.Moves[0].TrailingComments.Single().Text == "Main line" &&
    parsedGame.MainLine.Moves[0].Variations.Single().Moves.Single().San == "c5",
    "PGN parser preserves comments, NAG, and RAV");
checks.Check(parsedGame.MainLine.Moves[1].LeadingComments.Single().Text == "Symmetric reply" &&
    parsedGame.MainLine.Moves[1].LeadingComments.Single().Style == PgnCommentStyle.Brace,
    "PGN parser preserves leading comment placement in canonical export");

var tolerantParse = PgnParser.Parse("1. e4 e5 *", new PgnParserOptions(PgnParserMode.ImportTolerant));
checks.Check(tolerantParse.Success && tolerantParse.Document?.Games[0].Tags.Count == 0,
    "PGN tolerant parser accepts movetext without roster");
var strictParse = PgnParser.Parse("1. e4 e5 *", new PgnParserOptions(PgnParserMode.ExportStrict));
checks.Check(!strictParse.Success && strictParse.Diagnostics.Single().Code == "missingRequiredTag",
    "PGN strict parser requires Seven Tag Roster");
var duplicateRequiredParse = PgnParser.Parse("[Result \"*\"] [Result \"*\"] *");
checks.Check(!duplicateRequiredParse.Success && duplicateRequiredParse.Diagnostics.Single().Code == "duplicateRequiredTag",
    "PGN parser detects duplicate required tags");
var mismatchParse = PgnParser.Parse("[Result \"1-0\"] 1. e4 0-1");
checks.Check(!mismatchParse.Success && mismatchParse.Diagnostics.Single().Code == "resultMismatch",
    "PGN parser rejects inconsistent termination marker");
var badVariationParse = PgnParser.Parse("1. e4 (1... c5 *");
checks.Check(!badVariationParse.Success && badVariationParse.Diagnostics.Single().Code == "unterminatedVariation",
    "PGN parser rejects unterminated RAV without partial document");

var resolverCandidates = new[]
{
    Candidate(6, 0, 5, 2, piece: 2),
    Candidate(1, 0, 3, 1, piece: 2),
    Candidate(4, 1, 4, 3),
    Candidate(4, 4, 3, 5, captured: -1, enPassant: true),
    Candidate(4, 6, 4, 7, promotion: 5, check: true),
    Candidate(4, 0, 6, 0, piece: 6, castle: ChessCastleKind.KingSide)
};
checks.Check(ChessSanResolver.Resolve("Nf3", resolverCandidates).Move is { FromFile: 6, FromRank: 0 },
    "SAN resolver selects a unique legal knight move");
checks.Check(ChessSanResolver.Resolve("e4", resolverCandidates).Move is { FromFile: 4, ToRank: 3 },
    "SAN resolver selects a legal pawn move");
checks.Check(ChessSanResolver.Resolve("exd6", resolverCandidates).Move is { Context.IsEnPassant: true },
    "SAN resolver selects legal en passant by constraints");
checks.Check(ChessSanResolver.Resolve("e8=Q+", resolverCandidates).Move is { PromotionPiece: 5 },
    "SAN resolver selects legal promotion and check suffix");
checks.Check(ChessSanResolver.Resolve("O-O", resolverCandidates).Move is { Context.CastleKind: ChessCastleKind.KingSide },
    "SAN resolver selects legal castling");
checks.Check(ChessSanResolver.Resolve("Qa8", resolverCandidates).Error == ChessSanResolutionError.NoMatchingMove,
    "SAN resolver reports no matching move");
checks.Check(ChessSanResolver.Resolve("Nxd2", resolverCandidates).Error == ChessSanResolutionError.WrongCaptureMarker,
    "SAN resolver reports wrong capture marker");
var ambiguousCandidates = new[] { Candidate(1, 0, 3, 1, piece: 2), Candidate(5, 0, 3, 1, piece: 2) };
checks.Check(ChessSanResolver.Resolve("Nd2", ambiguousCandidates).Error == ChessSanResolutionError.AmbiguousSan,
    "SAN resolver rejects ambiguous SAN");
checks.Check(ChessSanResolver.Resolve("Nbd2", ambiguousCandidates).Move is { FromFile: 1 },
    "SAN resolver applies file disambiguation");
checks.Check(ChessSanResolver.Resolve("e8=R+", resolverCandidates).Error == ChessSanResolutionError.WrongPromotion,
    "SAN resolver reports wrong promotion");
checks.Check(ChessSanResolver.Resolve("e8=Q", resolverCandidates).Error == ChessSanResolutionError.WrongCheckSuffix,
    "SAN resolver reports wrong check suffix");

var sessionHistory = new ChessGameHistory(ChessGameHistory.StandardInitialFen);
checks.Check(sessionHistory.TryCommit(e4, ChessGameResult.Ongoing, ChessTerminationReason.None, out _),
    "session fixture commits a structured move");
var session = ChessSessionDocument.FromGame(
    sessionHistory.Snapshot(), e4.PostMoveFen,
    new ChessSessionPresentation(ChessSessionBoardOrientation.White, "Classic", "procedural",
        ChessSessionUiMode.Board2D),
    new ChessSessionEngineOptions(true, "Cpu", 8, 1500, 100000, 2), dirty: true,
    sessionId: Guid.Parse("d449b760-53e0-49ea-a6ef-fcb470dd76ae"));
var serializedSession = ChessSessionSerializer.Serialize(session);
checks.Check(serializedSession.Success && serializedSession.Json.Contains("\"format\": \"chess2d-session\"", StringComparison.Ordinal),
    "session serializer emits valid versioned JSON");
var repeatedSession = ChessSessionSerializer.Serialize(session);
checks.Check(repeatedSession.Hash == serializedSession.Hash && repeatedSession.Json == serializedSession.Json,
    "session JSON and diagnostic hash are deterministic");
var loadedSession = ChessSessionSerializer.Deserialize(serializedSession.Json);
checks.Check(loadedSession.Success && loadedSession.Document?.CurrentFen == e4.PostMoveFen &&
    loadedSession.Document.ToGameRecord().Moves.Single().San == "e4",
    "session JSON roundtrips structured game state");
checks.Check(!ChessSessionSerializer.Deserialize("{not json").Success,
    "invalid session JSON fails without a partial document");
checks.Check(!ChessSessionSerializer.Serialize(session with { CurrentFen = ChessGameHistory.StandardInitialFen }).Success,
    "session validator rejects a broken final FEN chain");
checks.Check(!ChessSessionSerializer.Serialize(session with
{
    Presentation = session.Presentation with { ModelSetId = "C:\\private\\model.obj" }
}).Success, "session validator rejects absolute presentation paths");

var sessionDirectory = Path.Combine(Path.GetTempPath(), $"chess-session-contract-{Guid.NewGuid():N}");
Directory.CreateDirectory(sessionDirectory);
try
{
    var sessionPath = Path.Combine(sessionDirectory, "game.chesssession.json");
    var fileService = new ChessSessionFileService();
    var savedSession = fileService.Save(sessionPath, session);
    checks.Check(savedSession.Success && File.Exists(sessionPath), "session file service saves through verified sibling file");
    var originalBytes = File.ReadAllBytes(sessionPath);
    var diskSession = fileService.Load(sessionPath);
    checks.Check(diskSession.Success && diskSession.Hash == serializedSession.Hash,
        "session file load preserves deterministic hash");

    foreach (var stage in new[]
             {
                 ChessSessionFileStage.AfterTempWrite,
                 ChessSessionFileStage.AfterFlush,
                 ChessSessionFileStage.AfterVerify,
                 ChessSessionFileStage.BeforeReplace
             })
    {
        var failingService = new ChessSessionFileService(current =>
        {
            if (current == stage) throw new InvalidOperationException($"Injected {stage}");
        });
        var failedSave = failingService.Save(sessionPath, session with { Dirty = false });
        checks.Check(!failedSave.Success && File.ReadAllBytes(sessionPath).SequenceEqual(originalBytes) &&
            !File.Exists(sessionPath + ".tmp"), $"session save failure at {stage} preserves original and cleans temp");
    }

    var afterReplaceService = new ChessSessionFileService(stage =>
    {
        if (stage == ChessSessionFileStage.AfterReplace) throw new InvalidOperationException("Injected AfterReplace");
    });
    var afterReplace = afterReplaceService.Save(sessionPath, session);
    checks.Check(!afterReplace.Success && fileService.Load(sessionPath).Success,
        "post-replace injected failure leaves a complete readable destination");
}
finally
{
    Directory.Delete(sessionDirectory, recursive: true);
}

var recoveryDirectory = Path.Combine(Path.GetTempPath(), $"chess-recovery-contract-{Guid.NewGuid():N}");
Directory.CreateDirectory(recoveryDirectory);
try
{
    var recovery = new ChessSessionRecoveryService(recoveryDirectory, retention: 2);
    File.WriteAllText(Path.Combine(recoveryDirectory, "incomplete.chesssession.json.tmp"), "partial");
    File.WriteAllText(Path.Combine(recoveryDirectory, "corrupt.chesssession.json"), "{bad json");
    checks.Check(recovery.GetCandidates().Count == 0, "recovery scan ignores incomplete temp and corrupt autosave files");

    var firstRecovery = recovery.SaveAutosave(session, 1);
    Thread.Sleep(5);
    var secondRecovery = recovery.SaveAutosave(session, 2);
    Thread.Sleep(5);
    var thirdRecovery = recovery.SaveAutosave(session, 3);
    var retained = recovery.GetCandidates(session.SessionId);
    checks.Check(firstRecovery.Success && secondRecovery.Success && thirdRecovery.Success && retained.Count == 2 &&
        retained[0].Document.Autosave?.Sequence == 3, "recovery autosave is atomic, ordered, and retention-bounded");

    var explicitPath = Path.Combine(recoveryDirectory, "explicit.chesssession.json");
    var explicitNewer = session with { ModifiedUtc = DateTimeOffset.UtcNow.AddMinutes(5), Dirty = false };
    checks.Check(new ChessSessionFileService().Save(explicitPath, explicitNewer).Success &&
        recovery.GetLatestCandidate(explicitPath) is null, "newer explicit session suppresses stale recovery");
    checks.Check(ChessSessionRecoveryService.ShouldScheduleAfterAction(true) &&
        !ChessSessionRecoveryService.ShouldScheduleAfterAction(false), "only accepted actions schedule autosave");

    var latest = recovery.GetLatestCandidate();
    checks.Check(latest is not null, "recovery exposes the latest valid candidate without mutating it");
    if (latest is not null)
    {
        recovery.Discard(latest.Path);
        checks.Check(!File.Exists(latest.Path), "recovery candidate can be explicitly discarded");
    }
}
finally
{
    Directory.Delete(recoveryDirectory, recursive: true);
}

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

static ChessLegalMoveCandidate Candidate(int fromFile, int fromRank, int toFile, int toRank, int piece = 1,
    int captured = 0, int promotion = 0, ChessCastleKind castle = ChessCastleKind.None, bool check = false,
    bool mate = false, bool enPassant = false) => new(fromFile, fromRank, toFile, toRank, promotion,
    Context(fromFile, fromRank, toFile, toRank, piece, captured, promotion, castle, check: check, mate: mate,
        enPassant: enPassant));

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
