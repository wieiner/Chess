using System.Windows.Media;

namespace ChessApp;

internal sealed record Chess3DCellVisualState(
    int X,
    int Y,
    int Z,
    int PieceCode,
    bool IsSelected,
    bool IsLegalTarget,
    bool IsCaptureTarget,
    bool IsCoreCell,
    int StackCount,
    int FusionKind,
    bool IsContested,
    bool IsAnchor,
    bool IsKingInCheck,
    bool IsLastMoveFrom,
    bool IsLastMoveTo);

internal sealed record Chess3DActionVisualHint(
    int ActionKind,
    Chess3DVisualPoint? PrimaryFrom,
    Chess3DVisualPoint? PrimaryTo,
    IReadOnlyList<Chess3DVisualSegment> MirrorMoves,
    int Axis,
    int Layer,
    int QuarterTurns,
    string Notation);

internal readonly record struct Chess3DVisualPoint(int X, int Y, int Z)
{
    public override string ToString() => $"({X},{Y},{Z})";
}

internal readonly record struct Chess3DVisualSegment(Chess3DVisualPoint From, Chess3DVisualPoint To, bool IsPrimary, bool IsBlocked);

internal static class Chess3DTheme
{
    public static readonly Color SceneBackground = Color.FromRgb(58, 64, 71);
    public static readonly Color BoardLight = Color.FromArgb(190, 214, 218, 204);
    public static readonly Color BoardDark = Color.FromArgb(190, 88, 103, 117);
    public static readonly Color WhitePiece = Color.FromRgb(238, 231, 206);
    public static readonly Color BlackPiece = Color.FromRgb(88, 96, 108);
    public static readonly Color Selected = Color.FromArgb(210, 246, 211, 101);
    public static readonly Color LegalTarget = Color.FromArgb(195, 79, 183, 214);
    public static readonly Color CaptureTarget = Color.FromArgb(220, 214, 92, 76);
    public static readonly Color CoreCell = Color.FromArgb(72, 100, 142, 220);
    public static readonly Color StackBadge = Color.FromArgb(230, 244, 190, 88);
    public static readonly Color FusionFriendly = Color.FromArgb(170, 102, 196, 138);
    public static readonly Color FusionRoyal = Color.FromArgb(215, 246, 211, 101);
    public static readonly Color FusionContested = Color.FromArgb(210, 224, 82, 98);
    public static readonly Color FusionImplosion = Color.FromArgb(220, 180, 110, 235);
    public static readonly Color Anchor = Color.FromArgb(210, 255, 240, 150);
    public static readonly Color HodgePrimaryArrow = Color.FromArgb(230, 98, 190, 255);
    public static readonly Color HodgeMirrorArrow = Color.FromArgb(210, 175, 132, 255);
    public static readonly Color HodgeBlockedArrow = Color.FromArgb(230, 230, 80, 95);
    public static readonly Color RubikLayer = Color.FromArgb(92, 112, 196, 255);
    public static readonly Color ActionFlash = Color.FromArgb(210, 255, 255, 255);
}
