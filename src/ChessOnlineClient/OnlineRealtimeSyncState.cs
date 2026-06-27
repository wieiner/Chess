using ChessOnlineProtocol;

namespace ChessOnlineClient;

public sealed class OnlineRealtimeSyncState
{
    public long LastServerSeq { get; private set; }
    public string LastSnapshotHash { get; private set; } = "";
    public int DuplicateEventCount { get; private set; }
    public int GapEventCount { get; private set; }
    public bool ResyncRequired { get; private set; }
    public string LastReason { get; private set; } = "idle";
    public string LastMessageType { get; private set; } = "";

    public string Summary =>
        $"Realtime: seq={LastServerSeq} hash={ShortHash(LastSnapshotHash)} duplicates={DuplicateEventCount} gaps={GapEventCount} resync={(ResyncRequired ? "yes" : "no")} reason={LastReason}";

    public OnlineRealtimeObservation Observe(OnlineProtocolMessage? message)
    {
        if (message == null)
        {
            LastReason = "missing message";
            return new OnlineRealtimeObservation(false, false, ResyncRequired, LastReason, LastServerSeq);
        }

        LastMessageType = message.Envelope.MessageType;
        var seq = message.Envelope.ServerSeq;
        var duplicate = false;
        var gap = false;

        if (seq > 0)
        {
            duplicate = seq <= LastServerSeq;
            if (duplicate)
            {
                DuplicateEventCount++;
                if (IsResyncMessage(message))
                {
                    ResyncRequired = true;
                    LastReason = string.IsNullOrWhiteSpace(message.Error?.ReasonText)
                        ? $"duplicate seq {seq}; resync required"
                        : message.Error!.ReasonText;
                }
                else
                {
                    LastReason = $"duplicate seq {seq}";
                }
                return new OnlineRealtimeObservation(true, false, ResyncRequired, LastReason, LastServerSeq);
            }

            gap = LastServerSeq > 0 && seq > LastServerSeq + 1;
            if (gap)
            {
                GapEventCount++;
                ResyncRequired = true;
                LastReason = $"gap {LastServerSeq}->{seq}";
            }

            LastServerSeq = seq;
        }

        if (message.Snapshot != null)
        {
            LastSnapshotHash = message.Snapshot.StateHash;
            if (!message.Envelope.MessageType.Equals(OnlineMessageTypes.ResyncRequired, StringComparison.Ordinal))
            {
                ResyncRequired = false;
            }
            if (!gap)
            {
                LastReason = "snapshot";
            }
        }

        if (IsResyncMessage(message))
        {
            ResyncRequired = true;
            LastReason = string.IsNullOrWhiteSpace(message.Error?.ReasonText)
                ? "resync required"
                : message.Error!.ReasonText;
        }

        if (!duplicate && !gap && string.IsNullOrWhiteSpace(message.Error?.ReasonCode) && message.Snapshot == null)
        {
            LastReason = string.IsNullOrWhiteSpace(LastMessageType) ? "event" : LastMessageType;
        }

        return new OnlineRealtimeObservation(duplicate, gap, ResyncRequired, LastReason, LastServerSeq);
    }

    public void MarkConnectionState(string state)
    {
        LastReason = state;
    }

    public void ClearResync()
    {
        ResyncRequired = false;
        LastReason = "resync cleared";
    }

    private static string ShortHash(string hash) =>
        string.IsNullOrWhiteSpace(hash) ? "none" : hash.Length <= 12 ? hash : hash[..12];

    private static bool IsResyncMessage(OnlineProtocolMessage message) =>
        message.Envelope.MessageType.Equals(OnlineMessageTypes.ResyncRequired, StringComparison.Ordinal) ||
        message.Error?.ReasonCode == OnlineRejectReasons.StaleStateHash;
}

public sealed record OnlineRealtimeObservation(
    bool IsDuplicate,
    bool HasGap,
    bool RequiresResync,
    string Reason,
    long LastServerSeq);
