using ChessOnlineProtocol;

namespace ChessOnlineClient;

public static class OnlinePreviewActionDispatchPolicy
{
    public static bool CanSubmitFromGenericBoard(string? actionKind, out string disabledReason)
    {
        if (string.Equals(actionKind, OnlineActionKinds.NormalMove, StringComparison.Ordinal))
        {
            disabledReason = "";
            return true;
        }

        disabledReason = DisabledReason(actionKind);
        return false;
    }

    public static string DisabledReason(string? actionKind)
    {
        return actionKind switch
        {
            OnlineActionKinds.RubikLayerTurn => "Rubik layer action requires the Rubik Layer Actions panel.",
            OnlineActionKinds.HodgeProjectedMove => "Hodge projection action requires the Hodge Projection Actions panel.",
            OnlineActionKinds.ReserveRestore => "Reserve restore requires an explicit reserve restore control.",
            null or "" => "Unsupported online action kind: empty.",
            _ => $"Unsupported online action kind: {actionKind}."
        };
    }
}
