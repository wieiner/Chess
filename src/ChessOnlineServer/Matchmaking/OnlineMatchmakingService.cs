using ChessOnlineProtocol;

namespace ChessOnlineServer.Matchmaking;

public sealed class OnlineMatchmakingService
{
    private readonly object _gate = new();
    private readonly Dictionary<string, MatchmakingTicketState> _ticketsByPlayer = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Queue<MatchmakingTicketState>> _queues = new(StringComparer.OrdinalIgnoreCase);
    private int _matchNumber;

    public OnlineMatchmakingResult Join(
        string playerId,
        string clientId,
        OnlineMatchmakingCommand command,
        OnlineRoomRegistry registry,
        string profileRoot)
    {
        lock (_gate)
        {
            CleanupExpiredLocked();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return OnlineMatchmakingResult.Fail(OnlineRejectReasons.IllegalAction, "Authenticated player is required for matchmaking.");
            }
            if (_ticketsByPlayer.ContainsKey(playerId))
            {
                return OnlineMatchmakingResult.Fail(OnlineRejectReasons.AlreadyQueued, "Player already has an active matchmaking ticket.");
            }

            var rulesetId = command.RequestedRulesetId.Trim();
            if (!RuleProfileCatalog.TryResolve(profileRoot, rulesetId, out var profile))
            {
                return OnlineMatchmakingResult.Fail(OnlineRejectReasons.UnsupportedRuleset, "Ruleset is not one of the five Chess3D RuleProfiles.");
            }

            var ticket = new MatchmakingTicketState
            {
                TicketId = $"ticket-{Guid.NewGuid():N}",
                PlayerId = playerId,
                ClientId = clientId,
                RulesetId = profile.RulesetId,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Clamp(command.ExpireSeconds <= 0 ? 120 : command.ExpireSeconds, 10, 3600)),
                State = "Queued"
            };

            _ticketsByPlayer[playerId] = ticket;
            if (!_queues.TryGetValue(profile.RulesetId, out var queue))
            {
                queue = new Queue<MatchmakingTicketState>();
                _queues[profile.RulesetId] = queue;
            }
            queue.Enqueue(ticket);

            var needed = RequiredPlayers(profile.RulesetId);
            if (queue.Count >= needed)
            {
                var matched = Enumerable.Range(0, needed).Select(_ => queue.Dequeue()).ToArray();
                _matchNumber++;
                var roomId = $"match-{_matchNumber}-{ShortProfile(profile.RulesetId)}";
                var tableId = $"table-{_matchNumber}";
                var creator = matched[0];
                var createEnv = Envelope(OnlineMessageTypes.CreateRoom, roomId, "", creator.ClientId, creator.PlayerId);
                var room = registry.CreateRoom(createEnv, new OnlineRoomCommand
                {
                    RoomId = roomId,
                    DisplayName = $"Match {_matchNumber} {profile.DisplayName}",
                    MaxTables = 1
                });
                if (room.Envelope.MessageType != OnlineMessageTypes.RoomCreated)
                {
                    return OnlineMatchmakingResult.Fail(room.Error?.ReasonCode ?? OnlineRejectReasons.InternalError, room.Error?.ReasonText ?? "Room creation failed.");
                }
                registry.JoinRoom(Envelope(OnlineMessageTypes.JoinRoom, roomId, "", creator.ClientId, creator.PlayerId));
                var table = registry.CreateTable(Envelope(OnlineMessageTypes.CreateTable, roomId, "", creator.ClientId, creator.PlayerId), new OnlineTableCommand
                {
                    TableId = tableId,
                    RulesetId = profile.RulesetId
                });
                if (table.Envelope.MessageType != OnlineMessageTypes.TableCreated)
                {
                    return OnlineMatchmakingResult.Fail(table.Error?.ReasonCode ?? OnlineRejectReasons.InternalError, table.Error?.ReasonText ?? "Table creation failed.");
                }

                for (var i = 0; i < matched.Length; i++)
                {
                    var seat = i + 1;
                    var t = matched[i];
                    registry.JoinRoom(Envelope(OnlineMessageTypes.JoinRoom, roomId, "", t.ClientId, t.PlayerId));
                    var assigned = registry.JoinTableSeat(Envelope(OnlineMessageTypes.JoinTableSeat, roomId, tableId, t.ClientId, t.PlayerId), new OnlineTableCommand { SeatIndex = seat });
                    if (assigned.Envelope.MessageType != OnlineMessageTypes.SeatAssigned)
                    {
                        return OnlineMatchmakingResult.Fail(assigned.Error?.ReasonCode ?? OnlineRejectReasons.InternalError, assigned.Error?.ReasonText ?? "Seat assignment failed.");
                    }
                    t.State = "Matched";
                    t.RoomId = roomId;
                    t.TableId = tableId;
                    t.SeatIndex = seat;
                    _ticketsByPlayer.Remove(t.PlayerId);
                }

                return OnlineMatchmakingResult.MatchFound(matched.Select(ToDto).ToList(), roomId, tableId);
            }

