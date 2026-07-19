using System.Text;

namespace ChessGameRecords;

[Flags]
public enum ChessSanDisambiguation
{
    None = 0,
    File = 1,
    Rank = 2
}

public enum ChessCastleKind
{
    None = 0,
    KingSide = 1,
    QueenSide = 2
}

public enum ChessSanError
{
    None = 0,
    IllegalMove,
    InvalidCoordinates,
    InvalidPiece,
    InvalidCapture,
    InvalidDisambiguation,
    InvalidCastle,
    InvalidPromotion,
    InvalidCheckState
}

public sealed record ChessSanMoveContext(
    bool IsLegal,
    int FromFile,
    int FromRank,
    int ToFile,
    int ToRank,
    int MovedPiece,
    int CapturedPiece,
    int PromotionPiece,
    bool IsCapture,
    bool IsEnPassant,
    ChessCastleKind CastleKind,
    ChessSanDisambiguation Disambiguation,
    bool IsCheck,
    bool IsCheckmate);

public sealed record ChessSanParserToken(
    string PieceDesignator,
    string OriginQualifier,
    bool IsCapture,
    string Destination,
    string Promotion,
    string CheckSuffix);

public sealed record ChessSanGenerationResult(
    bool Success,
    string San,
    ChessSanParserToken? Token,
    ChessSanError Error,
    string Message)
{
    internal static ChessSanGenerationResult Failed(ChessSanError error, string message) =>
        new(false, string.Empty, null, error, message);
}

public static class ChessSanGenerator
{
    private const int Pawn = 1;
    private const int Knight = 2;
    private const int Bishop = 3;
    private const int Rook = 4;
    private const int Queen = 5;
    private const int King = 6;

    public static ChessSanGenerationResult Generate(ChessSanMoveContext context)
    {
        if (!context.IsLegal)
        {
            return ChessSanGenerationResult.Failed(ChessSanError.IllegalMove, "SAN requires an exact legal move.");
        }
        if (!IsSquare(context.FromFile, context.FromRank) || !IsSquare(context.ToFile, context.ToRank))
        {
            return ChessSanGenerationResult.Failed(ChessSanError.InvalidCoordinates, "Move coordinates must be on the 8x8 board.");
        }

        var pieceType = Math.Abs(context.MovedPiece);
        var pieceLetter = PieceLetter(pieceType);
        if (context.MovedPiece == 0 || pieceLetter is null)
        {
            return ChessSanGenerationResult.Failed(ChessSanError.InvalidPiece, "Moved piece code is invalid.");
        }
        if (context.IsCheckmate && !context.IsCheck)
        {
            return ChessSanGenerationResult.Failed(ChessSanError.InvalidCheckState, "Checkmate must also report check.");
        }

        var captureTruth = context.CapturedPiece != 0 || context.IsEnPassant;
        if (context.IsCapture != captureTruth || (context.IsEnPassant && pieceType != Pawn))
        {
            return ChessSanGenerationResult.Failed(ChessSanError.InvalidCapture, "Capture flags and captured-piece facts are inconsistent.");
        }
        if ((context.Disambiguation & ~(ChessSanDisambiguation.File | ChessSanDisambiguation.Rank)) != 0 ||
            (pieceType == Pawn && context.Disambiguation != ChessSanDisambiguation.None))
        {
            return ChessSanGenerationResult.Failed(ChessSanError.InvalidDisambiguation, "Disambiguation is invalid for this move.");
        }

        var suffix = context.IsCheckmate ? "#" : context.IsCheck ? "+" : string.Empty;
        if (context.CastleKind != ChessCastleKind.None)
        {
            if (!IsValidCastle(context, pieceType))
            {
                return ChessSanGenerationResult.Failed(ChessSanError.InvalidCastle, "Castling context is inconsistent.");
            }
            var castle = context.CastleKind == ChessCastleKind.KingSide ? "O-O" : "O-O-O";
            var castleToken = new ChessSanParserToken(castle, string.Empty, false, string.Empty, string.Empty, suffix);
            return new ChessSanGenerationResult(true, castle + suffix, castleToken, ChessSanError.None, string.Empty);
        }

        var promotion = PromotionText(context, pieceType);
        if (promotion is null)
        {
            return ChessSanGenerationResult.Failed(ChessSanError.InvalidPromotion, "Promotion context is inconsistent.");
        }

        var origin = BuildOriginQualifier(context, pieceType);
        var destination = SquareName(context.ToFile, context.ToRank);
        var builder = new StringBuilder();
        if (pieceType != Pawn)
        {
            builder.Append(pieceLetter);
        }
        builder.Append(origin);
        if (context.IsCapture)
        {
            builder.Append('x');
        }
        builder.Append(destination);
        builder.Append(promotion);
        builder.Append(suffix);

        var token = new ChessSanParserToken(
            pieceType == Pawn ? string.Empty : pieceLetter,
            origin,
            context.IsCapture,
            destination,
            promotion,
            suffix);
        return new ChessSanGenerationResult(true, builder.ToString(), token, ChessSanError.None, string.Empty);
    }

    private static bool IsValidCastle(ChessSanMoveContext context, int pieceType)
    {
        if (pieceType != King || context.IsCapture || context.IsEnPassant || context.CapturedPiece != 0 ||
            context.PromotionPiece != 0 || context.Disambiguation != ChessSanDisambiguation.None)
        {
            return false;
        }
        var homeRank = context.MovedPiece > 0 ? 0 : 7;
        var expectedFile = context.CastleKind == ChessCastleKind.KingSide ? 6 : 2;
        return context.FromFile == 4 && context.FromRank == homeRank && context.ToFile == expectedFile && context.ToRank == homeRank;
    }

    private static string? PromotionText(ChessSanMoveContext context, int pieceType)
    {
        var reachesLastRank = pieceType == Pawn && (context.ToRank == 0 || context.ToRank == 7);
        if (context.PromotionPiece == 0)
        {
            return reachesLastRank ? null : string.Empty;
        }
        if (!reachesLastRank)
        {
            return null;
        }
        var letter = PieceLetter(Math.Abs(context.PromotionPiece));
        return Math.Abs(context.PromotionPiece) is Knight or Bishop or Rook or Queen ? $"={letter}" : null;
    }

    private static string BuildOriginQualifier(ChessSanMoveContext context, int pieceType)
    {
        if (pieceType == Pawn)
        {
            return context.IsCapture ? ((char)('a' + context.FromFile)).ToString() : string.Empty;
        }
        var builder = new StringBuilder(2);
        if (context.Disambiguation.HasFlag(ChessSanDisambiguation.File))
        {
            builder.Append((char)('a' + context.FromFile));
        }
        if (context.Disambiguation.HasFlag(ChessSanDisambiguation.Rank))
        {
            builder.Append((char)('1' + context.FromRank));
        }
        return builder.ToString();
    }

    private static string? PieceLetter(int pieceType) => pieceType switch
    {
        Pawn => string.Empty,
        Knight => "N",
        Bishop => "B",
        Rook => "R",
        Queen => "Q",
        King => "K",
        _ => null
    };

    private static bool IsSquare(int file, int rank) => file is >= 0 and < 8 && rank is >= 0 and < 8;
    private static string SquareName(int file, int rank) => $"{(char)('a' + file)}{rank + 1}";
}
