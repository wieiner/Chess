using System.Collections.Concurrent;
using ChessOnlineProtocol;

namespace ChessOnlineServer;

public sealed class OnlineHubConnectionRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, OnlineConnectionSession> _byConnection = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OnlineConnectionSession> _bySessionToken = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<DateTime>> _commandTimes = new(StringComparer.Ordinal);

    public OnlineConnectionSession Hello(string connectionId, OnlineMessageEnvelope envelope)
    {
        lock (_gate)
        {
            if (!string.IsNullOrWhiteSpace(envelope.SessionToken) &&
                _bySessionToken.TryGetValue(envelope.SessionToken, out var existing) &&
                (string.IsNullOrWhiteSpace(envelope.PlayerId) || string.Equals(existing.PlayerId, envelope.PlayerId, StringComparison.OrdinalIgnoreCase)))
            {
                existing.ConnectionIds.Add(connectionId);
                existing.IsConnected = true;
                existing.LastSeenUtc = DateTime.UtcNow;
                _byConnection[connectionId] = existing;
                return existing.Clone();
            }

            var playerId = string.IsNullOrWhiteSpace(envelope.PlayerId)
                ? $"player-{Guid.NewGuid():N}"
                : envelope.PlayerId.Trim();
            var session = new OnlineConnectionSession
            {
                ClientId = string.IsNullOrWhiteSpace(envelope.ClientId) ? $"client-{Guid.NewGuid():N}" : envelope.ClientId.Trim(),
                PlayerId = playerId,
                SessionToken = Guid.NewGuid().ToString("N"),
                IsConnected = true,
                LastSeenUtc = DateTime.UtcNow
            };
            session.ConnectionIds.Add(connectionId);
            _byConnection[connectionId] = session;
            _bySessionToken[session.SessionToken] = session;
            return session.Clone();
        }
    }

    public OnlineConnectionSession HelloAuthenticated(string connectionId, OnlineMessageEnvelope envelope, string playerId, string sessionId)
    {
        lock (_gate)
        {
            if (_bySessionToken.TryGetValue(sessionId, out var existing))
            {
                existing.ConnectionIds.Add(connectionId);
                existing.IsConnected = true;
                existing.LastSeenUtc = DateTime.UtcNow;
                _byConnection[connectionId] = existing;
                return existing.Clone();
            }

            var session = new OnlineConnectionSession
            {
                ClientId = string.IsNullOrWhiteSpace(envelope.ClientId) ? $"client-{Guid.NewGuid():N}" : envelope.ClientId.Trim(),
                PlayerId = playerId.Trim(),
                SessionToken = sessionId.Trim(),
                IsConnected = true,
                LastSeenUtc = DateTime.UtcNow
            };
            session.ConnectionIds.Add(connectionId);
            _byConnection[connectionId] = session;
            _bySessionToken[session.SessionToken] = session;
            return session.Clone();
        }
    }

    public bool CanReconnect(string playerId, string sessionToken)
    {
        lock (_gate)
        {
            return !string.IsNullOrWhiteSpace(sessionToken) &&
                _bySessionToken.TryGetValue(sessionToken, out var existing) &&
                (string.IsNullOrWhiteSpace(playerId) || string.Equals(existing.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool TryGet(string connectionId, out OnlineConnectionSession session)
    {
        lock (_gate)
        {
            if (_byConnection.TryGetValue(connectionId, out var found))
            {
                session = found.Clone();
                return true;
            }
        }
        session = new OnlineConnectionSession();
        return false;
    }

    public bool TryGetByPlayerId(string playerId, out OnlineConnectionSession session)
    {
        lock (_gate)
        {
            var found = _byConnection.Values.FirstOrDefault(s => string.Equals(s.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
            if (found != null)
            {
                session = found.Clone();
                return true;
            }
        }
        session = new OnlineConnectionSession();
        return false;
    }

    public void SetMembership(string connectionId, string roomId, string tableId = "")
    {
        lock (_gate)
        {
            if (_byConnection.TryGetValue(connectionId, out var session))
            {
                session.RoomId = roomId;
                if (!string.IsNullOrWhiteSpace(tableId))
                {
                    session.TableId = tableId;
                }
                session.LastSeenUtc = DateTime.UtcNow;
            }
        }
    }

    public void Disconnected(string connectionId)
    {
        lock (_gate)
        {
            if (_byConnection.Remove(connectionId, out var session))
            {
                session.ConnectionIds.Remove(connectionId);
                session.IsConnected = session.ConnectionIds.Count > 0;
                session.LastSeenUtc = DateTime.UtcNow;
            }
            _commandTimes.Remove(connectionId);
        }
    }

    public bool AllowCommand(string connectionId, int permitLimit, int windowSeconds)
    {
        lock (_gate)
        {
            var now = DateTime.UtcNow;
            if (!_commandTimes.TryGetValue(connectionId, out var times))
            {
                times = new List<DateTime>();
                _commandTimes[connectionId] = times;
            }
            times.RemoveAll(t => (now - t).TotalSeconds > windowSeconds);
            if (times.Count >= permitLimit)
            {
                return false;
            }
            times.Add(now);
            return true;
        }
    }

    public int ActiveConnectionCount
    {
        get
        {
            lock (_gate)
            {
                return _byConnection.Count;
            }
        }
    }
}

public sealed class OnlineConnectionSession
{
    public string ClientId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string SessionToken { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public bool IsConnected { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public HashSet<string> ConnectionIds { get; } = new(StringComparer.Ordinal);

    public OnlineConnectionSession Clone()
    {
        var clone = new OnlineConnectionSession
        {
            ClientId = ClientId,
            PlayerId = PlayerId,
            SessionToken = SessionToken,
            RoomId = RoomId,
            TableId = TableId,
            IsConnected = IsConnected,
            LastSeenUtc = LastSeenUtc
        };
        foreach (var connectionId in ConnectionIds)
        {
            clone.ConnectionIds.Add(connectionId);
        }
        return clone;
    }
}
