using ChessOnlineProtocol;

namespace ChessOnlineClient;

public sealed class LegalPreviewState
{
    public string StateHash { get; init; } = "";
    public long ServerSeq { get; init; }
    public int SourceX { get; init; }
    public int SourceY { get; init; }
    public int SourceZ { get; init; }
    public int ActorSide { get; init; }
    public int MacroPlayer { get; init; }
    public bool IsStale { get; init; }
    public string Reason { get; init; } = "";
    public IReadOnlyList<LegalActionOptionViewModel> Options { get; init; } = Array.Empty<LegalActionOptionViewModel>();
    public IReadOnlyList<LegalTargetMarker> Targets { get; init; } = Array.Empty<LegalTargetMarker>();

    public static LegalPreviewState Empty(string reason = "")
    {
        return new LegalPreviewState { Reason = reason };
    }

    public static LegalPreviewState FromMessage(OnlineProtocolMessage? message)
    {
        if (message?.LegalPreview == null)
        {
            return Empty("No legal preview is available.");
        }

        var preview = message.LegalPreview;
        var options = preview.Options
            .Select(LegalActionOptionViewModel.FromOption)
            .ToArray();
        return new LegalPreviewState
        {
            StateHash = preview.StateHash,
            ServerSeq = preview.ServerSeq,
            SourceX = preview.SourceX,
            SourceY = preview.SourceY,
            SourceZ = preview.SourceZ,
            ActorSide = preview.ActorSide,
            MacroPlayer = preview.MacroPlayer,
            IsStale = preview.IsStale,
            Reason = preview.Error?.ReasonText ?? preview.NoLegalActionReason,
            Options = options,
            Targets = options
                .Where(o => o.HasBoardTarget)
                .Select(o => new LegalTargetMarker
                {
                    X = o.ToX,
                    Y = o.ToY,
                    Z = o.ToZ,
                    ActionKind = o.ActionKind,
                    IsCapture = o.IsCapture,
                    IsSpecial = o.IsSpecial,
                    DisplayLabel = o.DisplayLabel
                })
                .ToArray()
        };
    }
}

public sealed class LegalActionOptionViewModel
{
    public string ActionKind { get; init; } = "";
    public int ActorSide { get; init; }
    public int MacroPlayer { get; init; }
    public int FromX { get; init; }
    public int FromY { get; init; }
    public int FromZ { get; init; }
    public int ToX { get; init; }
    public int ToY { get; init; }
    public int ToZ { get; init; }
    public int Axis { get; init; }
    public int Layer { get; init; }
    public int QuarterTurns { get; init; }
    public int PrimarySide { get; init; }
    public bool IsCapture { get; init; }
    public bool IsSpecial { get; init; }
    public string DisplayLabel { get; init; } = "";
    public string Reason { get; init; } = "";
    public OnlineActionCommand Command { get; init; } = new();

    public bool HasBoardTarget => ToX >= 0 && ToY >= 0 && ToZ >= 0;

    public static LegalActionOptionViewModel FromOption(OnlineLegalActionOption option)
    {
        return new LegalActionOptionViewModel
        {
            ActionKind = option.ActionKind,
            ActorSide = option.ActorSide,
            MacroPlayer = option.MacroPlayer,
            FromX = option.From.X,
            FromY = option.From.Y,
            FromZ = option.From.Z,
            ToX = option.To.X,
            ToY = option.To.Y,
            ToZ = option.To.Z,
            Axis = option.Axis,
            Layer = option.Layer,
            QuarterTurns = option.QuarterTurns,
            PrimarySide = option.PrimarySide,
            IsCapture = option.IsCapture,
            IsSpecial = option.IsSpecial,
            DisplayLabel = string.IsNullOrWhiteSpace(option.DisplayLabel) ? option.ActionKind : option.DisplayLabel,
            Reason = option.Reason,
            Command = ToActionCommand(option)
        };
    }

    private static OnlineActionCommand ToActionCommand(OnlineLegalActionOption option)
    {
        return new OnlineActionCommand
        {
            ActionKind = option.ActionKind,
            ActorSide = option.ActorSide,
            MacroPlayer = option.MacroPlayer,
            FromX = option.From.X,
            FromY = option.From.Y,
            FromZ = option.From.Z,
            ToX = option.To.X,
            ToY = option.To.Y,
            ToZ = option.To.Z,
            PromotionType = option.PromotionType,
            Side = option.Side,
            PieceType = option.PieceType,
            X = option.ReserveTarget.X,
            Y = option.ReserveTarget.Y,
            Z = option.ReserveTarget.Z,
            PrimarySide = option.PrimarySide,
            Axis = option.Axis,
            Layer = option.Layer,
            QuarterTurns = option.QuarterTurns
        };
    }
}

public sealed class LegalTargetMarker
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Z { get; init; }
    public string ActionKind { get; init; } = "";
    public bool IsCapture { get; init; }
    public bool IsSpecial { get; init; }
    public string DisplayLabel { get; init; } = "";
}
