using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ChessApp;

internal sealed class Chess3DInternetRelayClient : IDisposable
{
    private readonly string _nodeId = Guid.NewGuid().ToString("N");
    private readonly ConcurrentDictionary<string, byte> _seen = new(StringComparer.OrdinalIgnoreCase);
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private string _roomId = "";
    private int _seat;
    private int _groupSlot;
    private string _role = "player";
    private long _sequence;

    public event Action<Chess3DNetworkMessage>? MessageReceived;
    public event Action<Chess3DRelayEnvelope>? EnvelopeReceived;
    public event Action<Chess3DRelayRoomInfo>? RoomInfoReceived;
    public event Action<string>? StatusChanged;

    public bool IsConnected => _socket?.State == WebSocketState.Open;
    public string NodeId => _nodeId;
    public string RoomId => _roomId;
    public int Seat => _seat;
    public int GroupSlot => _groupSlot;

    public static async Task<Chess3DRelayRoomInfo?> CreateRoomAsync(
        Uri platformBaseUri,
        Chess3DRelayRoomCreateRequest request,
        string token,
        CancellationToken cancellationToken = default)
    {
        using var http = CreateHttpClient(platformBaseUri, token);
        using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync("/api/chess3d/rooms", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<Chess3DRelayRoomInfo>(stream, cancellationToken: cancellationToken);
    }

    public static async Task<Chess3DRelayRoomInfo?> GetRoomAsync(
        Uri platformBaseUri,
        string roomId,
        string token,
        CancellationToken cancellationToken = default)
    {
        using var http = CreateHttpClient(platformBaseUri, token);
        using var response = await http.GetAsync($"/api/chess3d/rooms/{Uri.EscapeDataString(roomId)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<Chess3DRelayRoomInfo>(stream, cancellationToken: cancellationToken);
    }

    public Task ConnectAsync(Uri relayUri, string roomId, int seat, int groupSlot, string token, CancellationToken cancellationToken = default)
    {
        return ConnectAsync(new Chess3DRelayConnectOptions
        {
            WebSocketUri = relayUri,
            RoomId = roomId,
            Seat = seat,
            GroupSlot = groupSlot,
            AccessToken = token,
            Role = groupSlot > 0 ? "group" : "player"
        }, cancellationToken);
    }

    public async Task ConnectAsync(Chess3DRelayConnectOptions options, CancellationToken cancellationToken = default)
    {
        Disconnect();
        _roomId = string.IsNullOrWhiteSpace(options.RoomId) ? "cube-main" : options.RoomId.Trim();
        _seat = Math.Clamp(options.Seat, 0, 6);
        _groupSlot = Math.Clamp(options.GroupSlot, 0, 6);
        _role = string.IsNullOrWhiteSpace(options.Role) ? (_groupSlot > 0 ? "group" : "player") : options.Role.Trim();

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _socket = new ClientWebSocket();
        if (!string.IsNullOrWhiteSpace(options.AccessToken))
        {
            _socket.Options.SetRequestHeader("Authorization", $"Bearer {options.AccessToken}");
        }
        if (!string.IsNullOrWhiteSpace(options.ProtocolVersion))
        {
            _socket.Options.SetRequestHeader("X-Chess3D-Protocol", options.ProtocolVersion);
        }

        var uri = BuildWebSocketUri(options.WebSocketUri, _roomId, _seat, _groupSlot, _role, _nodeId);
        await _socket.ConnectAsync(uri, _cts.Token);
        StatusChanged?.Invoke($"3D Internet: connected to {uri.Host}, room {_roomId}, role {_role}");
        _ = ReadLoopAsync(_cts.Token);
        await SendEnvelopeAsync("hello3d", new Chess3DNetworkMessage
        {
            Type = "hello3d",
            GroupId = _roomId,
            Seat = _seat,
            GroupSlot = _groupSlot,
            Kind = _groupSlot > 0 ? (int)Chess3DNetworkPeerKind.Group : (int)Chess3DNetworkPeerKind.Player
        }, cancellationToken);
    }

    public Task SendMoveAsync(int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int promotion, CancellationToken cancellationToken = default)
    {
        return SendEnvelopeAsync("move3d", new Chess3DNetworkMessage
        {
            Type = "move3d",
            FromX = fromX,
            FromY = fromY,
            FromZ = fromZ,
            ToX = toX,
            ToY = toY,
            ToZ = toZ,
            Promotion = promotion
        }, cancellationToken);
    }

    public Task SendRotateAsync(int axis, int layer, int quarterTurns, CancellationToken cancellationToken = default)
    {
        return SendEnvelopeAsync("rotate3d", new Chess3DNetworkMessage
        {
            Type = "rotate3d",
            Axis = axis,
            Layer = layer,
            QuarterTurns = quarterTurns
        }, cancellationToken);
    }

    public Task SendBoardSyncAsync(int[] board512, int sideToMove, CancellationToken cancellationToken = default)
    {
        return SendEnvelopeAsync("sync3d", new Chess3DNetworkMessage
        {
            Type = "sync3d",
            Board = board512,
            SideToMove = sideToMove
        }, cancellationToken);
    }

    public Task SendReadyAsync(bool ready, CancellationToken cancellationToken = default)
    {
        return SendEnvelopeAsync(ready ? "ready3d" : "not-ready3d", new Chess3DNetworkMessage
        {
            Type = ready ? "ready3d" : "not-ready3d"
        }, cancellationToken);
    }

    public Task SendChatAsync(string text, CancellationToken cancellationToken = default)
    {
        return SendEnvelopeAsync("chat3d", new Chess3DNetworkMessage
        {
            Type = "chat3d"
        }, cancellationToken, new Dictionary<string, string> { ["text"] = text });
    }

    public Task SendAsync(Chess3DNetworkMessage message, CancellationToken cancellationToken = default)
    {
        return SendEnvelopeAsync(string.IsNullOrWhiteSpace(message.Type) ? "message3d" : message.Type, message, cancellationToken);
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _socket?.Dispose();
        _socket = null;
    }

    public void Dispose()
    {
        Disconnect();
    }

    private async Task SendEnvelopeAsync(
        string type,
        Chess3DNetworkMessage payload,
        CancellationToken cancellationToken,
        Dictionary<string, string>? metadata = null)
    {
        if (_socket?.State != WebSocketState.Open)
        {
            return;
        }

        PreparePayload(payload, type);
        var envelope = new Chess3DRelayEnvelope
        {
            Protocol = "chess3d.relay.v1",
            Type = type,
            RoomId = _roomId,
            NodeId = _nodeId,
            Seat = _seat,
            GroupSlot = _groupSlot,
            Role = _role,
            Sequence = payload.Sequence,
            MessageId = payload.MessageId,
            Payload = payload,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        var json = JsonSerializer.Serialize(envelope);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        var builder = new StringBuilder();
        try
        {
            while (_socket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage)
                {
                    continue;
                }

                var json = builder.ToString();
                builder.Clear();
                HandleEnvelopeJson(json);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"3D Internet: disconnected, {ex.Message}");
        }
    }

    private void HandleEnvelopeJson(string json)
    {
        var envelope = JsonSerializer.Deserialize<Chess3DRelayEnvelope>(json);
        if (envelope == null || string.Equals(envelope.NodeId, _nodeId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        if (!Remember(envelope.MessageId))
        {
            return;
        }

        EnvelopeReceived?.Invoke(envelope);
        if (envelope.Room != null)
        {
            RoomInfoReceived?.Invoke(envelope.Room);
        }
        if (envelope.Payload != null)
        {
            MessageReceived?.Invoke(envelope.Payload);
        }
    }

    private void PreparePayload(Chess3DNetworkMessage message, string type)
    {
        message.Type = string.IsNullOrWhiteSpace(message.Type) ? type : message.Type;
        message.SourceNodeId = string.IsNullOrWhiteSpace(message.SourceNodeId) ? _nodeId : message.SourceNodeId;
        message.GroupId = string.IsNullOrWhiteSpace(message.GroupId) ? _roomId : message.GroupId;
        message.Seat = message.Seat == 0 ? _seat : message.Seat;
        message.GroupSlot = message.GroupSlot == 0 ? _groupSlot : message.GroupSlot;
        message.MessageId = string.IsNullOrWhiteSpace(message.MessageId)
            ? $"{_nodeId}:internet:{Interlocked.Increment(ref _sequence)}"
            : message.MessageId;
        message.Sequence = Interlocked.Increment(ref _sequence);
    }

    private bool Remember(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return true;
        }
        if (_seen.Count > 8192)
        {
            _seen.Clear();
        }
        return _seen.TryAdd(messageId, 0);
    }

    private static Uri BuildWebSocketUri(Uri baseUri, string roomId, int seat, int groupSlot, string role, string nodeId)
    {
        var separator = string.IsNullOrEmpty(baseUri.Query) ? "?" : "&";
        var url = $"{baseUri}{separator}room={Uri.EscapeDataString(roomId)}&seat={seat}&groupSlot={groupSlot}&role={Uri.EscapeDataString(role)}&node={Uri.EscapeDataString(nodeId)}";
        return new Uri(url);
    }

    private static HttpClient CreateHttpClient(Uri platformBaseUri, string token)
    {
        var http = new HttpClient { BaseAddress = platformBaseUri };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ChessAdvisor3D/1.0");
        if (!string.IsNullOrWhiteSpace(token))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return http;
    }
}

internal sealed class Chess3DRelayConnectOptions
{
    public Uri WebSocketUri { get; set; } = new("wss://example.invalid/ws/chess3d");
    public string AccessToken { get; set; } = "";
    public string RoomId { get; set; } = "cube-main";
    public string Role { get; set; } = "player";
    public string ProtocolVersion { get; set; } = "chess3d.relay.v1";
    public int Seat { get; set; }
    public int GroupSlot { get; set; }
}

internal sealed class Chess3DRelayEnvelope
{
    public string Protocol { get; set; } = "chess3d.relay.v1";
    public string Type { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string NodeId { get; set; } = "";
    public string MessageId { get; set; } = "";
    public string Role { get; set; } = "";
    public long Sequence { get; set; }
    public int Seat { get; set; }
    public int GroupSlot { get; set; }
    public Chess3DNetworkMessage? Payload { get; set; }
    public Chess3DRelayRoomInfo? Room { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

internal sealed class Chess3DRelayRoomCreateRequest
{
    public string RoomId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int MaxPlayers { get; set; } = 6;
    public int MaxGroups { get; set; } = 6;
    public string RulesetId { get; set; } = "cube8x8x8-draft";
    public bool AllowSpectators { get; set; } = true;
}

internal sealed class Chess3DRelayRoomInfo
{
    public string RoomId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string RulesetId { get; set; } = "";
    public int MaxPlayers { get; set; } = 6;
    public int MaxGroups { get; set; } = 6;
    public int ConnectedPlayers { get; set; }
    public int ConnectedGroups { get; set; }
    public string State { get; set; } = "open";
}
