namespace ChessGameRecords;

public enum ChessSanResolutionError
{
    None = 0,
    IllegalSan,
    AmbiguousSan,
    WrongCaptureMarker,
    WrongCheckSuffix,
    WrongPromotion,
    UnexpectedCastle,
    NoMatchingMove,
    MultipleMatchingMoves
}

public sealed record ChessLegalMoveCandidate(
    int FromFile,
    int FromRank,
    int ToFile,
    int ToRank,
    int PromotionPiece,
    ChessSanMoveContext Context);

public sealed record ChessSanResolutionResult(
    bool Success,
    ChessLegalMoveCandidate? Move,
    ChessSanResolutionError Error,
    string Message);

public static class ChessSanResolver
{
    public static ChessSanResolutionResult Resolve(string san, IEnumerable<ChessLegalMoveCandidate> legalMoves)
    {
        ArgumentNullException.ThrowIfNull(legalMoves);
        var candidates = legalMoves.ToArray();
        if (!TryParse(san, out var constraints, out var parseError))
        {
            return Failed(parseError, $"Invalid SAN '{san}'.");
        }

        var structural = candidates.Where(candidate => MatchesStructure(constraints, candidate.Context)).ToArray();
        if (structural.Length == 0)
        {
            var error = ClassifyMismatch(constraints, candidates);
            return Failed(error, $"SAN '{san}' does not match a current legal move.");
        }
        var exact = structural.Where(candidate => SuffixMatches(constraints, candidate.Context)).ToArray();
        if (exact.Length == 0)
        {
            return Failed(ChessSanResolutionError.WrongCheckSuffix, $"SAN '{san}' has an incorrect check or mate suffix.");
        }
        if (exact.Length > 1)
        {
            return Failed(constraints.HasQualifier ? ChessSanResolutionError.MultipleMatchingMoves : ChessSanResolutionError.AmbiguousSan,
                $"SAN '{san}' matches more than one legal move.");
        }
        return new ChessSanResolutionResult(true, exact[0], ChessSanResolutionError.None, string.Empty);
    }

    private static bool MatchesStructure(SanConstraints san, ChessSanMoveContext move)
    {
        if (san.Castle != ChessCastleKind.None)
        {
            return move.CastleKind == san.Castle;
        }
        if (move.CastleKind != ChessCastleKind.None || Math.Abs(move.MovedPiece) != san.Piece ||
            move.ToFile != san.ToFile || move.ToRank != san.ToRank || move.IsCapture != san.IsCapture ||
            move.PromotionPiece != san.Promotion)
        {
            return false;
        }
        return (san.FromFile is null || move.FromFile == san.FromFile) &&
               (san.FromRank is null || move.FromRank == san.FromRank);
    }

    private static bool SuffixMatches(SanConstraints san, ChessSanMoveContext move) =>
        san.IsMate == move.IsCheckmate && san.IsCheck == move.IsCheck;

    private static ChessSanResolutionError ClassifyMismatch(SanConstraints san, ChessLegalMoveCandidate[] moves)
    {
        if (san.Castle != ChessCastleKind.None && moves.All(move => move.Context.CastleKind == ChessCastleKind.None))
        {
            return ChessSanResolutionError.UnexpectedCastle;
        }
        var samePieceTarget = moves.Where(move => Math.Abs(move.Context.MovedPiece) == san.Piece &&
            move.ToFile == san.ToFile && move.ToRank == san.ToRank).ToArray();
        if (samePieceTarget.Any(move => move.Context.IsCapture != san.IsCapture))
        {
            return ChessSanResolutionError.WrongCaptureMarker;
        }
        if (samePieceTarget.Any(move => move.PromotionPiece != san.Promotion))
        {
            return ChessSanResolutionError.WrongPromotion;
        }
        return ChessSanResolutionError.NoMatchingMove;
    }

    private static bool TryParse(string san, out SanConstraints result, out ChessSanResolutionError error)
    {
        result = default;
        error = ChessSanResolutionError.IllegalSan;
        if (string.IsNullOrWhiteSpace(san) || san.Any(char.IsWhiteSpace))
        {
            return false;
        }
        var text = san;
        var mate = text.EndsWith('#');
        var check = mate || text.EndsWith('+');
        if (check)
        {
            text = text[..^1];
        }
        if (text is "O-O" or "O-O-O")
        {
            result = new SanConstraints(6, -1, -1, null, null, false, 0,
                text == "O-O" ? ChessCastleKind.KingSide : ChessCastleKind.QueenSide, check, mate);
            return true;
        }
        if (text.StartsWith("O-", StringComparison.Ordinal))
        {
            error = ChessSanResolutionError.UnexpectedCastle;
            return false;
        }

        var promotion = 0;
        var equals = text.LastIndexOf('=');
        if (equals >= 0)
        {
            if (equals != text.Length - 2 || !TryPiece(text[^1], out promotion) || promotion is 1 or 6)
            {
                error = ChessSanResolutionError.WrongPromotion;
                return false;
            }
            text = text[..equals];
        }
        if (text.Length < 2 || !TrySquare(text[^2], text[^1], out var toFile, out var toRank))
        {
            return false;
        }
        var prefix = text[..^2];
        var piece = 1;
        if (prefix.Length > 0 && TryPiece(prefix[0], out var parsedPiece) && parsedPiece != 1)
        {
            piece = parsedPiece;
            prefix = prefix[1..];
        }
        var capture = prefix.EndsWith('x');
        if (capture)
        {
            prefix = prefix[..^1];
        }
        else if (prefix.Contains('x'))
        {
            error = ChessSanResolutionError.WrongCaptureMarker;
            return false;
        }
        int? fromFile = null;
        int? fromRank = null;
        foreach (var character in prefix)
        {
            if (character is >= 'a' and <= 'h' && fromFile is null) fromFile = character - 'a';
            else if (character is >= '1' and <= '8' && fromRank is null) fromRank = character - '1';
            else return false;
        }
        if (piece == 1 && capture != (fromFile is not null) || piece == 1 && fromRank is not null ||
            promotion != 0 && piece != 1)
        {
            return false;
        }
        result = new SanConstraints(piece, toFile, toRank, fromFile, fromRank, capture, promotion,
            ChessCastleKind.None, check, mate);
        error = ChessSanResolutionError.None;
        return true;
    }

    private static bool TrySquare(char file, char rank, out int fileIndex, out int rankIndex)
    {
        fileIndex = file - 'a';
        rankIndex = rank - '1';
        return fileIndex is >= 0 and < 8 && rankIndex is >= 0 and < 8;
    }

    private static bool TryPiece(char value, out int piece)
    {
        piece = value switch { 'N' => 2, 'B' => 3, 'R' => 4, 'Q' => 5, 'K' => 6, _ => 0 };
        return piece != 0;
    }

    private static ChessSanResolutionResult Failed(ChessSanResolutionError error, string message) =>
        new(false, null, error, message);

    private readonly record struct SanConstraints(int Piece, int ToFile, int ToRank, int? FromFile, int? FromRank,
        bool IsCapture, int Promotion, ChessCastleKind Castle, bool IsCheck, bool IsMate)
    {
        public bool HasQualifier => FromFile is not null || FromRank is not null;
    }
}
