using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChessOnlinePersistence.Entities;

namespace ChessOnlinePersistence.Repositories;

public sealed class JsonOnlineStore : IOnlineIdentityStore, IOnlineSessionStore, IOnlineRoomPersistenceStore
{
    private readonly object _gate = new();
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private OnlineStoreDocument _document = new();

    public JsonOnlineStore(OnlineStoreOptions options)
    {
        options.Normalize();
        _path = options.StorePath;
        if (options.AutoCreate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
        }
        Load();
    }

    public Task<PlayerAccountEntity?> FindByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_document.Players.FirstOrDefault(p => string.Equals(p.NormalizedUserName, normalizedUserName, StringComparison.Ordinal)));
        }
    }

    public Task<PlayerAccountEntity?> FindByPlayerIdAsync(string playerId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_document.Players.FirstOrDefault(p => string.Equals(p.PlayerId, playerId, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task CreatePlayerAsync(PlayerAccountEntity account, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_document.Players.Any(p => string.Equals(p.NormalizedUserName, account.NormalizedUserName, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Player username already exists.");
            }
            _document.Players.Add(account);
            Save();
            return Task.CompletedTask;
        }
    }

    public Task UpdatePlayerAsync(PlayerAccountEntity account, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var index = _document.Players.FindIndex(p => string.Equals(p.PlayerId, account.PlayerId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                throw new InvalidOperationException("Player account not found.");
            }
            _document.Players[index] = account;
            Save();
            return Task.CompletedTask;
        }
    }

    public Task<PlayerSessionEntity?> FindSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_document.Sessions.FirstOrDefault(s => string.Equals(s.SessionId, sessionId, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task CreateSessionAsync(PlayerSessionEntity session, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _document.Sessions.Add(session);
            Save();
            return Task.CompletedTask;
        }
    }

    public Task UpdateSessionAsync(PlayerSessionEntity session, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var index = _document.Sessions.FindIndex(s => string.Equals(s.SessionId, session.SessionId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                throw new InvalidOperationException("Session not found.");
            }
            _document.Sessions[index] = session;
            Save();
            return Task.CompletedTask;
        }
    }

    public Task RevokeSessionAsync(string sessionId, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var session = _document.Sessions.FirstOrDefault(s => string.Equals(s.SessionId, sessionId, StringComparison.OrdinalIgnoreCase));
            if (session != null)
            {
                session.RevokedAtUtc = revokedAtUtc;
                Save();
            }
            return Task.CompletedTask;
        }
    }

    public Task UpdateLastSeenAsync(string sessionId, string roomId, string tableId, int seatIndex, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var session = _document.Sessions.FirstOrDefault(s => string.Equals(s.SessionId, sessionId, StringComparison.OrdinalIgnoreCase));
            if (session != null)
            {
                session.LastSeenAtUtc = DateTime.UtcNow;
                session.LastKnownRoomId = roomId;
                session.LastKnownTableId = tableId;
                session.LastKnownSeatIndex = seatIndex;
                Save();
            }
            return Task.CompletedTask;
        }
    }

    public Task UpsertRoomAsync(PersistentRoomEntity room, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var index = _document.Rooms.FindIndex(r => string.Equals(r.RoomId, room.RoomId, StringComparison.OrdinalIgnoreCase));
            if (index < 0) _document.Rooms.Add(room); else _document.Rooms[index] = room;
            Save();
            return Task.CompletedTask;
        }
    }

    public Task UpsertTableAsync(PersistentTableEntity table, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var index = _document.Tables.FindIndex(t => string.Equals(t.TableId, table.TableId, StringComparison.OrdinalIgnoreCase));
            if (index < 0) _document.Tables.Add(table); else _document.Tables[index] = table;
            Save();
            return Task.CompletedTask;
        }
    }

    public Task UpsertSeatAsync(PersistentSeatEntity seat, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var index = _document.Seats.FindIndex(s => string.Equals(s.TableId, seat.TableId, StringComparison.OrdinalIgnoreCase) && s.SeatIndex == seat.SeatIndex);
            if (index < 0) _document.Seats.Add(seat); else _document.Seats[index] = seat;
            Save();
            return Task.CompletedTask;
        }
    }

    public Task AppendActionAsync(PersistentActionLogEntity action, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_document.Actions.Any(a => string.Equals(a.TableId, action.TableId, StringComparison.OrdinalIgnoreCase) && a.ServerSeq == action.ServerSeq))
            {
                throw new InvalidOperationException("Duplicate action server sequence.");
            }
            action.PreviousEventHash = _document.Actions
                .Where(a => string.Equals(a.TableId, action.TableId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.ServerSeq)
                .FirstOrDefault()?.EventHash ?? "";
            action.EventHash = ComputeEventHash(action);
            _document.Actions.Add(action);
            Save();
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<PersistentRoomEntity>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<PersistentRoomEntity>>(_document.Rooms.ToArray());
        }
    }

    public Task<IReadOnlyList<PersistentTableEntity>> GetTablesAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<PersistentTableEntity>>(_document.Tables.ToArray());
        }
    }

    public Task<IReadOnlyList<PersistentSeatEntity>> GetSeatsAsync(string tableId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<PersistentSeatEntity>>(_document.Seats.Where(s => string.Equals(s.TableId, tableId, StringComparison.OrdinalIgnoreCase)).ToArray());
        }
    }

    public Task<IReadOnlyList<PersistentActionLogEntity>> GetActionLogAsync(string tableId, long fromServerSeq = 1, int maxCount = 256, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<PersistentActionLogEntity>>(_document.Actions
                .Where(a => string.Equals(a.TableId, tableId, StringComparison.OrdinalIgnoreCase) && a.ServerSeq >= fromServerSeq)
                .OrderBy(a => a.ServerSeq)
                .Take(Math.Clamp(maxCount, 1, 10000))
                .ToArray());
        }
    }

    private void Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_path))
            {
                _document = new OnlineStoreDocument();
                Save();
                return;
            }
            var json = File.ReadAllText(_path);
            _document = JsonSerializer.Deserialize<OnlineStoreDocument>(json, _jsonOptions) ?? new OnlineStoreDocument();
        }
    }

    private void Save()
    {
        _document.SchemaVersion = "0.1";
        _document.UpdatedAtUtc = DateTime.UtcNow;
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_document, _jsonOptions), Encoding.UTF8);
        if (File.Exists(_path))
        {
            File.Replace(temp, _path, null);
        }
        else
        {
            File.Move(temp, _path);
        }
    }

    private static string ComputeEventHash(PersistentActionLogEntity action)
    {
        var material = string.Join("|", action.PreviousEventHash, action.TableId, action.ServerSeq, action.ActorPlayerId, action.ActionKind, action.ActionJson, action.StateHashBefore, action.StateHashAfter, action.Notation);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
    }

    private sealed class OnlineStoreDocument
    {
        public string SchemaVersion { get; set; } = "0.1";
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public List<PlayerAccountEntity> Players { get; set; } = new();
        public List<PlayerSessionEntity> Sessions { get; set; } = new();
        public List<PersistentRoomEntity> Rooms { get; set; } = new();
        public List<PersistentTableEntity> Tables { get; set; } = new();
        public List<PersistentSeatEntity> Seats { get; set; } = new();
        public List<PersistentActionLogEntity> Actions { get; set; } = new();
    }
}

