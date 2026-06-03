using System.Text.Json;

namespace ChessOnlineProtocol;

public static class OnlineProtocolJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static readonly HashSet<string> KnownMessageTypes = new(StringComparer.Ordinal)
    {
        OnlineMessageTypes.Hello,
        OnlineMessageTypes.CreateRoom,
        OnlineMessageTypes.JoinRoom,
        OnlineMessageTypes.LeaveRoom,
        OnlineMessageTypes.ListRooms,
        OnlineMessageTypes.CreateTable,
        OnlineMessageTypes.JoinTableSeat,
        OnlineMessageTypes.LeaveTableSeat,
        OnlineMessageTypes.Ready,
        OnlineMessageTypes.StartGame,
        OnlineMessageTypes.SubmitAction,
        OnlineMessageTypes.RequestSnapshot,
        OnlineMessageTypes.RequestActionLog,
        OnlineMessageTypes.RequestDiagnostics,
        OnlineMessageTypes.Ping,
        OnlineMessageTypes.ChatMessage,
        OnlineMessageTypes.Welcome,
        OnlineMessageTypes.RoomCreated,
        OnlineMessageTypes.RoomJoined,
        OnlineMessageTypes.RoomLeft,
        OnlineMessageTypes.RoomList,
        OnlineMessageTypes.TableCreated,
        OnlineMessageTypes.TableState,
        OnlineMessageTypes.SeatAssigned,
        OnlineMessageTypes.GameStarted,
        OnlineMessageTypes.ActionAccepted,
        OnlineMessageTypes.ActionRejected,
        OnlineMessageTypes.AuthoritativeSnapshot,
        OnlineMessageTypes.ActionLogChunk,
        OnlineMessageTypes.ResyncRequired,
        OnlineMessageTypes.Pong,
        OnlineMessageTypes.Error,
        OnlineMessageTypes.Diagnostics,
        OnlineMessageTypes.ChatBroadcast
    };

    public static string Serialize(OnlineProtocolMessage message)
    {
        PrepareEnvelope(message.Envelope);
        return JsonSerializer.Serialize(message, Options);
    }

    public static bool TryDeserialize(string json, out OnlineProtocolMessage message, out OnlineError error)
    {
        message = new OnlineProtocolMessage();
        error = new OnlineError();

        if (string.IsNullOrWhiteSpace(json))
        {
            error = Error(OnlineRejectReasons.InvalidJson, "Message is empty.");
            return false;
        }

        if (System.Text.Encoding.UTF8.GetByteCount(json) > OnlineProtocolVersion.MaxMessageBytes)
        {
            error = Error(OnlineRejectReasons.OversizedMessage, "Message exceeds the P3E v0.1 size limit.");
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<OnlineProtocolMessage>(json, Options);
            if (parsed == null)
            {
                error = Error(OnlineRejectReasons.InvalidJson, "Message did not deserialize.");
                return false;
            }
            if (!ValidateEnvelope(parsed.Envelope, out error))
            {
                return false;
            }
            message = parsed;
            return true;
        }
        catch (JsonException)
        {
            error = Error(OnlineRejectReasons.InvalidJson, "Malformed JSON message.");
            return false;
        }
    }

    public static bool ValidateEnvelope(OnlineMessageEnvelope envelope, out OnlineError error)
    {
        error = new OnlineError();

        if (!string.Equals(envelope.ProtocolId, OnlineProtocolVersion.ProtocolId, StringComparison.Ordinal))
        {
            error = Error(OnlineRejectReasons.WrongProtocol, "Unsupported protocol id.");
            return false;
        }

        if (!string.Equals(envelope.ProtocolVersion, OnlineProtocolVersion.ProtocolVersion, StringComparison.Ordinal))
        {
            error = Error(OnlineRejectReasons.UnsupportedVersion, "Unsupported protocol version.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(envelope.MessageType) || !KnownMessageTypes.Contains(envelope.MessageType))
        {
            error = Error(OnlineRejectReasons.UnknownMessageType, "Unknown message type.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(envelope.ClientId))
        {
            error = Error(OnlineRejectReasons.MissingRequiredField, "clientId is required.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(envelope.MessageId))
        {
            error = Error(OnlineRejectReasons.MissingRequiredField, "messageId is required.");
            return false;
        }

        return true;
    }

    public static OnlineProtocolMessage Wrap(string messageType, string clientId, string playerId = "")
    {
        return new OnlineProtocolMessage
        {
            Envelope = new OnlineMessageEnvelope
            {
                MessageType = messageType,
                MessageId = Guid.NewGuid().ToString("N"),
                ClientId = clientId,
                PlayerId = playerId,
                SentAtUtc = DateTime.UtcNow.ToString("O")
            }
        };
    }

    public static OnlineError Error(string reasonCode, string reasonText, string stateHash = "", long serverSeq = 0)
    {
        return new OnlineError
        {
            ReasonCode = reasonCode,
            ReasonText = reasonText,
            StateHash = stateHash,
            ServerSeq = serverSeq
        };
    }

    private static void PrepareEnvelope(OnlineMessageEnvelope envelope)
    {
        envelope.ProtocolId = string.IsNullOrWhiteSpace(envelope.ProtocolId) ? OnlineProtocolVersion.ProtocolId : envelope.ProtocolId;
        envelope.ProtocolVersion = string.IsNullOrWhiteSpace(envelope.ProtocolVersion) ? OnlineProtocolVersion.ProtocolVersion : envelope.ProtocolVersion;
        envelope.MessageId = string.IsNullOrWhiteSpace(envelope.MessageId) ? Guid.NewGuid().ToString("N") : envelope.MessageId;
        envelope.SentAtUtc = string.IsNullOrWhiteSpace(envelope.SentAtUtc) ? DateTime.UtcNow.ToString("O") : envelope.SentAtUtc;
    }
}
