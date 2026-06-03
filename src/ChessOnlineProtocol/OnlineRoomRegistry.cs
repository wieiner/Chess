using System.Text.Json;
using ChessApp;

namespace ChessOnlineProtocol;

public enum OnlineRoomState
{
    Open,
    Closed
}

public enum OnlineTableState
{
    WaitingForPlayers,
    ReadyCheck,
    InGame,
    Finished,
    Abandoned
}

public sealed class OnlineRoomRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, OnlineRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _profileRoot;
    private readonly OnlineDiagnostics _diagnostics = new();

    public OnlineRoomRegistry(string profileRoot)
    {
        _profileRoot = profileRoot;
    }

    public IReadOnlyCollection<OnlineRoom> Rooms
    {
        get
        {
            lock (_gate)
            {
                return _rooms.Values.Select(r => r.CloneShallow()).ToArray();
            }
        }
    }

    public OnlineProtocolMessage Hello(OnlineMessageEnvelope envelope)
    {
        return Reply(OnlineMessageTypes.Welcome, envelope, text: "P3E local authority harness ready.");
    }

    public OnlineProtocolMessage CreateRoom(OnlineMessageEnvelope envelope, OnlineRoomCommand command)
    {
        lock (_gate)
        {
            var roomId = string.IsNullOrWhiteSpace(command.RoomId) ? $"room-{_rooms.Count + 1}" : command.RoomId.Trim();
            if (_rooms.ContainsKey(roomId))
            {
                return Reject(envelope, OnlineRejectReasons.IllegalAction, "Room already exists.");
            }

            var room = new OnlineRoom
            {
                RoomId = roomId,
                DisplayName = string.IsNullOrWhiteSpace(command.DisplayName) ? roomId : command.DisplayName.Trim(),
                MaxTables = Math.Clamp(command.MaxTables, 1, 32),
                CreatedAtUtc = DateTime.UtcNow,
                State = OnlineRoomState.Open
            };
            _rooms.Add(room.RoomId, room);
            return Reply(OnlineMessageTypes.RoomCreated, envelope, room: new OnlineRoomCommand
            {
                RoomId = room.RoomId,
                DisplayName = room.DisplayName,
                MaxTables = room.MaxTables
            });
        }
    }

    public OnlineProtocolMessage JoinRoom(OnlineMessageEnvelope envelope)
    {
        lock (_gate)
        {
            if (!TryGetRoom(envelope.RoomId, out var room))
            {
                return Reject(envelope, OnlineRejectReasons.RoomNotFound, "Room not found.");
            }

            var playerId = RequirePlayerId(envelope);
            room.Players[playerId] = new OnlinePlayer
            {
                PlayerId = playerId,
                DisplayName = playerId,
                IsConnected = true,
                LastSeenUtc = DateTime.UtcNow
            };
            _diagnostics.ConnectionCount = room.Players.Count;
            return Reply(OnlineMessageTypes.RoomJoined, envelope, room: new OnlineRoomCommand
            {
                RoomId = room.RoomId,
                DisplayName = room.DisplayName,
                MaxTables = room.MaxTables
            });
        }
    }

    public OnlineProtocolMessage ListRooms(OnlineMessageEnvelope envelope)
    {
        lock (_gate)
        {
            return Reply(OnlineMessageTypes.RoomList, envelope, text: JsonSerializer.Serialize(_rooms.Values.Select(r => r.RoomId).ToArray()));
        }
    }

    public OnlineProtocolMessage CreateTable(OnlineMessageEnvelope envelope, OnlineTableCommand command)
    {
        lock (_gate)
        {
            if (!TryGetRoom(envelope.RoomId, out var room))
            {
                return Reject(envelope, OnlineRejectReasons.RoomNotFound, "Room not found.");
            }
            if (room.Tables.Count >= room.MaxTables)
            {
                return Reject(envelope, OnlineRejectReasons.IllegalAction, "Room table limit reached.");
            }

            var tableId = string.IsNullOrWhiteSpace(command.TableId) ? $"table-{room.Tables.Count + 1}" : command.TableId.Trim();
            if (room.Tables.ContainsKey(tableId))
            {
                return Reject(envelope, OnlineRejectReasons.IllegalAction, "Table already exists.");
            }
            if (!RuleProfileCatalog.TryResolve(_profileRoot, command.RulesetId, out var profile))
            {
                return Reject(envelope, OnlineRejectReasons.UnsupportedRuleset, "Ruleset is not one of the five Chess3D RuleProfiles.");
            }

            var table = new OnlineTable
            {
                RoomId = room.RoomId,
                TableId = tableId,
                RulesetId = profile.RulesetId,
                ProfileFileName = profile.FileName,
                SeatCount = profile.SeatCount,
                State = OnlineTableState.WaitingForPlayers,
                CreatedAtUtc = DateTime.UtcNow
            };
            table.Session = new OnlineGameSession(profile, _profileRoot);
            room.Tables.Add(table.TableId, table);
            _diagnostics.TableCount = room.Tables.Count;
            return Reply(OnlineMessageTypes.TableCreated, envelope, table: new OnlineTableCommand
            {
                TableId = table.TableId,
                RulesetId = table.RulesetId
            });
        }
    }

    public OnlineProtocolMessage JoinTableSeat(OnlineMessageEnvelope envelope, OnlineTableCommand command)
    {
        lock (_gate)
        {
            if (!TryGetTable(envelope.RoomId, envelope.TableId, out _, out var table))
            {
                return Reject(envelope, OnlineRejectReasons.TableNotFound, "Table not found.");
            }

            var seat = command.SeatIndex <= 0 ? 1 : command.SeatIndex;
            if (seat > table.SeatCount)
            {
                return Reject(envelope, OnlineRejectReasons.IllegalAction, "Seat index is outside this profile's seat range.");
            }
            if (table.Seats.TryGetValue(seat, out var existing) && !string.Equals(existing.PlayerId, envelope.PlayerId, StringComparison.OrdinalIgnoreCase))
            {
                return Reject(envelope, OnlineRejectReasons.SeatOccupied, "Seat is already assigned.");
            }

            table.Seats[seat] = new OnlineSeat
            {
                SeatIndex = seat,
                SideId = table.IsHodge ? 0 : seat,
                MacroPlayer = table.IsHodge ? seat : 0,
                PlayerId = RequirePlayerId(envelope),
                IsConnected = true,
                LastSeenUtc = DateTime.UtcNow
            };
            return Reply(OnlineMessageTypes.SeatAssigned, envelope, table: new OnlineTableCommand
            {
                TableId = table.TableId,
                RulesetId = table.RulesetId,
                SeatIndex = seat
            });
        }
    }

    public OnlineProtocolMessage Ready(OnlineMessageEnvelope envelope, OnlineTableCommand command)
    {
        lock (_gate)
        {
            if (!TrySeat(envelope, out _, out var table, out var seat))
            {
                return Reject(envelope, OnlineRejectReasons.PlayerNotSeated, "Player is not seated at this table.");
            }

            seat.IsReady = command.Ready;
            table.State = table.Seats.Values.Any(s => s.IsReady) ? OnlineTableState.ReadyCheck : OnlineTableState.WaitingForPlayers;
            return Reply(OnlineMessageTypes.TableState, envelope, table: new OnlineTableCommand
            {
                TableId = table.TableId,
                RulesetId = table.RulesetId,
                SeatIndex = seat.SeatIndex,
                Ready = seat.IsReady
            });
        }
    }

    public OnlineProtocolMessage StartGame(OnlineMessageEnvelope envelope)
    {
        lock (_gate)
        {
            if (!TrySeat(envelope, out _, out var table, out var seat))
            {
                return Reject(envelope, OnlineRejectReasons.PlayerNotSeated, "Player is not seated at this table.");
            }
            if (!seat.IsReady)
            {
                return Reject(envelope, OnlineRejectReasons.IllegalAction, "Seat must be ready before StartGame.");
            }

            table.Session = new OnlineGameSession(RuleProfileCatalog.ResolveRequired(_profileRoot, table.RulesetId), _profileRoot);
            table.State = OnlineTableState.InGame;
            table.StartedAtUtc = DateTime.UtcNow;
            table.ServerSeq = 0;
            table.ActionLog.Clear();
            table.LastStateHash = table.Session.StateHash;
            _diagnostics.LastStateHash = table.LastStateHash;
            return Reply(OnlineMessageTypes.GameStarted, envelope, snapshot: table.Session.CreateSnapshot(table.RoomId, table.TableId, table.ServerSeq));
        }
    }

    public OnlineProtocolMessage SubmitAction(OnlineMessageEnvelope envelope, OnlineActionCommand command)
    {
        lock (_gate)
        {
            if (!TrySeat(envelope, out _, out var table, out var seat))
            {
                return Reject(envelope, OnlineRejectReasons.PlayerNotSeated, "Player is not seated at this table.");
            }
            if (table.State != OnlineTableState.InGame || table.Session == null)
            {
                return Reject(envelope, OnlineRejectReasons.TableNotInGame, "Table is not in game.");
            }

            var beforeHash = table.Session.StateHash;
            if (!string.IsNullOrWhiteSpace(command.ExpectedStateHashBefore) &&
                !string.Equals(command.ExpectedStateHashBefore, beforeHash, StringComparison.Ordinal))
            {
                var rejected = Reject(envelope, OnlineRejectReasons.StaleStateHash, "Client expected hash does not match authoritative state.");
                rejected.Envelope.MessageType = OnlineMessageTypes.ResyncRequired;
                rejected.Snapshot = table.Session.CreateSnapshot(table.RoomId, table.TableId, table.ServerSeq);
                return rejected;
            }

            if (!ActorMatchesSeat(table, seat, command))
            {
                return Reject(envelope, OnlineRejectReasons.WrongActor, "Player does not own the current turn actor.", beforeHash, table.ServerSeq);
            }

            var applied = table.Session.TryApply(command, out var rejectReason, out var rejectText);
            if (!applied)
            {
                if (!string.Equals(table.Session.StateHash, beforeHash, StringComparison.Ordinal))
                {
                    rejectReason = OnlineRejectReasons.InternalError;
                    rejectText = "Rejected action mutated authoritative state.";
                }
                return Reject(envelope, rejectReason, rejectText, table.Session.StateHash, table.ServerSeq);
            }

            table.ServerSeq++;
            table.LastStateHash = table.Session.StateHash;
            var actionIndex = table.Session.ActionCount;
            var actionEvent = new OnlineActionEvent
            {
                ServerSeq = table.ServerSeq,
                ActionIndex = actionIndex,
                PlayerId = seat.PlayerId,
                SeatIndex = seat.SeatIndex,
                ActionKind = command.ActionKind,
                Command = CloneCommand(command),
                Notation = table.Session.LastActionNotation,
                StateHashAfter = table.LastStateHash,
                GamePhase = table.Session.GamePhase.ToString(),
                GameOutcome = table.Session.GameOutcome.ToString(),
                CreatedAtUtc = DateTime.UtcNow.ToString("O")
            };
            table.ActionLog.Add(actionEvent);
            _diagnostics.LastServerSeq = table.ServerSeq;
            _diagnostics.LastAcceptedAction = actionEvent.Notation;
            _diagnostics.LastStateHash = table.LastStateHash;
            _diagnostics.ActionLogLength = table.ActionLog.Count;
            return Reply(OnlineMessageTypes.ActionAccepted, envelope, action: command, actionLog: new OnlineActionLogChunk
            {
                RoomId = table.RoomId,
                TableId = table.TableId,
                FromServerSeq = actionEvent.ServerSeq,
                ToServerSeq = actionEvent.ServerSeq,
                Events = new List<OnlineActionEvent> { actionEvent }
            });
        }
    }

    public OnlineProtocolMessage RequestSnapshot(OnlineMessageEnvelope envelope)
    {
        lock (_gate)
        {
            if (!TryGetTable(envelope.RoomId, envelope.TableId, out _, out var table) || table.Session == null)
            {
                return Reject(envelope, OnlineRejectReasons.TableNotFound, "Table not found.");
            }
            var snapshot = table.Session.CreateSnapshot(table.RoomId, table.TableId, table.ServerSeq);
            _diagnostics.LastSnapshotBytes = System.Text.Encoding.UTF8.GetByteCount(snapshot.SaveGameJson);
            return Reply(OnlineMessageTypes.AuthoritativeSnapshot, envelope, snapshot: snapshot);
        }
    }

    public OnlineProtocolMessage RequestActionLog(OnlineMessageEnvelope envelope, long fromSeq = 1, int maxCount = 64)
    {
        lock (_gate)
        {
            if (!TryGetTable(envelope.RoomId, envelope.TableId, out _, out var table))
            {
                return Reject(envelope, OnlineRejectReasons.TableNotFound, "Table not found.");
            }
            var events = table.ActionLog
                .Where(e => e.ServerSeq >= fromSeq)
                .Take(Math.Clamp(maxCount, 1, 512))
                .Select(CloneEvent)
                .ToList();
            return Reply(OnlineMessageTypes.ActionLogChunk, envelope, actionLog: new OnlineActionLogChunk
            {
                RoomId = table.RoomId,
                TableId = table.TableId,
                FromServerSeq = events.FirstOrDefault()?.ServerSeq ?? fromSeq,
                ToServerSeq = events.LastOrDefault()?.ServerSeq ?? fromSeq - 1,
                Events = events
            });
        }
    }

    public OnlineDiagnostics GetDiagnostics()
    {
        lock (_gate)
        {
            return new OnlineDiagnostics
            {
                RoomCount = _rooms.Count,
                TableCount = _rooms.Values.Sum(r => r.Tables.Count),
                ConnectionCount = _rooms.Values.Sum(r => r.Players.Count),
                LastServerSeq = _diagnostics.LastServerSeq,
                LastAcceptedAction = _diagnostics.LastAcceptedAction,
                LastRejectReason = _diagnostics.LastRejectReason,
                LastStateHash = _diagnostics.LastStateHash,
                LastSnapshotBytes = _diagnostics.LastSnapshotBytes,
                ActionLogLength = _diagnostics.ActionLogLength,
                ProtocolErrorCount = _diagnostics.ProtocolErrorCount
            };
        }
    }

    public static string HashFromSaveGameJson(string saveGameJson)
    {
        using var engine = new NativeChess3DEngine();
        if (!engine.LoadSaveGameJson(saveGameJson))
        {
            throw new InvalidOperationException("Snapshot savegame did not load into a fresh engine.");
        }
        return engine.GetStateHash();
    }

    public string ReplayActionLogToHash(string rulesetId, IEnumerable<OnlineActionEvent> events)
    {
        var profile = RuleProfileCatalog.ResolveRequired(_profileRoot, rulesetId);
        using var session = new OnlineGameSession(profile, _profileRoot);
        foreach (var actionEvent in events.OrderBy(e => e.ServerSeq))
        {
            if (!session.TryApply(actionEvent.Command, out var reason, out var text))
            {
                throw new InvalidOperationException($"Replay failed at seq {actionEvent.ServerSeq}: {reason} {text}");
            }
        }
        return session.StateHash;
    }

    public OnlineActionCommand? BuildFirstLegalNormalMoveCommand(string roomId, string tableId, int actorSide)
    {
        lock (_gate)
        {
            return TryGetTable(roomId, tableId, out _, out var table) && table.Session != null
                ? table.Session.FirstLegalNormalMoveCommand(actorSide)
                : null;
        }
    }

    public OnlineActionCommand? BuildFirstAiCandidateCommand(string roomId, string tableId, string preferredKind = "")
    {
        lock (_gate)
        {
            return TryGetTable(roomId, tableId, out _, out var table) && table.Session != null
                ? table.Session.FirstAiCandidateCommand(preferredKind)
                : null;
        }
    }

    private OnlineProtocolMessage Reject(OnlineMessageEnvelope request, string reasonCode, string reasonText, string stateHash = "", long serverSeq = 0)
    {
        _diagnostics.LastRejectReason = reasonCode;
        _diagnostics.ProtocolErrorCount++;
        return Reply(OnlineMessageTypes.ActionRejected, request, error: OnlineProtocolJson.Error(reasonCode, reasonText, stateHash, serverSeq));
    }

    private static OnlineProtocolMessage Reply(
        string messageType,
        OnlineMessageEnvelope request,
        OnlineRoomCommand? room = null,
        OnlineTableCommand? table = null,
        OnlineActionCommand? action = null,
        OnlineSnapshot? snapshot = null,
        OnlineActionLogChunk? actionLog = null,
        OnlineError? error = null,
        string text = "")
    {
        return new OnlineProtocolMessage
        {
            Envelope = new OnlineMessageEnvelope
            {
                MessageType = messageType,
                MessageId = Guid.NewGuid().ToString("N"),
                CorrelationId = request.MessageId,
                RoomId = request.RoomId,
                TableId = request.TableId,
                ClientId = "server",
                PlayerId = request.PlayerId,
                ServerSeq = snapshot?.ServerSeq ?? actionLog?.ToServerSeq ?? error?.ServerSeq ?? 0,
                SentAtUtc = DateTime.UtcNow.ToString("O")
            },
            Room = room,
            Table = table,
            Action = action,
            Snapshot = snapshot,
            ActionLog = actionLog,
            Error = error,
            Text = text
        };
    }

    private bool TryGetRoom(string roomId, out OnlineRoom room)
    {
        room = null!;
        return !string.IsNullOrWhiteSpace(roomId) && _rooms.TryGetValue(roomId, out room!);
    }

    private bool TryGetTable(string roomId, string tableId, out OnlineRoom room, out OnlineTable table)
    {
        table = null!;
        return TryGetRoom(roomId, out room) &&
            !string.IsNullOrWhiteSpace(tableId) &&
            room.Tables.TryGetValue(tableId, out table!);
    }

    private bool TrySeat(OnlineMessageEnvelope envelope, out OnlineRoom room, out OnlineTable table, out OnlineSeat seat)
    {
        seat = null!;
        if (!TryGetTable(envelope.RoomId, envelope.TableId, out room, out table))
        {
            return false;
        }
        var playerId = RequirePlayerId(envelope);
        seat = table.Seats.Values.FirstOrDefault(s => string.Equals(s.PlayerId, playerId, StringComparison.OrdinalIgnoreCase))!;
        return seat != null;
    }

    private static string RequirePlayerId(OnlineMessageEnvelope envelope)
    {
        return string.IsNullOrWhiteSpace(envelope.PlayerId) ? envelope.ClientId : envelope.PlayerId;
    }

    private static bool ActorMatchesSeat(OnlineTable table, OnlineSeat seat, OnlineActionCommand command)
    {
        if (table.IsHodge)
        {
            var macro = command.MacroPlayer != 0 ? command.MacroPlayer : command.ActorSide;
            return macro == seat.MacroPlayer;
        }

        var actor = command.ActorSide != 0 ? command.ActorSide : command.Side;
        return actor == 0 || actor == seat.SideId;
    }

    private static OnlineActionCommand CloneCommand(OnlineActionCommand command)
    {
        var json = JsonSerializer.Serialize(command, OnlineProtocolJson.Options);
        return JsonSerializer.Deserialize<OnlineActionCommand>(json, OnlineProtocolJson.Options) ?? new OnlineActionCommand();
    }

    private static OnlineActionEvent CloneEvent(OnlineActionEvent actionEvent)
    {
        var json = JsonSerializer.Serialize(actionEvent, OnlineProtocolJson.Options);
        return JsonSerializer.Deserialize<OnlineActionEvent>(json, OnlineProtocolJson.Options) ?? new OnlineActionEvent();
    }
}

