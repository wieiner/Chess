namespace ChessOnlineClient;

public enum OnlineConnectionState
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Reconnecting = 3,
    Reconnected = 4,
    Closed = 5
}

public enum OnlineReconnectEvent
{
    Initialize = 0,
    BeginConnect = 1,
    Connected = 2,
    Reconnecting = 3,
    Reconnected = 4,
    Closed = 5,
    Disconnected = 6
}

public sealed class OnlineReconnectState
{
    public OnlineConnectionState State { get; private set; } = OnlineConnectionState.Disconnected;
    public string LastConnectionId { get; private set; } = "";
    public int ReconnectAttemptCount { get; private set; }
    public string LastSafeError { get; private set; } = "";
    public DateTime LastTransitionUtc { get; private set; } = DateTime.UtcNow;
    public OnlineReconnectEvent LastEvent { get; private set; } = OnlineReconnectEvent.Initialize;

    public bool ShouldDisableSubmit =>
        State is OnlineConnectionState.Disconnected or
            OnlineConnectionState.Connecting or
            OnlineConnectionState.Reconnecting or
            OnlineConnectionState.Closed;

    public bool ShouldRequestSnapshotAfterReconnect { get; private set; }

    public bool ShouldRequestActionLogAfterReconnect { get; private set; }

    public bool IsPlayable => State is OnlineConnectionState.Connected or OnlineConnectionState.Reconnected;

    public OnlineReconnectSummary Summary => new(
        State,
        ShortConnectionId(LastConnectionId),
        ReconnectAttemptCount,
        LastSafeError,
        LastTransitionUtc,
        ShouldDisableSubmit,
        ShouldRequestSnapshotAfterReconnect,
        ShouldRequestActionLogAfterReconnect,
        IsPlayable);

    public OnlineConnectionHealthSnapshot HealthSnapshot => new(
        State,
        ShortConnectionId(LastConnectionId),
        ReconnectAttemptCount,
        LastSafeError,
        LastTransitionUtc,
        ShouldDisableSubmit,
        ShouldRequestSnapshotAfterReconnect,
        ShouldRequestActionLogAfterReconnect);

    public void MarkConnecting(DateTime? utc = null)
    {
        Transition(OnlineReconnectEvent.BeginConnect, OnlineConnectionState.Connecting, "", "", utc);
        ShouldRequestSnapshotAfterReconnect = false;
        ShouldRequestActionLogAfterReconnect = false;
    }

    public void MarkConnected(string connectionId, DateTime? utc = null)
    {
        Transition(OnlineReconnectEvent.Connected, OnlineConnectionState.Connected, connectionId, "", utc);
        ReconnectAttemptCount = 0;
        ShouldRequestSnapshotAfterReconnect = false;
        ShouldRequestActionLogAfterReconnect = false;
    }

    public void MarkReconnecting(Exception? error = null, DateTime? utc = null)
    {
        ReconnectAttemptCount++;
        Transition(OnlineReconnectEvent.Reconnecting, OnlineConnectionState.Reconnecting, LastConnectionId, SafeError(error), utc);
        ShouldRequestSnapshotAfterReconnect = false;
        ShouldRequestActionLogAfterReconnect = false;
    }

    public void MarkReconnected(string connectionId, DateTime? utc = null)
    {
        Transition(OnlineReconnectEvent.Reconnected, OnlineConnectionState.Reconnected, connectionId, "", utc);
        ShouldRequestSnapshotAfterReconnect = true;
        ShouldRequestActionLogAfterReconnect = true;
    }

    public void MarkClosed(Exception? error = null, DateTime? utc = null)
    {
        Transition(OnlineReconnectEvent.Closed, OnlineConnectionState.Closed, LastConnectionId, SafeError(error), utc);
        ShouldRequestSnapshotAfterReconnect = false;
        ShouldRequestActionLogAfterReconnect = false;
    }

    public void MarkDisconnected(DateTime? utc = null)
    {
        Transition(OnlineReconnectEvent.Disconnected, OnlineConnectionState.Disconnected, "", "", utc);
        ReconnectAttemptCount = 0;
        ShouldRequestSnapshotAfterReconnect = false;
        ShouldRequestActionLogAfterReconnect = false;
    }

    public void ClearResyncRequest()
    {
        ShouldRequestSnapshotAfterReconnect = false;
        ShouldRequestActionLogAfterReconnect = false;
    }

    private void Transition(
        OnlineReconnectEvent reconnectEvent,
        OnlineConnectionState state,
        string connectionId,
        string safeError,
        DateTime? utc)
    {
        LastEvent = reconnectEvent;
        State = state;
        LastConnectionId = connectionId ?? "";
        LastSafeError = safeError ?? "";
        LastTransitionUtc = utc ?? DateTime.UtcNow;
    }

    private static string SafeError(Exception? error)
    {
        return error == null ? "" : ChessOnlineSecretRedactor.Redact(error.Message);
    }

    private static string ShortConnectionId(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return "";
        }

        return connectionId.Length <= 8
            ? connectionId
            : $"{connectionId[..4]}...{connectionId[^4..]}";
    }
}

public sealed record OnlineReconnectSummary(
    OnlineConnectionState State,
    string ConnectionIdShort,
    int ReconnectAttemptCount,
    string LastSafeError,
    DateTime LastTransitionUtc,
    bool ShouldDisableSubmit,
    bool ShouldRequestSnapshotAfterReconnect,
    bool ShouldRequestActionLogAfterReconnect,
    bool IsPlayable)
{
    public override string ToString()
    {
        var error = string.IsNullOrWhiteSpace(LastSafeError) ? "" : $" error={LastSafeError}";
        var connection = string.IsNullOrWhiteSpace(ConnectionIdShort) ? "" : $" connection={ConnectionIdShort}";
        return $"state={State}{connection} attempts={ReconnectAttemptCount} disableSubmit={ShouldDisableSubmit} " +
            $"resyncSnapshot={ShouldRequestSnapshotAfterReconnect} resyncActionLog={ShouldRequestActionLogAfterReconnect}{error}";
    }
}

public sealed record OnlineConnectionHealthSnapshot(
    OnlineConnectionState State,
    string ConnectionIdShort,
    int ReconnectAttemptCount,
    string LastSafeError,
    DateTime LastTransitionUtc,
    bool ShouldDisableSubmit,
    bool ShouldRequestSnapshotAfterReconnect,
    bool ShouldRequestActionLogAfterReconnect);
