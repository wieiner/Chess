using ChessOnlineProtocol;
using Microsoft.AspNetCore.SignalR;

namespace ChessOnlineServer;

public sealed class Chess3DRelayHub : Hub
{
    private readonly OnlineRoomRegistry _registry;
    private readonly OnlineHubConnectionRegistry _connections;
    private readonly HostedOnlineOptions _options;
    private readonly ILogger<Chess3DRelayHub> _logger;

    public Chess3DRelayHub(
        OnlineRoomRegistry registry,
        OnlineHubConnectionRegistry connections,
        HostedOnlineOptions options,
        ILogger<Chess3DRelayHub> logger)
    {
        _registry = registry;
        _connections = connections;
        _options = options;
        _logger = logger;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connections.TryGet(Context.ConnectionId, out var session))
        {
            if (!string.IsNullOrWhiteSpace(session.RoomId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(session.RoomId));
            }
            if (!string.IsNullOrWhiteSpace(session.TableId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, TableGroup(session.TableId));
            }
        }
        _connections.Disconnected(Context.ConnectionId);
        _registry.SetActiveConnectionCount(_connections.ActiveConnectionCount);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<OnlineProtocolMessage> Hello(OnlineProtocolMessage message)
    {
        if (!Validate(message, OnlineMessageTypes.Hello, out var error))
        {
            await SendCaller("ReceiveError", error);
            return error;
        }
        if (!string.IsNullOrWhiteSpace(message.Envelope.SessionToken) &&
            !_connections.CanReconnect(message.Envelope.PlayerId, message.Envelope.SessionToken))
        {
            var invalid = Error(message.Envelope, OnlineRejectReasons.IllegalAction, "Invalid session token.");
            await SendCaller("ReceiveError", invalid);
            return invalid;
        }

        var session = _connections.Hello(Context.ConnectionId, message.Envelope);
        _registry.SetActiveConnectionCount(_connections.ActiveConnectionCount);
        var result = _registry.Hello(WithSession(message.Envelope, session));
        result.Envelope.PlayerId = session.PlayerId;
        result.Envelope.ClientId = session.ClientId;
        result.Envelope.SessionToken = session.SessionToken;

        if (!string.IsNullOrWhiteSpace(session.RoomId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(session.RoomId));
        }
        if (!string.IsNullOrWhiteSpace(session.TableId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TableGroup(session.TableId));
        }

        await SendCaller("ReceiveWelcome", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> CreateRoom(OnlineProtocolMessage message)
    {
        var command = message.Room ?? new OnlineRoomCommand();
        if (string.IsNullOrWhiteSpace(command.RoomId))
        {
            command.RoomId = message.Envelope.RoomId;
        }
        var result = InvokeRegistry(message, OnlineMessageTypes.CreateRoom, env => _registry.CreateRoom(env, command));
        await SendCaller(result.Envelope.MessageType == OnlineMessageTypes.RoomCreated ? "ReceiveRoomCreated" : "ReceiveError", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> JoinRoom(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.JoinRoom, env => _registry.JoinRoom(env));
        if (result.Envelope.MessageType == OnlineMessageTypes.RoomJoined)
        {
            _connections.SetMembership(Context.ConnectionId, result.Envelope.RoomId);
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(result.Envelope.RoomId));
            await Clients.Group(RoomGroup(result.Envelope.RoomId)).SendAsync("ReceiveRoomJoined", result);
        }
        else
        {
            await SendCaller("ReceiveError", result);
        }
        return result;
    }

    public async Task<OnlineProtocolMessage> LeaveRoom(OnlineProtocolMessage message)
    {
        if (_connections.TryGet(Context.ConnectionId, out var session) && !string.IsNullOrWhiteSpace(session.RoomId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(session.RoomId));
        }
        var result = Reply(OnlineMessageTypes.RoomLeft, message.Envelope);
        await SendCaller("ReceiveRoomLeft", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> ListRooms(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.ListRooms, env => _registry.ListRooms(env));
        await SendCaller("ReceiveRoomList", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> CreateTable(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.CreateTable, env => _registry.CreateTable(env, message.Table ?? new OnlineTableCommand()));
        if (result.Envelope.MessageType == OnlineMessageTypes.TableCreated)
        {
            await Clients.Group(RoomGroup(result.Envelope.RoomId)).SendAsync("ReceiveTableCreated", result);
        }
        else
        {
            await SendCaller("ReceiveError", result);
        }
        return result;
    }

    public async Task<OnlineProtocolMessage> JoinTableSeat(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.JoinTableSeat, env => _registry.JoinTableSeat(env, message.Table ?? new OnlineTableCommand()));
        if (result.Envelope.MessageType == OnlineMessageTypes.SeatAssigned)
        {
            _connections.SetMembership(Context.ConnectionId, result.Envelope.RoomId, result.Envelope.TableId);
            await Groups.AddToGroupAsync(Context.ConnectionId, TableGroup(result.Envelope.TableId));
            await Clients.Group(TableGroup(result.Envelope.TableId)).SendAsync("ReceiveSeatAssigned", result);
            await Clients.Group(TableGroup(result.Envelope.TableId)).SendAsync("ReceiveTableState", result);
        }
        else
        {
            await SendCaller("ReceiveError", result);
        }
        return result;
    }

    public async Task<OnlineProtocolMessage> LeaveTableSeat(OnlineProtocolMessage message)
    {
        if (_connections.TryGet(Context.ConnectionId, out var session) && !string.IsNullOrWhiteSpace(session.TableId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, TableGroup(session.TableId));
        }
        var result = Reply(OnlineMessageTypes.TableState, message.Envelope, "seat left for this connection");
        await SendCaller("ReceiveTableState", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> Ready(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.Ready, env => _registry.Ready(env, message.Table ?? new OnlineTableCommand()));
        await SendToTableOrCaller(result, "ReceiveTableState");
        return result;
    }

    public async Task<OnlineProtocolMessage> StartGame(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.StartGame, env => _registry.StartGame(env));
        if (result.Envelope.MessageType == OnlineMessageTypes.GameStarted)
        {
            await Clients.Group(TableGroup(result.Envelope.TableId)).SendAsync("ReceiveGameStarted", result);
            await Clients.Group(TableGroup(result.Envelope.TableId)).SendAsync("ReceiveAuthoritativeSnapshot", result);
        }
        else
        {
            await SendCaller("ReceiveError", result);
        }
        return result;
    }

    public async Task<OnlineProtocolMessage> SubmitAction(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.SubmitAction, env => _registry.SubmitAction(env, message.Action ?? new OnlineActionCommand()));
        if (result.Envelope.MessageType == OnlineMessageTypes.ActionAccepted)
        {
            await Clients.Group(TableGroup(result.Envelope.TableId)).SendAsync("ReceiveActionAccepted", result);
        }
        else if (result.Envelope.MessageType == OnlineMessageTypes.ResyncRequired)
        {
            await SendCaller("ReceiveResyncRequired", result);
        }
        else
        {
            await SendCaller("ReceiveActionRejected", result);
        }
        return result;
    }

    public async Task<OnlineProtocolMessage> RequestSnapshot(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.RequestSnapshot, env => _registry.RequestSnapshot(env));
        await SendCaller(result.Envelope.MessageType == OnlineMessageTypes.AuthoritativeSnapshot ? "ReceiveAuthoritativeSnapshot" : "ReceiveError", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> RequestActionLog(OnlineProtocolMessage message)
    {
        var from = message.ActionLog?.FromServerSeq == 0 ? 1 : message.ActionLog?.FromServerSeq ?? 1;
        var max = message.ActionLog?.Events.Count == 0 ? 64 : message.ActionLog?.Events.Count ?? 64;
        var result = InvokeRegistry(message, OnlineMessageTypes.RequestActionLog, env => _registry.RequestActionLog(env, from, max));
        await SendCaller(result.Envelope.MessageType == OnlineMessageTypes.ActionLogChunk ? "ReceiveActionLogChunk" : "ReceiveError", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> Ping(OnlineProtocolMessage message)
    {
        var result = Reply(OnlineMessageTypes.Pong, message.Envelope);
        await SendCaller("ReceivePong", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> Diagnostics(OnlineProtocolMessage message)
    {
        var diagnostics = _registry.GetDiagnostics();
        diagnostics.ActiveConnectionCount = _connections.ActiveConnectionCount;
        var result = new OnlineProtocolMessage
        {
            Envelope = message.Envelope,
            Diagnostics = diagnostics,
            Text = "P3F local hosted diagnostics"
        };
        result.Envelope.MessageType = OnlineMessageTypes.Diagnostics;
        await SendCaller("ReceiveDiagnostics", result);
        return result;
    }

    private OnlineProtocolMessage InvokeRegistry(OnlineProtocolMessage message, string expectedType, Func<OnlineMessageEnvelope, OnlineProtocolMessage> call)
    {
        try
        {
            if (!Validate(message, expectedType, out var error))
            {
                return error;
            }
            if (!_connections.AllowCommand(Context.ConnectionId, _options.RateLimitPermitLimit, _options.RateLimitWindowSeconds))
            {
                return Error(message.Envelope, OnlineRejectReasons.IllegalAction, "Rate limit exceeded.");
            }
            var envelope = CurrentEnvelope(message.Envelope);
            return call(envelope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hub command failed for {MessageType}", expectedType);
            return Error(message.Envelope, OnlineRejectReasons.InternalError, "Internal server error.");
        }
    }

    private bool Validate(OnlineProtocolMessage message, string expectedType, out OnlineProtocolMessage error)
    {
        if (message.Envelope.MessageType != expectedType)
        {
            message.Envelope.MessageType = expectedType;
        }
        if (!OnlineProtocolJson.ValidateEnvelope(message.Envelope, out var protocolError))
        {
            error = Error(message.Envelope, protocolError.ReasonCode, protocolError.ReasonText);
            return false;
        }
        var size = System.Text.Encoding.UTF8.GetByteCount(System.Text.Json.JsonSerializer.Serialize(message, OnlineProtocolJson.Options));
        if (size > _options.MaxReceiveMessageBytes)
        {
            error = Error(message.Envelope, OnlineRejectReasons.OversizedMessage, "Message exceeds hosted transport size limit.");
            return false;
        }
        error = new OnlineProtocolMessage();
        return true;
    }

    private OnlineMessageEnvelope CurrentEnvelope(OnlineMessageEnvelope envelope)
    {
        if (_connections.TryGet(Context.ConnectionId, out var session))
        {
            return WithSession(envelope, session);
        }
        return envelope;
    }

    private static OnlineMessageEnvelope WithSession(OnlineMessageEnvelope envelope, OnlineConnectionSession session)
    {
        envelope.ClientId = string.IsNullOrWhiteSpace(envelope.ClientId) ? session.ClientId : envelope.ClientId;
        envelope.PlayerId = session.PlayerId;
        envelope.SessionToken = session.SessionToken;
        return envelope;
    }

    private static OnlineProtocolMessage Error(OnlineMessageEnvelope request, string reason, string text)
    {
        return new OnlineProtocolMessage
        {
            Envelope = new OnlineMessageEnvelope
            {
                MessageType = OnlineMessageTypes.Error,
                MessageId = Guid.NewGuid().ToString("N"),
                CorrelationId = request.MessageId,
                ClientId = "server",
                PlayerId = request.PlayerId,
                RoomId = request.RoomId,
                TableId = request.TableId,
                SentAtUtc = DateTime.UtcNow.ToString("O")
            },
            Error = OnlineProtocolJson.Error(reason, text)
        };
    }

    private static OnlineProtocolMessage Reply(string messageType, OnlineMessageEnvelope request, string text = "")
    {
        return new OnlineProtocolMessage
        {
            Envelope = new OnlineMessageEnvelope
            {
                MessageType = messageType,
                MessageId = Guid.NewGuid().ToString("N"),
                CorrelationId = request.MessageId,
                ClientId = "server",
                PlayerId = request.PlayerId,
                RoomId = request.RoomId,
                TableId = request.TableId,
                SentAtUtc = DateTime.UtcNow.ToString("O")
            },
            Text = text
        };
    }

    private async Task SendToTableOrCaller(OnlineProtocolMessage result, string eventName)
    {
        if (result.Error == null && !string.IsNullOrWhiteSpace(result.Envelope.TableId))
        {
            await Clients.Group(TableGroup(result.Envelope.TableId)).SendAsync(eventName, result);
        }
        else
        {
            await SendCaller("ReceiveError", result);
        }
    }

    private Task SendCaller(string eventName, OnlineProtocolMessage message)
    {
        return Clients.Caller.SendAsync(eventName, message);
    }

    private static string RoomGroup(string roomId) => $"room:{roomId}";
    private static string TableGroup(string tableId) => $"table:{tableId}";
}
