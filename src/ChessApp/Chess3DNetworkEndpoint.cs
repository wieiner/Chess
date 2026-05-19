using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ChessApp;

internal enum Chess3DNetworkPeerKind
{
    Player = 0,
    Group = 1
}

internal sealed class Chess3DNetworkEndpoint : IDisposable
{
    private const int MaxSlots = 6;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly HashSet<string> _seenMessages = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Chess3DNetworkPeer> _peers = new();
    private readonly string _nodeId = Guid.NewGuid().ToString("N");
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private long _sequence;
    private string _groupId = "cube-main";

    public event Action<Chess3DNetworkMessage>? MessageReceived;
    public event Action<string>? StatusChanged;
    public event Action? PeerConnected;
    public event Action? TopologyChanged;

    public int LocalSeat { get; private set; }
    public bool IsRunning => _cts != null;
    public bool IsHost => _listener != null;

    public string TopologyText
    {
        get
        {
            lock (_gate)
            {
                var players = string.Join(",", _peers.Where(p => p.Kind == Chess3DNetworkPeerKind.Player && p.Seat > 0).Select(p => p.Seat).OrderBy(s => s));
                var groups = string.Join(",", _peers.Where(p => p.Kind == Chess3DNetworkPeerKind.Group && p.GroupSlot > 0).Select(p => p.GroupSlot).OrderBy(s => s));
                return $"3DNet {(_listener != null ? "host" : "client")} local {LocalSeat:0}, players [{players}], groups [{groups}], peers {_peers.Count}";
            }
        }
    }

    public async Task StartHostAsync(int port, string groupId, int localSeat = 0)
    {
        Stop();
        _groupId = string.IsNullOrWhiteSpace(groupId) ? "cube-main" : groupId.Trim();
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        LocalSeat = localSeat >= 1 && localSeat <= MaxSlots ? localSeat : 0;
        StatusChanged?.Invoke($"3DNet: host 0.0.0.0:{port}, group {_groupId}, local seat {LocalSeat:0}");
        TopologyChanged?.Invoke();
        _ = AcceptLoopAsync(_cts.Token);
        await Task.CompletedTask;
    }

    public async Task ConnectAsync(string host, int port, Chess3DNetworkPeerKind kind, int requestedSlot, string groupId)
    {
        Stop();
        _groupId = string.IsNullOrWhiteSpace(groupId) ? "cube-main" : groupId.Trim();
        _cts = new CancellationTokenSource();
        var client = new TcpClient();
        await client.ConnectAsync(host, port, _cts.Token);
        var peer = AttachPeer(client, kind, 0, 0);
        StatusChanged?.Invoke($"3DNet: connected to {host}:{port}");
        await SendToPeerAsync(peer, new Chess3DNetworkMessage
        {
            Type = "hello3d",
            Kind = (int)kind,
            Seat = kind == Chess3DNetworkPeerKind.Player ? requestedSlot : 0,
            GroupSlot = kind == Chess3DNetworkPeerKind.Group ? requestedSlot : 0,
            GroupId = _groupId,
            SourceNodeId = _nodeId,
            MessageId = NewMessageId()
        });
        TopologyChanged?.Invoke();
    }

