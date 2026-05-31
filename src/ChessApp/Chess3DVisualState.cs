namespace ChessApp;

internal enum Chess3DVisualSelectionState
{
    None,
    CellSelected,
    PieceSelected,
    ActionPreview,
    InvalidTarget,
    AnimationLocked,
    ReplayStepping
}

internal enum Chess3DVisualModeState
{
    Classic,
    SingleSide,
    Asgard,
    Rubik,
    Hodge
}

internal sealed record Chess3DVisualTurnState(
    int CurrentSide,
    int CurrentMacroPlayer,
    string CurrentTurnKind,
    int AllowedActionMask,
    string GamePhase,
    string GameOutcome,
    string CheckStatus);

internal sealed record Chess3DVisualActionState(
    string LastActionKind,
    string LastNotation,
    string LastInvalidReason,
    string LastReplayError,
    string LastLayerTurnInfo,
    string LastProjectionError,
    string LastCaptureInfo,
    string LastRestoreInfo);

internal sealed record Chess3DVisualOptions(
    bool ShowCore,
    bool ShowHodgeArrows,
    bool ShowRubikLayer,
    bool HighContrastPieces,
    string BackgroundTheme);

internal sealed record Chess3DVisualStateSnapshot(
    Chess3DVisualSelectionState SelectionState,
    Chess3DVisualModeState ModeState,
    Chess3DVisualTurnState TurnState,
    Chess3DVisualActionState ActionState,
    Chess3DVisualOptions Options,
    int SelectedX,
    int SelectedY,
    int SelectedZ,
    int LegalPreviewCount,
    int OverlayCount,
    bool IsAnimationLocked)
{
    public static Chess3DVisualStateSnapshot Empty { get; } = new(
        Chess3DVisualSelectionState.None,
        Chess3DVisualModeState.Classic,
        new Chess3DVisualTurnState(1, 0, "sideTurn", 0, "playing", "none", string.Empty),
        new Chess3DVisualActionState(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
        new Chess3DVisualOptions(ShowCore: true, ShowHodgeArrows: true, ShowRubikLayer: true, HighContrastPieces: false, BackgroundTheme: "Neutral"),
        -1,
        -1,
        -1,
        0,
        0,
        false);
}
