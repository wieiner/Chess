using System.Security.Claims;
using System.Text.Json;
using ChessOnlinePersistence.Entities;
using ChessOnlinePersistence.Repositories;
using ChessOnlineProtocol;
using ChessOnlineServer.Matchmaking;
using Microsoft.AspNetCore.SignalR;

namespace ChessOnlineServer;

public sealed class Chess3DRelayHub : Hub
{
    private readonly OnlineRoomRegistry _registry;
    private readonly OnlineHubConnectionRegistry _connections;
    private readonly OnlineSpectatorRegistry _spectators;
    private readonly OnlineMatchmakingService _matchmaking;
    private readonly IOnlineRoomPersistenceStore _roomStore;
    private readonly IOnlineSessionStore _sessionStore;
    private readonly HostedOnlineOptions _options;
    private readonly ILogger<Chess3DRelayHub> _logger;

    public Chess3DRelayHub(
        OnlineRoomRegistry registry,
        OnlineHubConnectionRegistry connections,
        OnlineSpectatorRegistry spectators,
        OnlineMatchmakingService matchmaking,
        IOnlineRoomPersistenceStore roomStore,
        IOnlineSessionStore sessionStore,
        HostedOnlineOptions options,
        ILogger<Chess3DRelayHub> logger)
    {
        _registry = registry;
        _connections = connections;
        _spectators = spectators;
        _matchmaking = matchmaking;
        _roomStore = roomStore;
        _sessionStore = sessionStore;
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
        var authError = ValidateAuthenticatedEnvelope(message.Envelope);
        if (authError != null)
        {
            await SendCaller("ReceiveError", authError);
            return authError;
        }
        if (!string.IsNullOrWhiteSpace(message.Envelope.SessionToken) &&
            !_connections.CanReconnect(message.Envelope.PlayerId, message.Envelope.SessionToken))
        {
            var invalid = Error(message.Envelope, OnlineRejectReasons.IllegalAction, "Invalid session token.");
            await SendCaller("ReceiveError", invalid);
            return invalid;
        }

        var session = AuthenticatedPlayerId() is { Length: > 0 } playerId && AuthenticatedSessionId() is { Length: > 0 } authSessionId
            ? _connections.HelloAuthenticated(Context.ConnectionId, message.Envelope, playerId, authSessionId)
            : _connections.Hello(Context.ConnectionId, message.Envelope);
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
        if (result.Envelope.MessageType == OnlineMessageTypes.RoomCreated)
        {
            await PersistRoom(result);
        }
        await SendCaller(result.Envelope.MessageType == OnlineMessageTypes.RoomCreated ? "ReceiveRoomCreated" : "ReceiveError", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> JoinRoom(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.JoinRoom, env => _registry.JoinRoom(env));
        if (result.Envelope.MessageType == OnlineMessageTypes.RoomJoined)
        {
            _connections.SetMembership(Context.ConnectionId, result.Envelope.RoomId);
            await PersistSessionMembership(result.Envelope.RoomId, "", 0);
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
            await PersistTable(result);
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
            await PersistSeat(result);
            await PersistSessionMembership(result.Envelope.RoomId, result.Envelope.TableId, result.Table?.SeatIndex ?? 0);
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
        if (result.Envelope.MessageType == OnlineMessageTypes.TableState)
        {
            await PersistSeat(result);
        }
        await SendToTableOrCaller(result, "ReceiveTableState");
        return result;
    }

    public async Task<OnlineProtocolMessage> StartGame(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.StartGame, env => _registry.StartGame(env));
        if (result.Envelope.MessageType == OnlineMessageTypes.GameStarted)
        {
            await PersistTable(result);
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
            await PersistAcceptedAction(result);
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

    public async Task<OnlineProtocolMessage> RequestResumeMatch(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.RequestResumeMatch, env => _registry.RequestResumeMatch(env, message.ResumeRequest ?? new OnlineResumeRequest()));
        if (result.ResumeResult?.Success == true)
        {
            _connections.SetMembership(Context.ConnectionId, result.ResumeResult.RoomId, result.ResumeResult.TableId);
            await PersistSessionMembership(result.ResumeResult.RoomId, result.ResumeResult.TableId, result.ResumeResult.SeatIndex);
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(result.ResumeResult.RoomId));
            await Groups.AddToGroupAsync(Context.ConnectionId, TableGroup(result.ResumeResult.TableId));
        }
        await SendCaller("ReceiveResumeMatchResult", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> JoinSpectator(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.JoinSpectator, env => _registry.JoinSpectator(env, message.SpectatorRequest ?? new OnlineJoinSpectatorRequest()));
        if (result.SpectatorResult?.Success == true)
        {
            var registration = _spectators.Register(
                result.SpectatorResult.RoomId,
                result.SpectatorResult.TableId,
                result.SpectatorResult.State.ViewerPlayerId,
                Context.ConnectionId);
            if (registration.ReplacedConnection && !string.IsNullOrWhiteSpace(registration.ReplacedConnectionId))
            {
                await Groups.RemoveFromGroupAsync(registration.ReplacedConnectionId, RoomGroup(result.SpectatorResult.RoomId));
                await Groups.RemoveFromGroupAsync(registration.ReplacedConnectionId, TableGroup(result.SpectatorResult.TableId));
            }
            _connections.SetMembership(Context.ConnectionId, result.SpectatorResult.RoomId, result.SpectatorResult.TableId);
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(result.SpectatorResult.RoomId));
            await Groups.AddToGroupAsync(Context.ConnectionId, TableGroup(result.SpectatorResult.TableId));
        }
        await SendCaller("ReceiveJoinSpectatorResult", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> RequestLegalPreview(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(message, OnlineMessageTypes.RequestLegalPreview, env => _registry.RequestLegalPreview(env, message.LegalPreviewRequest ?? new OnlineLegalPreviewRequest()));
        await SendCaller(result.Envelope.MessageType == OnlineMessageTypes.LegalPreviewResult ? "ReceiveLegalPreviewResult" : "ReceiveError", result);
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

    public async Task<OnlineProtocolMessage> RequestLobbySnapshot(OnlineProtocolMessage message)
    {
        var result = InvokeRegistry(
            message,
            OnlineMessageTypes.RequestLobbySnapshot,
            env => _registry.RequestLobbySnapshot(env, message.LobbyRequest ?? new OnlineLobbySnapshotRequest()));
        _spectators.ApplyCounts(result.LobbySnapshot);
        await SendCaller(result.Envelope.MessageType == OnlineMessageTypes.LobbySnapshot ? "ReceiveLobbySnapshot" : "ReceiveError", result);
        return result;
    }

    public async Task<OnlineProtocolMessage> JoinMatchmaking(OnlineProtocolMessage message)
    {
        if (!Validate(message, OnlineMessageTypes.JoinMatchmaking, out var error))
        {
            await SendCaller("ReceiveError", error);
            return error;
        }
        var authError = ValidateAuthenticatedEnvelope(message.Envelope);
        if (authError != null)
        {
            await SendCaller("ReceiveError", authError);
            return authError;
        }
        var envelope = CurrentEnvelope(message.Envelope);
        var result = _matchmaking.Join(envelope.PlayerId, envelope.ClientId, message.Matchmaking ?? new OnlineMatchmakingCommand(), _registry, _options.ProfileRoot);
        var response = MatchmakingReply(result.MessageType, envelope, result.Status, result.ErrorCode, result.ErrorText);
        if (result.MessageType == OnlineMessageTypes.MatchFound)
        {
            foreach (var ticket in result.MatchedTickets)
            {
                if (_connections.TryGetByPlayerId(ticket.PlayerId, out var session))
                {
                    foreach (var connectionId in session.ConnectionIds)
                    {
                        _connections.SetMembership(connectionId, result.RoomId, result.TableId);
                        await Groups.AddToGroupAsync(connectionId, RoomGroup(result.RoomId));
                        await Groups.AddToGroupAsync(connectionId, TableGroup(result.TableId));
                    }
                }
            }
            await PersistMatchFound(result);
            await Clients.Group(TableGroup(result.TableId)).SendAsync("ReceiveMatchFound", response);
        }
        else
        {
            await SendCaller(result.MessageType == OnlineMessageTypes.MatchmakingError ? "ReceiveMatchmakingError" : "ReceiveMatchmakingStatus", response);
        }
        return response;
    }

    public async Task<OnlineProtocolMessage> CancelMatchmaking(OnlineProtocolMessage message)
    {
        if (!Validate(message, OnlineMessageTypes.CancelMatchmaking, out var error))
        {
            await SendCaller("ReceiveError", error);
            return error;
        }
        var authError = ValidateAuthenticatedEnvelope(message.Envelope);
        if (authError != null)
        {
            await SendCaller("ReceiveError", authError);
            return authError;
        }
        var envelope = CurrentEnvelope(message.Envelope);
        var result = _matchmaking.Cancel(envelope.PlayerId);
        var response = MatchmakingReply(result.MessageType, envelope, result.Status, result.ErrorCode, result.ErrorText);
        await SendCaller(result.MessageType == OnlineMessageTypes.MatchmakingError ? "ReceiveMatchmakingError" : "ReceiveMatchmakingCancelled", response);
        return response;
    }

    public async Task<OnlineProtocolMessage> GetMatchmakingStatus(OnlineProtocolMessage message)
    {
        if (!Validate(message, OnlineMessageTypes.GetMatchmakingStatus, out var error))
        {
            await SendCaller("ReceiveError", error);
            return error;
        }
        var authError = ValidateAuthenticatedEnvelope(message.Envelope);
        if (authError != null)
        {
            await SendCaller("ReceiveError", authError);
            return authError;
        }
        var envelope = CurrentEnvelope(message.Envelope);
        var response = MatchmakingReply(OnlineMessageTypes.MatchmakingStatus, envelope, _matchmaking.Status(envelope.PlayerId));
        await SendCaller("ReceiveMatchmakingStatus", response);
        return response;
    }

    public async Task<OnlineProtocolMessage> ListMatchmakingQueues(OnlineProtocolMessage message)
    {
        if (!Validate(message, OnlineMessageTypes.ListMatchmakingQueues, out var error))
        {
            await SendCaller("ReceiveError", error);
            return error;
        }
        var response = MatchmakingReply(OnlineMessageTypes.MatchmakingStatus, CurrentEnvelope(message.Envelope), _matchmaking.QueueSummary());
        await SendCaller("ReceiveMatchmakingStatus", response);
        return response;
    }

    private OnlineProtocolMessage InvokeRegistry(OnlineProtocolMessage message, string expectedType, Func<OnlineMessageEnvelope, OnlineProtocolMessage> call)
    {
        try
        {
            if (!Validate(message, expectedType, out var error))
            {
                return error;
            }
            var authError = ValidateAuthenticatedEnvelope(message.Envelope);
            if (authError != null)
            {
                return authError;
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
        var playerId = AuthenticatedPlayerId();
        var sessionId = AuthenticatedSessionId();
        if (!string.IsNullOrWhiteSpace(playerId) && !string.IsNullOrWhiteSpace(sessionId))
        {
            envelope.PlayerId = playerId;
            envelope.SessionToken = sessionId;
        }
        if (_connections.TryGet(Context.ConnectionId, out var session))
        {
            return WithSession(envelope, session);
        }
        return envelope;
    }

    private OnlineProtocolMessage? ValidateAuthenticatedEnvelope(OnlineMessageEnvelope envelope)
    {
        if (!_options.Auth.EnableAuthentication)
        {
            return null;
        }

        var playerId = AuthenticatedPlayerId();
        if (!string.IsNullOrWhiteSpace(playerId))
        {
            if (!string.IsNullOrWhiteSpace(envelope.PlayerId) &&
                !string.Equals(envelope.PlayerId, playerId, StringComparison.OrdinalIgnoreCase))
            {
                return Error(envelope, OnlineRejectReasons.IllegalAction, "Authenticated player does not match message envelope.");
            }
            return null;
        }

        return _options.Auth.AllowDevAnonymousSessions
            ? null
            : Error(envelope, OnlineRejectReasons.IllegalAction, "Authentication is required.");
    }

    private string AuthenticatedPlayerId() => Context.User?.FindFirst("playerId")?.Value
        ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? "";

    private string AuthenticatedSessionId() => Context.User?.FindFirst("sessionId")?.Value ?? "";

    private async Task PersistSessionMembership(string roomId, string tableId, int seatIndex)
    {
        var sessionId = AuthenticatedSessionId();
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _sessionStore.UpdateLastSeenAsync(sessionId, roomId, tableId, seatIndex);
        }
    }

    private async Task PersistTable(OnlineProtocolMessage result)
    {
        var snapshot = result.Snapshot;
        var tableId = result.Envelope.TableId;
        if (string.IsNullOrWhiteSpace(tableId))
        {
            tableId = result.Table?.TableId ?? "";
        }
        if (string.IsNullOrWhiteSpace(tableId))
        {
            return;
        }
        var tableKey = PersistenceTableKey(result.Envelope.RoomId, tableId);
        if (result.Envelope.MessageType == OnlineMessageTypes.GameStarted)
        {
            await _roomStore.ClearActionLogAsync(tableKey);
        }
        await _roomStore.UpsertTableAsync(new PersistentTableEntity
        {
            RoomId = result.Envelope.RoomId,
            TableId = tableKey,
            RulesetId = result.Table?.RulesetId ?? snapshot?.RulesetId ?? "",
            ProfileKind = result.Table?.RulesetId ?? snapshot?.RulesetId ?? "",
            State = result.Envelope.MessageType,
            ServerSeq = snapshot?.ServerSeq ?? result.Envelope.ServerSeq,
            StateHash = snapshot?.StateHash ?? "",
            SaveGameJson = snapshot?.SaveGameJson ?? "",
            CreatedAtUtc = DateTime.UtcNow,
            StartedAtUtc = result.Envelope.MessageType == OnlineMessageTypes.GameStarted ? DateTime.UtcNow : null,
            LastUpdatedAtUtc = DateTime.UtcNow
        });
    }

    private async Task PersistRoom(OnlineProtocolMessage result)
    {
        var roomId = result.Room?.RoomId ?? result.Envelope.RoomId;
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }
        await _roomStore.UpsertRoomAsync(new PersistentRoomEntity
        {
            RoomId = roomId,
            DisplayName = result.Room?.DisplayName ?? roomId,
            CreatedAtUtc = DateTime.UtcNow,
            OwnerPlayerId = result.Envelope.PlayerId,
            State = result.Envelope.MessageType,
            LastServerSeq = result.Envelope.ServerSeq,
            LastUpdatedAtUtc = DateTime.UtcNow
        });
    }

    private async Task PersistSeat(OnlineProtocolMessage result)
    {
        if (result.Table == null || string.IsNullOrWhiteSpace(result.Envelope.TableId))
        {
            return;
        }
        await _roomStore.UpsertSeatAsync(new PersistentSeatEntity
        {
            TableId = PersistenceTableKey(result.Envelope.RoomId, result.Envelope.TableId),
            SeatIndex = result.Table.SeatIndex,
            SideId = result.Table.SeatIndex,
            MacroPlayer = 0,
            PlayerId = result.Envelope.PlayerId,
            IsReady = result.Table.Ready,
            IsConnected = true,
            LastSeenAtUtc = DateTime.UtcNow
        });
    }

    private async Task PersistMatchFound(OnlineMatchmakingResult result)
    {
        if (string.IsNullOrWhiteSpace(result.RoomId) || string.IsNullOrWhiteSpace(result.TableId))
        {
            return;
        }

        var rulesetId = result.MatchedTickets.FirstOrDefault()?.RequestedRulesetId ?? "";
        var tableKey = PersistenceTableKey(result.RoomId, result.TableId);
        await _roomStore.ClearActionLogAsync(tableKey);
        await _roomStore.UpsertRoomAsync(new PersistentRoomEntity
        {
            RoomId = result.RoomId,
            DisplayName = $"Match {result.RoomId}",
            CreatedAtUtc = DateTime.UtcNow,
            OwnerPlayerId = result.MatchedTickets.FirstOrDefault()?.PlayerId ?? "",
            State = OnlineMessageTypes.MatchFound,
            LastServerSeq = 0,
            LastUpdatedAtUtc = DateTime.UtcNow
        });
        await _roomStore.UpsertTableAsync(new PersistentTableEntity
        {
            RoomId = result.RoomId,
            TableId = tableKey,
            RulesetId = rulesetId,
            ProfileKind = rulesetId,
            State = OnlineMessageTypes.MatchFound,
            ServerSeq = 0,
            StateHash = "",
            SaveGameJson = "",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        });

        foreach (var ticket in result.MatchedTickets)
        {
            await _roomStore.UpsertSeatAsync(new PersistentSeatEntity
            {
                TableId = tableKey,
                SeatIndex = ticket.SeatIndex,
                SideId = rulesetId.Contains("hodge-projection-duel", StringComparison.OrdinalIgnoreCase) ? 0 : ticket.SeatIndex,
                MacroPlayer = rulesetId.Contains("hodge-projection-duel", StringComparison.OrdinalIgnoreCase) ? ticket.SeatIndex : 0,
                PlayerId = ticket.PlayerId,
                IsReady = false,
                IsConnected = true,
                LastSeenAtUtc = DateTime.UtcNow
            });

            if (_connections.TryGetByPlayerId(ticket.PlayerId, out var session) && !string.IsNullOrWhiteSpace(session.SessionToken))
            {
                await _sessionStore.UpdateLastSeenAsync(session.SessionToken, result.RoomId, result.TableId, ticket.SeatIndex);
            }
        }
    }

    private async Task PersistAcceptedAction(OnlineProtocolMessage result)
    {
        if (result.ActionLog?.Events.Count > 0)
        {
            foreach (var actionEvent in result.ActionLog.Events)
            {
                await _roomStore.AppendActionAsync(new PersistentActionLogEntity
                {
                    TableId = PersistenceTableKey(result.Envelope.RoomId, result.Envelope.TableId),
                    ServerSeq = actionEvent.ServerSeq,
                    ActionIndex = actionEvent.ActionIndex,
                    ActorPlayerId = actionEvent.PlayerId,
                    ActionKind = actionEvent.ActionKind,
                    ActionJson = JsonSerializer.Serialize(actionEvent.Command, OnlineProtocolJson.Options),
                    Notation = actionEvent.Notation,
                    StateHashAfter = actionEvent.StateHashAfter,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }
    }

    private static string PersistenceTableKey(string roomId, string tableId) => $"{roomId.Trim()}/{tableId.Trim()}";

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

    private static OnlineProtocolMessage MatchmakingReply(string messageType, OnlineMessageEnvelope request, OnlineMatchmakingStatus status, string errorCode = "", string errorText = "")
    {
        var reply = new OnlineProtocolMessage
        {
            Envelope = new OnlineMessageEnvelope
            {
                MessageType = messageType,
                MessageId = Guid.NewGuid().ToString("N"),
                CorrelationId = request.MessageId,
                ClientId = "server",
                PlayerId = request.PlayerId,
                RoomId = status.RoomId,
                TableId = status.TableId,
                SentAtUtc = DateTime.UtcNow.ToString("O")
            },
            MatchmakingStatus = status
        };
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            reply.Error = OnlineProtocolJson.Error(errorCode, errorText);
        }
        return reply;
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
