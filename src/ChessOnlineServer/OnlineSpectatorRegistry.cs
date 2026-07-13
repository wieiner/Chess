using ChessOnlineProtocol;
using System.Text.Json.Serialization;

namespace ChessOnlineServer;

public sealed class OnlineSpectatorRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<SpectatorKey, SpectatorMembership> _byViewer = new();
    private readonly Dictionary<string, SpectatorKey> _byConnection = new(StringComparer.Ordinal);

    public OnlineSpectatorRegistration Register(
        string roomId,
        string tableId,
        string viewerPlayerId,
        string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableId);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewerPlayerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_gate)
        {
            var now = DateTime.UtcNow;
            var key = SpectatorKey.Create(roomId, tableId, viewerPlayerId);

            if (_byConnection.TryGetValue(connectionId, out var priorKey) && priorKey != key)
            {
                _byConnection.Remove(connectionId);
                _byViewer.Remove(priorKey);
            }

            if (_byViewer.TryGetValue(key, out var existing))
            {
                var duplicate = string.Equals(existing.ConnectionId, connectionId, StringComparison.Ordinal);
                var replacedConnectionId = duplicate ? "" : existing.ConnectionId;
                if (!duplicate)
                {
                    _byConnection.Remove(existing.ConnectionId);
                    existing.ConnectionId = connectionId;
                    _byConnection[connectionId] = key;
                }
                existing.LastSeenUtc = now;
                return new OnlineSpectatorRegistration(
                    CountLocked(key.RoomId, key.TableId),
                    duplicate,
                    !duplicate,
                    replacedConnectionId);
            }

            _byViewer[key] = new SpectatorMembership
            {
                RoomId = roomId.Trim(),
                TableId = tableId.Trim(),
                ViewerPlayerId = viewerPlayerId.Trim(),
                ConnectionId = connectionId,
                JoinedUtc = now,
                LastSeenUtc = now
            };
            _byConnection[connectionId] = key;
            return new OnlineSpectatorRegistration(CountLocked(key.RoomId, key.TableId), false, false, "");
        }
    }

    public int Count(string roomId, string tableId)
    {
        lock (_gate)
        {
            return CountLocked(Normalize(roomId), Normalize(tableId));
        }
    }

    public int TotalCount
    {
        get
        {
            lock (_gate)
            {
                return _byViewer.Count;
            }
        }
    }

    public void ApplyCounts(OnlineLobbySnapshot? snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        lock (_gate)
        {
            foreach (var row in snapshot.Tables)
            {
                row.SpectatorCount = CountLocked(Normalize(row.RoomId), Normalize(row.TableId));
            }
        }
    }

    private int CountLocked(string roomId, string tableId) =>
        _byViewer.Keys.Count(key => key.RoomId == roomId && key.TableId == tableId);

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private readonly record struct SpectatorKey(string RoomId, string TableId, string ViewerPlayerId)
    {
        public static SpectatorKey Create(string roomId, string tableId, string viewerPlayerId) =>
            new(Normalize(roomId), Normalize(tableId), Normalize(viewerPlayerId));
    }

    private sealed class SpectatorMembership
    {
        public string RoomId { get; init; } = "";
        public string TableId { get; init; } = "";
        public string ViewerPlayerId { get; init; } = "";
        public string ConnectionId { get; set; } = "";
        public DateTime JoinedUtc { get; init; }
        public DateTime LastSeenUtc { get; set; }
    }
}

public sealed record OnlineSpectatorRegistration(
    int TableSpectatorCount,
    bool IsDuplicate,
    bool ReplacedConnection,
    [property: JsonIgnore] string ReplacedConnectionId);
