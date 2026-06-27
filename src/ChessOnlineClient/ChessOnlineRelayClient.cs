using ChessOnlineProtocol;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChessOnlineClient;

public sealed class ChessOnlineRelayClient : IAsyncDisposable
{
    private readonly ChessOnlineClientSession _session;
    private readonly HubConnection _connection;
    private long _clientSeq;

    public ChessOnlineRelayClient(ChessOnlineClientSession session)
    {
        _session = session;
        _connection = CreateHubConnection(session.Endpoint, () => session.Token?.AccessToken ?? "");
        RegisterEvents(_connection);
    }

    public HubConnectionState State => _connection.State;

    public ChessOnlineClientEventLog EventLog { get; } = new();

    public OnlineProtocolMessage? LastSnapshot { get; private set; }

    public OnlineProtocolMessage? LastActionLog { get; private set; }

    public OnlineProtocolMessage? LastLegalPreview { get; private set; }

    public OnlineMatchmakingStatus? LastMatchmakingStatus { get; private set; }

    public event Action<string, OnlineProtocolMessage>? MessageReceived;

    public static HubConnection CreateHubConnection(ChessOnlineServerEndpoint endpoint, Func<string> accessTokenProvider)
    {
        return new HubConnectionBuilder()
            .WithUrl(endpoint.HubUri, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessTokenProvider());
            })
            .WithAutomaticReconnect()
            .Build();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connection.StartAsync(cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _connection.StopAsync(cancellationToken);
    }

    public Task<OnlineProtocolMessage> HelloAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return InvokeAsync("Hello", Message(OnlineMessageTypes.Hello, clientId), cancellationToken);
    }

    public Task<OnlineProtocolMessage> JoinMatchmakingAsync(
        string clientId,
        string rulesetId,
        CancellationToken cancellationToken = default)
    {
        var message = Message(OnlineMessageTypes.JoinMatchmaking, clientId);
        message.Matchmaking = new OnlineMatchmakingCommand
        {
            RequestedRulesetId = rulesetId,
            ExpireSeconds = 120
        };
        return InvokeAsync("JoinMatchmaking", message, cancellationToken);
    }

    public Task<OnlineProtocolMessage> ReadyAsync(string clientId, string roomId, string tableId, CancellationToken cancellationToken = default)
    {
        var message = Message(OnlineMessageTypes.Ready, clientId, roomId, tableId);
        message.Table = new OnlineTableCommand { Ready = true };
        return InvokeAsync("Ready", message, cancellationToken);
    }

    public Task<OnlineProtocolMessage> StartGameAsync(string clientId, string roomId, string tableId, CancellationToken cancellationToken = default)
    {
        return InvokeAsync("StartGame", Message(OnlineMessageTypes.StartGame, clientId, roomId, tableId), cancellationToken);
    }

    public Task<OnlineProtocolMessage> RequestSnapshotAsync(string clientId, string roomId, string tableId, CancellationToken cancellationToken = default)
    {
        return InvokeAsync("RequestSnapshot", Message(OnlineMessageTypes.RequestSnapshot, clientId, roomId, tableId), cancellationToken);
    }

    public Task<OnlineProtocolMessage> RequestActionLogAsync(string clientId, string roomId, string tableId, CancellationToken cancellationToken = default)
    {
        return InvokeAsync("RequestActionLog", Message(OnlineMessageTypes.RequestActionLog, clientId, roomId, tableId), cancellationToken);
    }

    public Task<OnlineProtocolMessage> RequestLegalPreviewAsync(
        string clientId,
        string roomId,
        string tableId,
        OnlineLegalPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var message = Message(OnlineMessageTypes.RequestLegalPreview, clientId, roomId, tableId);
        request.RoomId = string.IsNullOrWhiteSpace(request.RoomId) ? roomId : request.RoomId;
        request.TableId = string.IsNullOrWhiteSpace(request.TableId) ? tableId : request.TableId;
        request.PlayerId = string.IsNullOrWhiteSpace(request.PlayerId) ? _session.PlayerId : request.PlayerId;
        message.LegalPreviewRequest = request;
        return InvokeAsync("RequestLegalPreview", message, cancellationToken);
    }

    public Task<OnlineProtocolMessage> SubmitActionAsync(
        string clientId,
        string roomId,
        string tableId,
        OnlineActionCommand action,
        CancellationToken cancellationToken = default)
    {
        var message = Message(OnlineMessageTypes.SubmitAction, clientId, roomId, tableId);
        message.Action = action;
        return InvokeAsync("SubmitAction", message, cancellationToken);
    }

    public OnlineProtocolMessage Message(string messageType, string clientId, string roomId = "", string tableId = "")
    {
        return new OnlineProtocolMessage
        {
            Envelope = new OnlineMessageEnvelope
            {
                MessageType = messageType,
                MessageId = Guid.NewGuid().ToString("N"),
                ClientId = clientId,
                PlayerId = _session.PlayerId,
                RoomId = roomId,
                TableId = tableId,
                ClientSeq = Interlocked.Increment(ref _clientSeq),
                SentAtUtc = DateTime.UtcNow.ToString("O")
            }
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private async Task<OnlineProtocolMessage> InvokeAsync(string methodName, OnlineProtocolMessage message, CancellationToken cancellationToken)
    {
        var result = await _connection.InvokeAsync<OnlineProtocolMessage>(methodName, message, cancellationToken);
        Remember(methodName, result);
        return result;
    }

    private void RegisterEvents(HubConnection connection)
    {
        foreach (var eventName in ChessOnlineRelayEvents.All)
        {
            connection.On<OnlineProtocolMessage>(eventName, message => Remember(eventName, message));
        }

        connection.Closed += error =>
        {
            EventLog.Add(error == null ? "SignalR closed" : $"SignalR closed: {error.Message}");
            return Task.CompletedTask;
        };
    }

    private void Remember(string label, OnlineProtocolMessage message)
    {
        EventLog.Add($"{label}: {message.Envelope.MessageType} seq={message.Envelope.ServerSeq} error={message.Error?.ReasonCode ?? OnlineRejectReasons.None}");
        if (message.Snapshot != null)
        {
            LastSnapshot = message;
        }
        if (message.ActionLog != null)
        {
            LastActionLog = message;
        }
        if (message.LegalPreview != null)
        {
            LastLegalPreview = message;
        }
        if (message.MatchmakingStatus != null)
        {
            LastMatchmakingStatus = message.MatchmakingStatus;
        }
        MessageReceived?.Invoke(label, message);
    }
}

public static class ChessOnlineRelayEvents
{
    public static IReadOnlyList<string> All { get; } = new[]
    {
        "ReceiveWelcome",
        "ReceiveRoomCreated",
        "ReceiveRoomJoined",
        "ReceiveRoomLeft",
        "ReceiveRoomList",
        "ReceiveTableCreated",
        "ReceiveTableState",
        "ReceiveSeatAssigned",
        "ReceiveGameStarted",
        "ReceiveActionAccepted",
        "ReceiveActionRejected",
        "ReceiveAuthoritativeSnapshot",
        "ReceiveActionLogChunk",
        "ReceiveLegalPreviewResult",
        "ReceiveResyncRequired",
        "ReceiveMatchmakingStatus",
        "ReceiveMatchmakingCancelled",
        "ReceiveMatchFound",
        "ReceiveMatchmakingError",
        "ReceivePong",
        "ReceiveError",
        "ReceiveDiagnostics"
    };
}