            return OnlineMatchmakingResult.Queued(ToDto(ticket), QueueCountLocked(profile.RulesetId));
        }
    }

    public OnlineMatchmakingResult Cancel(string playerId)
    {
        lock (_gate)
        {
            CleanupExpiredLocked();
            if (!_ticketsByPlayer.Remove(playerId, out var ticket))
            {
                return OnlineMatchmakingResult.Fail(OnlineRejectReasons.NotQueued, "Player is not queued.");
            }
            ticket.State = "Cancelled";
            return OnlineMatchmakingResult.Cancelled(ToDto(ticket));
        }
    }

    public OnlineMatchmakingStatus Status(string playerId)
    {
        lock (_gate)
        {
            CleanupExpiredLocked();
            return _ticketsByPlayer.TryGetValue(playerId, out var ticket)
                ? ToStatus(ToDto(ticket), QueueCountLocked(ticket.RulesetId))
                : new OnlineMatchmakingStatus { PlayerId = playerId, State = "Idle", QueueCount = TotalQueueCountLocked() };
        }
    }

    public OnlineMatchmakingStatus QueueSummary()
    {
        lock (_gate)
        {
            CleanupExpiredLocked();
            return new OnlineMatchmakingStatus
            {
                State = "Summary",
                QueueCount = TotalQueueCountLocked(),
                Tickets = _queues.Values.SelectMany(q => q).Where(t => t.State == "Queued").Select(ToDto).ToList()
            };
        }
    }

    public int ActiveQueueCount
    {
        get
        {
            lock (_gate)
            {
                CleanupExpiredLocked();
                return TotalQueueCountLocked();
            }
        }
    }

    private static int RequiredPlayers(string rulesetId) =>
        rulesetId.Contains("single-side", StringComparison.OrdinalIgnoreCase) ? 1 : 2;

    private int QueueCountLocked(string rulesetId) =>
        _queues.TryGetValue(rulesetId, out var queue) ? queue.Count(t => t.State == "Queued") : 0;

    private int TotalQueueCountLocked() => _queues.Values.Sum(q => q.Count(t => t.State == "Queued"));

    private void CleanupExpiredLocked()
    {
        var now = DateTime.UtcNow;
        foreach (var ticket in _ticketsByPlayer.Values.Where(t => t.ExpiresAtUtc <= now).ToArray())
        {
            ticket.State = "Expired";
            _ticketsByPlayer.Remove(ticket.PlayerId);
        }
        foreach (var key in _queues.Keys.ToArray())
        {
            var active = _queues[key].Where(t => t.State == "Queued" && t.ExpiresAtUtc > now).ToArray();
            _queues[key] = new Queue<MatchmakingTicketState>(active);
        }
    }

    private static OnlineMatchmakingTicket ToDto(MatchmakingTicketState ticket) => new()
    {
        TicketId = ticket.TicketId,
        PlayerId = ticket.PlayerId,
        RequestedRulesetId = ticket.RulesetId,
        State = ticket.State,
        RoomId = ticket.RoomId,
        TableId = ticket.TableId,
        SeatIndex = ticket.SeatIndex,
        CreatedAtUtc = ticket.CreatedAtUtc.ToString("O"),
        ExpiresAtUtc = ticket.ExpiresAtUtc.ToString("O")
    };

    private static OnlineMatchmakingStatus ToStatus(OnlineMatchmakingTicket ticket, int queueCount) => new()
    {
        TicketId = ticket.TicketId,
        PlayerId = ticket.PlayerId,
        RequestedRulesetId = ticket.RequestedRulesetId,
        State = ticket.State,
        RoomId = ticket.RoomId,
        TableId = ticket.TableId,
        SeatIndex = ticket.SeatIndex,
        QueueCount = queueCount,
        Tickets = new List<OnlineMatchmakingTicket> { ticket }
    };

    private static OnlineMessageEnvelope Envelope(string type, string roomId, string tableId, string clientId, string playerId) => new()
    {
        MessageType = type,
        MessageId = Guid.NewGuid().ToString("N"),
        RoomId = roomId,
        TableId = tableId,
        ClientId = clientId,
        PlayerId = playerId,
        SentAtUtc = DateTime.UtcNow.ToString("O")
    };

    private static string ShortProfile(string rulesetId)
    {
        var dash = rulesetId.IndexOf('-', StringComparison.Ordinal);
        return dash > 0 ? rulesetId[..dash] : "profile";
    }
}