    public async Task SendAsync(Chess3DNetworkMessage message)
    {
        PrepareOutgoing(message);
        Remember(message.MessageId);
        Chess3DNetworkPeer[] peers;
        lock (_gate)
        {
            peers = _peers.ToArray();
        }

        foreach (var peer in peers)
        {
            await SendToPeerAsync(peer, message);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _listener?.Stop();
        _listener = null;
        lock (_gate)
        {
            foreach (var peer in _peers)
            {
                peer.Dispose();
            }
            _peers.Clear();
            _seenMessages.Clear();
        }
        LocalSeat = 0;
        StatusChanged?.Invoke("3DNet: off");
        TopologyChanged?.Invoke();
    }

    public void Dispose()
    {
        Stop();
        _sendLock.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                var peer = AttachPeer(client, Chess3DNetworkPeerKind.Player, 0, 0);
                StatusChanged?.Invoke($"3DNet: peer {client.Client.RemoteEndPoint}");
                await SendToPeerAsync(peer, new Chess3DNetworkMessage
                {
                    Type = "hello3d",
                    Kind = (int)Chess3DNetworkPeerKind.Group,
                    GroupId = _groupId,
                    SourceNodeId = _nodeId,
                    MessageId = NewMessageId()
                });
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"3DNet: accept failed, {ex.Message}");
            }
        }
    }

    private Chess3DNetworkPeer AttachPeer(TcpClient client, Chess3DNetworkPeerKind kind, int seat, int groupSlot)
    {
        var stream = client.GetStream();
        var peer = new Chess3DNetworkPeer(client, stream)
        {
            Kind = kind,
            Seat = seat,
            GroupSlot = groupSlot
        };
        lock (_gate)
        {
            _peers.Add(peer);
        }
        _ = ReadLoopAsync(peer, _cts?.Token ?? CancellationToken.None);
        PeerConnected?.Invoke();
        TopologyChanged?.Invoke();
        return peer;
    }

    private async Task ReadLoopAsync(Chess3DNetworkPeer peer, CancellationToken token)
    {
        try
        {
            using var reader = new StreamReader(peer.Stream, Encoding.UTF8, leaveOpen: true);
            while (!token.IsCancellationRequested && peer.Client.Connected)
            {
                var line = await reader.ReadLineAsync(token);
                if (line == null)
                {
                    break;
                }
                var message = JsonSerializer.Deserialize<Chess3DNetworkMessage>(line);
                if (message == null)
                {
                    continue;
                }
                await HandleIncomingAsync(peer, message);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException ex)
        {
            StatusChanged?.Invoke($"3DNet: disconnected, {ex.Message}");
        }
        catch (JsonException ex)
        {
            StatusChanged?.Invoke($"3DNet: bad message, {ex.Message}");
        }
        finally
        {
            RemovePeer(peer);
        }
    }

    private async Task HandleIncomingAsync(Chess3DNetworkPeer peer, Chess3DNetworkMessage message)
    {
        if (message.Type == "hello3d")
        {
            await HandleHelloAsync(peer, message);
            return;
        }
        if (message.Type == "seat3d")
        {
            LocalSeat = message.Seat;
            StatusChanged?.Invoke($"3DNet: assigned seat {LocalSeat}");
            TopologyChanged?.Invoke();
            return;
        }

        if (string.Equals(message.SourceNodeId, _nodeId, StringComparison.OrdinalIgnoreCase) || !Remember(message.MessageId))
        {
            return;
        }

        MessageReceived?.Invoke(message);
        await BroadcastAsync(message, peer);
    }

    private async Task HandleHelloAsync(Chess3DNetworkPeer peer, Chess3DNetworkMessage message)
    {
        peer.RemoteNodeId = message.SourceNodeId;
        peer.Kind = message.Kind == (int)Chess3DNetworkPeerKind.Group
            ? Chess3DNetworkPeerKind.Group
            : Chess3DNetworkPeerKind.Player;
        if (_listener != null)
        {
            AssignSlot(peer, peer.Kind == Chess3DNetworkPeerKind.Player ? message.Seat : message.GroupSlot);
            await SendToPeerAsync(peer, new Chess3DNetworkMessage
            {
                Type = "seat3d",
                Kind = (int)peer.Kind,
                Seat = peer.Seat,
                GroupSlot = peer.GroupSlot,
                GroupId = _groupId,
                SourceNodeId = _nodeId,
                MessageId = NewMessageId()
            });
            StatusChanged?.Invoke($"3DNet: assigned {(peer.Kind == Chess3DNetworkPeerKind.Player ? "player" : "group")} slot {(peer.Kind == Chess3DNetworkPeerKind.Player ? peer.Seat : peer.GroupSlot)}");
        }
        TopologyChanged?.Invoke();
        PeerConnected?.Invoke();
    }

    private void AssignSlot(Chess3DNetworkPeer peer, int requested)
    {
        lock (_gate)
        {
            if (peer.Kind == Chess3DNetworkPeerKind.Group)
            {
                peer.GroupSlot = PickSlot(requested, _peers.Where(p => p != peer && p.Kind == Chess3DNetworkPeerKind.Group).Select(p => p.GroupSlot));
                peer.Seat = 0;
                return;
            }
            peer.Seat = PickSlot(requested, _peers.Where(p => p != peer && p.Kind == Chess3DNetworkPeerKind.Player).Select(p => p.Seat).Append(LocalSeat));
            peer.GroupSlot = 0;
        }
    }

    private static int PickSlot(int requested, IEnumerable<int> usedSlots)
    {
        var used = usedSlots.Where(s => s > 0).ToHashSet();
        if (requested >= 1 && requested <= MaxSlots && !used.Contains(requested))
        {
            return requested;
        }
        for (var slot = 1; slot <= MaxSlots; ++slot)
        {
            if (!used.Contains(slot))
            {
                return slot;
            }
        }
        return 0;
    }

    private async Task BroadcastAsync(Chess3DNetworkMessage message, Chess3DNetworkPeer except)
    {
        Chess3DNetworkPeer[] peers;
        lock (_gate)
        {
            peers = _peers.Where(p => p != except).ToArray();
        }
        foreach (var peer in peers)
        {
            await SendToPeerAsync(peer, message);
        }
    }

    private async Task SendToPeerAsync(Chess3DNetworkPeer peer, Chess3DNetworkMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        await _sendLock.WaitAsync();
        try
        {
            await peer.Writer.WriteLineAsync(json);
            await peer.Writer.FlushAsync();
        }
        catch (IOException ex)
        {
            StatusChanged?.Invoke($"3DNet: send failed, {ex.Message}");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private void RemovePeer(Chess3DNetworkPeer peer)
    {
        lock (_gate)
        {
            _peers.Remove(peer);
        }
        peer.Dispose();
        TopologyChanged?.Invoke();
    }

    private void PrepareOutgoing(Chess3DNetworkMessage message)
    {
        message.SourceNodeId = _nodeId;
        message.GroupId = string.IsNullOrWhiteSpace(message.GroupId) ? _groupId : message.GroupId;
        message.MessageId = string.IsNullOrWhiteSpace(message.MessageId) ? NewMessageId() : message.MessageId;
        if (message.Seat == 0 && LocalSeat > 0)
        {
            message.Seat = LocalSeat;
        }
        message.Sequence = Interlocked.Increment(ref _sequence);
    }

    private string NewMessageId()
    {
        return $"{_nodeId}:{Interlocked.Increment(ref _sequence)}";
    }

    private bool Remember(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return true;
        }
        lock (_gate)
        {
            if (_seenMessages.Count > 4096)
            {
                _seenMessages.Clear();
            }
            return _seenMessages.Add(messageId);
        }
    }
}

internal sealed class Chess3DNetworkPeer : IDisposable
{
    public Chess3DNetworkPeer(TcpClient client, NetworkStream stream)
    {
        Client = client;
        Stream = stream;
        Writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
    }

    public TcpClient Client { get; }
    public NetworkStream Stream { get; }
    public StreamWriter Writer { get; }
    public Chess3DNetworkPeerKind Kind { get; set; }
    public int Seat { get; set; }
    public int GroupSlot { get; set; }
    public string RemoteNodeId { get; set; } = "";

    public void Dispose()
    {
        Writer.Dispose();
        Client.Dispose();
    }
}

internal sealed class Chess3DNetworkMessage
{
    public string Type { get; set; } = "";
    public string MessageId { get; set; } = "";
    public string SourceNodeId { get; set; } = "";
    public string GroupId { get; set; } = "";
    public long Sequence { get; set; }
    public int Kind { get; set; }
    public int Seat { get; set; }
    public int GroupSlot { get; set; }
    public int SideToMove { get; set; }
    public int FromX { get; set; }
    public int FromY { get; set; }
    public int FromZ { get; set; }
    public int ToX { get; set; }
    public int ToY { get; set; }
    public int ToZ { get; set; }
    public int Promotion { get; set; }
    public int Axis { get; set; }
    public int Layer { get; set; }
    public int QuarterTurns { get; set; }
    public int[] Board { get; set; } = Array.Empty<int>();
}
