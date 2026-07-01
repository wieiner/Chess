using ChessOnlineProtocol;

namespace ChessOnlineClient;

public sealed class OnlineSpectatorClientState
{
    public bool IsSpectator { get; private set; }
    public string SpectatorRoomId { get; private set; } = "";
    public string SpectatorTableId { get; private set; } = "";
    public string SpectatorRulesetId { get; private set; } = "";
    public string SpectatorId { get; private set; } = "";
    public string SubmitDisabledReason { get; private set; } = "Spectator mode is not active.";
    public long LastKnownServerSeq { get; private set; }

    public void Clear()
    {
        IsSpectator = false;
        SpectatorRoomId = "";
        SpectatorTableId = "";
        SpectatorRulesetId = "";
        SpectatorId = "";
        SubmitDisabledReason = "Spectator mode is not active.";
        LastKnownServerSeq = 0;
    }

    public void Apply(OnlineJoinSpectatorResult? result)
    {
        if (result?.Success != true)
        {
            Clear();
            SubmitDisabledReason = string.IsNullOrWhiteSpace(result?.FailureText)
                ? "Spectator mode is not active."
                : result.FailureText;
            return;
        }

        IsSpectator = true;
        SpectatorRoomId = result.RoomId;
        SpectatorTableId = result.TableId;
        SpectatorRulesetId = result.RulesetId;
        SpectatorId = result.SpectatorId;
        LastKnownServerSeq = result.State.LastKnownServerSeq;
        SubmitDisabledReason = string.IsNullOrWhiteSpace(result.State.SubmitDisabledReason)
            ? "Spectator mode is read-only."
            : result.State.SubmitDisabledReason;
    }
}
