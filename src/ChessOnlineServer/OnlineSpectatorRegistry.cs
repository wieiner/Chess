using ChessOnlineProtocol;
using System.Globalization;
using System.Text.Json.Serialization;

namespace ChessOnlineServer;

public sealed class OnlineSpectatorRegistry
{
    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<SpectatorKey, SpectatorMembership> _byViewer = new();
    private readonly Dictionary<string, SpectatorKey> _byConnection = new(StringComparer.Ordinal);
    private readonly Dictionary<TableKey, DateTime> _tableUpdatedUtc = new();

    public OnlineSpectatorRegistry(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

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
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var key = SpectatorKey.Create(roomId, tableId, viewerPlayerId);

            if (_byConnection.TryGetValue(connectionId, out var priorKey) && priorKey != key)
            {
                _byConnection.Remove(connectionId);
                _byViewer.Remove(priorKey);
                TouchLocked(priorKey.RoomId, priorKey.TableId, now);
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
                if (!duplicate)
                {
                    TouchLocked(key.RoomId, key.TableId, now);
                }
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
            TouchLocked(key.RoomId, key.TableId, now);
            return new OnlineSpectatorRegistration(CountLocked(key.RoomId, key.TableId), false, false, "");
        }
    }

    public OnlineSpectatorDisconnectResult RemoveConnection(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return new OnlineSpectatorDisconnectResult(false, "", "", 0);
        }

        lock (_gate)
        {
            if (!_byConnection.Remove(connectionId, out var key) ||
                !_byViewer.TryGetValue(key, out var membership) ||
                !string.Equals(membership.ConnectionId, connectionId, StringComparison.Ordinal))
            {
                return new OnlineSpectatorDisconnectResult(false, "", "", 0);
            }

            _byViewer.Remove(key);
            TouchLocked(key.RoomId, key.TableId, _timeProvider.GetUtcNow().UtcDateTime);
            return new OnlineSpectatorDisconnectResult(
                true,
                membership.RoomId,
                membership.TableId,
                CountLocked(key.RoomId, key.TableId));
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

    public int PruneOrphans(
        Func<string, string, bool> tableExists,
        TimeSpan grace,
        DateTime nowUtc,
        int maxRemovals)
    {
        ArgumentNullException.ThrowIfNull(tableExists);
        if (maxRemovals <= 0)
        {
            return 0;
        }

        lock (_gate)
        {
            var candidates = _byViewer
                .Where(pair => !tableExists(pair.Value.RoomId, pair.Value.TableId) &&
                    nowUtc - pair.Value.LastSeenUtc >= grace)
                .Take(maxRemovals)
                .ToArray();
            foreach (var candidate in candidates)
            {
                _byViewer.Remove(candidate.Key);
                _byConnection.Remove(candidate.Value.ConnectionId);
                TouchLocked(candidate.Key.RoomId, candidate.Key.TableId, nowUtc);
            }
            return candidates.Length;
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
                var roomId = Normalize(row.RoomId);
                var tableId = Normalize(row.TableId);
                row.SpectatorCount = CountLocked(roomId, tableId);
                if (_tableUpdatedUtc.TryGetValue(new TableKey(roomId, tableId), out var membershipUpdatedUtc) &&
                    (!DateTime.TryParse(row.UpdatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var rowUpdatedUtc) ||
                     membershipUpdatedUtc > rowUpdatedUtc))
                {
                    row.UpdatedUtc = membershipUpdatedUtc.ToString("O");
                }
            }
        }
    }

    private int CountLocked(string roomId, string tableId) =>
        _byViewer.Keys.Count(key => key.RoomId == roomId && key.TableId == tableId);

    private void TouchLocked(string roomId, string tableId, DateTime now) =>
        _tableUpdatedUtc[new TableKey(roomId, tableId)] = now;

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private readonly record struct SpectatorKey(string RoomId, string TableId, string ViewerPlayerId)
    {
        public static SpectatorKey Create(string roomId, string tableId, string viewerPlayerId) =>
            new(Normalize(roomId), Normalize(tableId), Normalize(viewerPlayerId));
    }

    private readonly record struct TableKey(string RoomId, string TableId);

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

public sealed record OnlineSpectatorDisconnectResult(
    bool Removed,
    string RoomId,
    string TableId,
    int TableSpectatorCount);