public sealed class OnlineMatchmakingResult
{
    public string MessageType { get; init; } = "";
    public string ErrorCode { get; init; } = "";
    public string ErrorText { get; init; } = "";
    public OnlineMatchmakingStatus Status { get; init; } = new();
    public IReadOnlyList<OnlineMatchmakingTicket> MatchedTickets { get; init; } = Array.Empty<OnlineMatchmakingTicket>();
    public string RoomId { get; init; } = "";
    public string TableId { get; init; } = "";

    public static OnlineMatchmakingResult Queued(OnlineMatchmakingTicket ticket, int queueCount) => new()
    {
        MessageType = OnlineMessageTypes.MatchmakingJoined,
        Status = new OnlineMatchmakingStatus
        {
            TicketId = ticket.TicketId,
            PlayerId = ticket.PlayerId,
            RequestedRulesetId = ticket.RequestedRulesetId,
            State = ticket.State,
            QueueCount = queueCount,
            Tickets = new List<OnlineMatchmakingTicket> { ticket }
        }
    };

    public static OnlineMatchmakingResult Cancelled(OnlineMatchmakingTicket ticket) => new()
    {
        MessageType = OnlineMessageTypes.MatchmakingCancelled,
        Status = new OnlineMatchmakingStatus
        {
            TicketId = ticket.TicketId,
            PlayerId = ticket.PlayerId,
            RequestedRulesetId = ticket.RequestedRulesetId,
            State = ticket.State,
            Tickets = new List<OnlineMatchmakingTicket> { ticket }
        }
    };

    public static OnlineMatchmakingResult MatchFound(IReadOnlyList<OnlineMatchmakingTicket> tickets, string roomId, string tableId) => new()
    {
        MessageType = OnlineMessageTypes.MatchFound,
        MatchedTickets = tickets,
        RoomId = roomId,
        TableId = tableId,
        Status = new OnlineMatchmakingStatus
        {
            State = "Matched",
            RoomId = roomId,
            TableId = tableId,
            Tickets = tickets.ToList()
        }
    };

    public static OnlineMatchmakingResult Fail(string code, string text) => new()
    {
        MessageType = OnlineMessageTypes.MatchmakingError,
        ErrorCode = code,
        ErrorText = text,
        Status = new OnlineMatchmakingStatus { State = "Error", ErrorCode = code, ErrorText = text }
    };
}

internal sealed class MatchmakingTicketState
{
    public string TicketId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string RulesetId { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string State { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public int SeatIndex { get; set; }
}
