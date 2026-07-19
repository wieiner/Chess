using ChessGameRecords;

namespace ChessApp;

internal sealed record ChessPgnImportResult(bool Success, ChessGameRecord? Game, string FinalFen, string Error);

internal static class NativeChessPgnImporter
{
    public static ChessPgnImportResult Import(string text)
    {
        var parsed = PgnParser.Parse(text, new PgnParserOptions(PgnParserMode.ImportTolerant));
        if (!parsed.Success || parsed.Document?.Games.Count != 1)
        {
            var detail = parsed.Diagnostics.FirstOrDefault();
            return Failed(detail is null ? "PGN must contain exactly one game." :
                $"{detail.Message} (line {detail.Location.Line}, column {detail.Location.Column})");
        }
        var game = parsed.Document.Games[0];
        var setup = game.FindTag("SetUp");
        var fen = game.FindTag("FEN");
        var initialFen = setup == "1" && !string.IsNullOrWhiteSpace(fen) ? fen : ChessGameHistory.StandardInitialFen;
        using var engine = new NativeChessEngine();
        if (!engine.SetFen(initialFen))
        {
            return Failed("PGN initial FEN is invalid.");
        }
        var history = new ChessGameHistory(initialFen, Headers(game));
        foreach (var node in game.MainLine.Moves)
        {
            var candidates = BuildCandidates(engine);
            var resolved = ChessSanResolver.Resolve(node.San, candidates);
            if (!resolved.Success || resolved.Move is null)
            {
                return Failed($"Ply {node.PlyIndex + 1}: {resolved.Message}");
            }
            var move = resolved.Move;
            var before = engine.GetState();
            var preFen = engine.GetFen();
            if (!engine.TryGetMoveDescriptor(move.FromFile, move.FromRank, move.ToFile, move.ToRank, move.PromotionPiece, out var descriptor) ||
                !engine.TryMakeMove(move.FromFile, move.FromRank, move.ToFile, move.ToRank, move.PromotionPiece, out var played))
            {
                return Failed($"Ply {node.PlyIndex + 1}: resolved move was rejected by the candidate engine.");
            }
            var record = new ChessMoveRecord(history.Moves.Count, before.FullmoveNumber, before.SideToMove,
                new ChessSquare(move.FromFile, move.FromRank), new ChessSquare(move.ToFile, move.ToRank),
                descriptor.MovedPiece, descriptor.CapturedPiece, played.Promotion, (ChessCastleKind)descriptor.CastleKind,
                (played.Flags & NativeChessEngine.MoveEnPassant) != 0, (played.Flags & NativeChessEngine.MoveCapture) != 0,
                descriptor.ResultingIsCheck != 0, descriptor.ResultingStatus == NativeChessEngine.StatusCheckmate,
                preFen, engine.GetFen(), Uci(played), node.San, null,
                string.Join(" ", node.LeadingComments.Concat(node.TrailingComments).Select(comment => comment.Text)), null);
            if (!history.TryCommit(record, ChessGameResult.Ongoing, ChessTerminationReason.None, out var error))
            {
                return Failed($"Ply {node.PlyIndex + 1}: {error}");
            }
        }

        var state = engine.GetState();
        var expected = game.Result switch
        {
            PgnResult.WhiteWin => ChessGameResult.WhiteWin,
            PgnResult.BlackWin => ChessGameResult.BlackWin,
            PgnResult.Draw => ChessGameResult.Draw,
            _ => ChessGameResult.Ongoing
        };
        if (state.Status == NativeChessEngine.StatusCheckmate)
        {
            var actual = state.SideToMove == NativeChessEngine.White ? ChessGameResult.BlackWin : ChessGameResult.WhiteWin;
            if (expected != actual) return Failed("PGN result does not match the candidate checkmate position.");
            history.UpdateOutcome(actual, ChessTerminationReason.Checkmate);
        }
        else if (state.Status == NativeChessEngine.StatusStalemate)
        {
            if (expected != ChessGameResult.Draw) return Failed("PGN result does not match the candidate stalemate position.");
            history.UpdateOutcome(ChessGameResult.Draw, ChessTerminationReason.Stalemate);
        }
        else if (expected != ChessGameResult.Ongoing)
        {
            history.UpdateOutcome(expected, expected == ChessGameResult.Draw ? ChessTerminationReason.Agreement : ChessTerminationReason.Resignation);
        }
        return new ChessPgnImportResult(true, history.Snapshot(), engine.GetFen(), string.Empty);
    }

    private static ChessLegalMoveCandidate[] BuildCandidates(NativeChessEngine engine) => engine.GetLegalMoves().Select(move =>
    {
        if (!engine.TryGetMoveDescriptor(move.FromFile, move.FromRank, move.ToFile, move.ToRank, move.Promotion, out var descriptor))
            throw new InvalidOperationException("Native legal move descriptor is unavailable.");
        return new ChessLegalMoveCandidate(move.FromFile, move.FromRank, move.ToFile, move.ToRank, move.Promotion,
            new ChessSanMoveContext(true, move.FromFile, move.FromRank, move.ToFile, move.ToRank, descriptor.MovedPiece,
                descriptor.CapturedPiece, move.Promotion, (move.Flags & NativeChessEngine.MoveCapture) != 0,
                (move.Flags & NativeChessEngine.MoveEnPassant) != 0, (ChessCastleKind)descriptor.CastleKind,
                (ChessSanDisambiguation)descriptor.Disambiguation, descriptor.ResultingIsCheck != 0,
                descriptor.ResultingStatus == NativeChessEngine.StatusCheckmate));
    }).ToArray();

    private static ChessGameHeaders Headers(PgnGame game) => new(
        game.FindTag("Event") ?? "?", game.FindTag("Site") ?? "?", game.FindTag("Date") ?? "????.??.??",
        game.FindTag("Round") ?? "?", game.FindTag("White") ?? "White", game.FindTag("Black") ?? "Black",
        game.Tags.Where(tag => !PgnSevenTagRoster.Names.Contains(tag.Name, StringComparer.Ordinal) && tag.Name is not ("SetUp" or "FEN"))
            .GroupBy(tag => tag.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Last().Value));

    private static string Uci(ChessMoveDto move) => $"{(char)('a' + move.FromFile)}{move.FromRank + 1}{(char)('a' + move.ToFile)}{move.ToRank + 1}" +
        (move.Promotion switch { NativeChessEngine.Queen => "q", NativeChessEngine.Rook => "r", NativeChessEngine.Bishop => "b", NativeChessEngine.Knight => "n", _ => "" });
    private static ChessPgnImportResult Failed(string error) => new(false, null, string.Empty, error);
}