public sealed class OnlineRoom
{
    public string RoomId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int MaxTables { get; set; } = 8;
    public OnlineRoomState State { get; set; } = OnlineRoomState.Open;
    public DateTime CreatedAtUtc { get; set; }
    public Dictionary<string, OnlinePlayer> Players { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, OnlineTable> Tables { get; } = new(StringComparer.OrdinalIgnoreCase);

    public OnlineRoom CloneShallow()
    {
        return new OnlineRoom
        {
            RoomId = RoomId,
            DisplayName = DisplayName,
            MaxTables = MaxTables,
            State = State,
            CreatedAtUtc = CreatedAtUtc
        };
    }
}

public sealed class OnlineTable
{
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public string RulesetId { get; set; } = "";
    public string ProfileFileName { get; set; } = "";
    public int SeatCount { get; set; }
    public OnlineTableState State { get; set; } = OnlineTableState.WaitingForPlayers;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public long ServerSeq { get; set; }
    public string LastStateHash { get; set; } = "";
    public Dictionary<int, OnlineSeat> Seats { get; } = new();
    public List<OnlineActionEvent> ActionLog { get; } = new();
    public OnlineGameSession? Session { get; set; }
    public bool IsHodge => RulesetId.Contains("hodge-projection-duel", StringComparison.OrdinalIgnoreCase);
}

public sealed class OnlineSeat
{
    public int SeatIndex { get; set; }
    public int SideId { get; set; }
    public int MacroPlayer { get; set; }
    public string PlayerId { get; set; } = "";
    public bool IsReady { get; set; }
    public bool IsConnected { get; set; }
    public DateTime LastSeenUtc { get; set; }
}

public sealed class OnlinePlayer
{
    public string PlayerId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsConnected { get; set; }
    public DateTime LastSeenUtc { get; set; }
}
