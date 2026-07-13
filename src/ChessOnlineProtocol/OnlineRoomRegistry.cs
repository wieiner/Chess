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
    private readonly IChessOnlineGameSessionFactory _sessionFactory;
    private readonly TimeProvider _timeProvider;
    private readonly OnlineDiagnostics _diagnostics = new();

    public OnlineRoomRegistry(
        string profileRoot,
        IChessOnlineGameSessionFactory? sessionFactory = null,
        TimeProvider? timeProvider = null)
    {
        _profileRoot = profileRoot;
        _sessionFactory = sessionFactory ?? new NativeChessOnlineGameSessionFactory();
        _timeProvider = timeProvider ?? TimeProvider.System;
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
                CreatedAtUtc = UtcNow(),
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
                LastSeenUtc = UtcNow()
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
                CreatedAtUtc = UtcNow(),
                LastActivityUtc = UtcNow()
            };
            table.Session = _sessionFactory.Create(profile, _profileRoot);
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
                LastSeenUtc = UtcNow()
            };
            table.LastActivityUtc = UtcNow();
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
            table.LastActivityUtc = UtcNow();
            return Reply(OnlineMessageTypes.TableState, envelope, table: new OnlineTableCommand
            {
                TableId = table.TableId,
                RulesetId = table.RulesetId,
                SeatIndex = seat.SeatIndex,
                Ready = seat.IsReady
            });
        }
    }

    public bool SetPlayerConnectionState(string roomId, string tableId, string playerId, bool connected)
    {
        lock (_gate)
        {
            if (!TryGetTable(roomId, tableId, out var room, out var table))
            {
                return false;
            }

            var now = UtcNow();
            var seat = table.Seats.Values.FirstOrDefault(candidate =>
                string.Equals(candidate.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
            if (seat == null)
            {
                return false;
            }

            seat.IsConnected = connected;
            seat.LastSeenUtc = now;
            table.LastActivityUtc = now;
            if (connected && table.Seats.Values.All(candidate => candidate.IsConnected))
            {
                table.DisconnectedUtc = null;
            }
            else if (!connected && table.State == OnlineTableState.InGame)
            {
                table.DisconnectedUtc ??= now;
            }
            if (room.Players.TryGetValue(playerId, out var player))
            {
                player.IsConnected = connected;
                player.LastSeenUtc = now;
            }
            return true;
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

            table.Session = _sessionFactory.Create(RuleProfileCatalog.ResolveRequired(_profileRoot, table.RulesetId), _profileRoot);
            table.State = OnlineTableState.InGame;
            table.StartedAtUtc = UtcNow();
            table.LastActivityUtc = table.StartedAtUtc.Value;
            table.DisconnectedUtc = null;
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
                _diagnostics.ResyncCount++;
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
                CreatedAtUtc = UtcNow().ToString("O")
            };
            table.ActionLog.Add(actionEvent);
            table.LastActivityUtc = UtcNow();
            _diagnostics.LastServerSeq = table.ServerSeq;
            _diagnostics.LastAcceptedAction = actionEvent.Notation;
            _diagnostics.LastStateHash = table.LastStateHash;
            _diagnostics.ActionLogLength = table.ActionLog.Count;
            _diagnostics.AcceptedActionCount++;
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

    public OnlineProtocolMessage RequestResumeMatch(OnlineMessageEnvelope envelope, OnlineResumeRequest request)
    {
        lock (_gate)
        {
            request.RoomId = string.IsNullOrWhiteSpace(request.RoomId) ? envelope.RoomId : request.RoomId.Trim();
            request.TableId = string.IsNullOrWhiteSpace(request.TableId) ? envelope.TableId : request.TableId.Trim();
            request.PlayerId = string.IsNullOrWhiteSpace(request.PlayerId) ? RequirePlayerId(envelope) : request.PlayerId.Trim();

            if (!TryGetTable(request.RoomId, request.TableId, out _, out var table))
            {
                return ResumeFailure(envelope, request, OnlineResumeFailureReasons.TableNotFound, "Table not found in active runtime registry.");
            }

            var seat = request.SeatIndex > 0 && table.Seats.TryGetValue(request.SeatIndex, out var requestedSeat)
                ? requestedSeat
                : table.Seats.Values.FirstOrDefault(s => string.Equals(s.PlayerId, request.PlayerId, StringComparison.OrdinalIgnoreCase));
            if (seat == null || !string.Equals(seat.PlayerId, request.PlayerId, StringComparison.OrdinalIgnoreCase))
            {
                return ResumeFailure(envelope, request, OnlineResumeFailureReasons.PlayerNotInTable, "Player is not seated at this table.");
            }

            if (!string.IsNullOrWhiteSpace(request.ExpectedRulesetId) &&
                !string.Equals(request.ExpectedRulesetId, table.RulesetId, StringComparison.OrdinalIgnoreCase))
            {
                return ResumeFailure(envelope, request, OnlineResumeFailureReasons.RulesetMismatch, "Requested ruleset does not match active table.");
            }

            if (table.State != OnlineTableState.InGame)
            {
                return ResumeFailure(envelope, request, OnlineResumeFailureReasons.TableNotActive, "Table is not in game.");
            }

            if (table.Session == null)
            {
                return ResumeFailure(envelope, request, OnlineResumeFailureReasons.CannotResumeAfterServerRestartYet, "Active native game session is not available.");
            }

            var snapshot = table.Session.CreateSnapshot(table.RoomId, table.TableId, table.ServerSeq);
            var fromSeq = Math.Max(1, request.LastKnownServerSeq + 1);
            var events = table.ActionLog
                .Where(e => e.ServerSeq >= fromSeq)
                .Take(64)
                .Select(CloneEvent)
                .ToList();
            var actionLog = new OnlineActionLogChunk
            {
                RoomId = table.RoomId,
                TableId = table.TableId,
                FromServerSeq = events.FirstOrDefault()?.ServerSeq ?? fromSeq,
                ToServerSeq = events.LastOrDefault()?.ServerSeq ?? fromSeq - 1,
                Events = events
            };
            return Reply(OnlineMessageTypes.ResumeMatchResult, envelope, snapshot: snapshot, actionLog: actionLog, resumeResult: new OnlineResumeResult
            {
                Success = true,
                RoomId = table.RoomId,
                TableId = table.TableId,
                SeatIndex = seat.SeatIndex,
                RulesetId = table.RulesetId,
                Snapshot = snapshot,
                ActionLog = actionLog
            });
        }
    }

    public OnlineProtocolMessage JoinSpectator(OnlineMessageEnvelope envelope, OnlineJoinSpectatorRequest request)
    {
        lock (_gate)
        {
            request.RoomId = string.IsNullOrWhiteSpace(request.RoomId) ? envelope.RoomId : request.RoomId.Trim();
            request.TableId = string.IsNullOrWhiteSpace(request.TableId) ? envelope.TableId : request.TableId.Trim();
            request.PlayerId = string.IsNullOrWhiteSpace(request.PlayerId) ? RequirePlayerId(envelope) : request.PlayerId.Trim();
            envelope.RoomId = request.RoomId;
            envelope.TableId = request.TableId;
            envelope.PlayerId = request.PlayerId;

            if (string.IsNullOrWhiteSpace(request.PlayerId))
            {
                return SpectatorFailure(envelope, request, OnlineSpectatorFailureReasons.NotAuthenticated, "Spectator requires an authenticated temporary user.");
            }

            if (!TryGetTable(request.RoomId, request.TableId, out _, out var table))
            {
                return SpectatorFailure(envelope, request, OnlineSpectatorFailureReasons.TableNotFound, "Table not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.ExpectedRulesetId) &&
                !string.Equals(request.ExpectedRulesetId, table.RulesetId, StringComparison.OrdinalIgnoreCase))
            {
                return SpectatorFailure(envelope, request, OnlineSpectatorFailureReasons.RulesetMismatch, "Requested ruleset does not match active table.");
            }

            if (table.State != OnlineTableState.InGame || table.Session == null)
            {
                return SpectatorFailure(envelope, request, OnlineSpectatorFailureReasons.TableNotActive, "Table is not in game.");
            }

            var snapshot = table.Session.CreateSnapshot(table.RoomId, table.TableId, table.ServerSeq);
            var fromSeq = Math.Max(1, request.LastKnownServerSeq + 1);
            var events = table.ActionLog
                .Where(e => e.ServerSeq >= fromSeq)
                .Take(64)
                .Select(CloneEvent)
                .ToList();
            var actionLog = new OnlineActionLogChunk
            {
                RoomId = table.RoomId,
                TableId = table.TableId,
                FromServerSeq = events.FirstOrDefault()?.ServerSeq ?? fromSeq,
                ToServerSeq = events.LastOrDefault()?.ServerSeq ?? fromSeq - 1,
                Events = events
            };
            var state = new OnlineSpectatorState
            {
                IsSpectator = true,
                RoomId = table.RoomId,
                TableId = table.TableId,
                RulesetId = table.RulesetId,
                SpectatorId = $"spectator-{request.PlayerId}",
                ViewerPlayerId = request.PlayerId,
                LastKnownServerSeq = table.ServerSeq,
                SubmitDisabledReason = "Spectator mode is read-only."
            };
            return Reply(OnlineMessageTypes.JoinSpectatorResult, envelope, snapshot: snapshot, actionLog: actionLog, spectatorResult: new OnlineJoinSpectatorResult
            {
                Success = true,
                RoomId = table.RoomId,
                TableId = table.TableId,
                RulesetId = table.RulesetId,
                SpectatorId = state.SpectatorId,
                State = state,
                Snapshot = snapshot,
                ActionLog = actionLog
            });
        }
    }

    public OnlineProtocolMessage RequestLegalPreview(OnlineMessageEnvelope envelope, OnlineLegalPreviewRequest request)
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

            request.PlayerId = string.IsNullOrWhiteSpace(request.PlayerId) ? RequirePlayerId(envelope) : request.PlayerId;
            request.RoomId = string.IsNullOrWhiteSpace(request.RoomId) ? table.RoomId : request.RoomId;
            request.TableId = string.IsNullOrWhiteSpace(request.TableId) ? table.TableId : request.TableId;
            if (request.ActorSide == 0)
            {
                request.ActorSide = table.IsHodge ? seat.MacroPlayer : seat.SideId;
            }
            if (request.MacroPlayer == 0 && table.IsHodge)
            {
                request.MacroPlayer = seat.MacroPlayer;
            }

            if (!ActorMatchesSeat(table, seat, new OnlineActionCommand
            {
                ActorSide = request.ActorSide,
                MacroPlayer = request.MacroPlayer,
                Side = request.ActorSide
            }))
            {
                return Reject(envelope, OnlineRejectReasons.WrongActor, "Player does not own the requested preview actor.", table.Session.StateHash, table.ServerSeq);
            }

            var beforeHash = table.Session.StateHash;
            if (!string.IsNullOrWhiteSpace(request.ExpectedStateHash) &&
                !string.Equals(request.ExpectedStateHash, beforeHash, StringComparison.Ordinal))
            {
                _diagnostics.ResyncCount++;
                return Reply(OnlineMessageTypes.LegalPreviewResult, envelope, legalPreview: new OnlineLegalPreviewResult
                {
                    RoomId = table.RoomId,
                    TableId = table.TableId,
                    RulesetId = table.RulesetId,
                    StateHash = beforeHash,
                    ServerSeq = table.ServerSeq,
                    SourceX = request.SourceX,
                    SourceY = request.SourceY,
                    SourceZ = request.SourceZ,
                    ActorSide = request.ActorSide,
                    MacroPlayer = request.MacroPlayer,
                    IsStale = true,
                    NoLegalActionReason = "Client expected hash does not match authoritative state.",
                    Error = new OnlineLegalPreviewError
                    {
                        ReasonCode = OnlineRejectReasons.StaleStateHash,
                        ReasonText = "Client expected hash does not match authoritative state.",
                        RequiresResync = true
                    }
                });
            }

            var preview = table.Session.BuildLegalPreview(request, table.RoomId, table.TableId, table.ServerSeq);
            if (!string.Equals(table.Session.StateHash, beforeHash, StringComparison.Ordinal))
            {
                preview.Error = new OnlineLegalPreviewError
                {
                    ReasonCode = OnlineRejectReasons.InternalError,
                    ReasonText = "Legal preview mutated authoritative state.",
                    RequiresResync = true
                };
                preview.Options.Clear();
                preview.NoLegalActionReason = preview.Error.ReasonText;
            }
            return Reply(OnlineMessageTypes.LegalPreviewResult, envelope, legalPreview: preview);
        }
    }

    public OnlineDiagnostics GetDiagnostics()
    {
        lock (_gate)
        {
            return new OnlineDiagnostics
            {
                ProtocolVersion = OnlineProtocolVersion.ProtocolVersion,
                RequestLegalPreviewSupported = true,
                RealtimeResyncSupported = true,
                ActionLogSupported = true,
                MatchmakingSupported = true,
                ResumeMatchSupported = true,
                SpectatorModeSupported = true,
                LobbySnapshotSupported = true,
                SupportedHubMethods =
                [
                    OnlineMessageTypes.Hello,
                    OnlineMessageTypes.JoinMatchmaking,
                    OnlineMessageTypes.CancelMatchmaking,
                    OnlineMessageTypes.GetMatchmakingStatus,
                    OnlineMessageTypes.Ready,
                    OnlineMessageTypes.StartGame,
                    OnlineMessageTypes.SubmitAction,
                    OnlineMessageTypes.RequestSnapshot,
                    OnlineMessageTypes.RequestActionLog,
                    OnlineMessageTypes.RequestResumeMatch,
                    OnlineMessageTypes.JoinSpectator,
                    OnlineMessageTypes.RequestLobbySnapshot,
                    OnlineMessageTypes.RequestLegalPreview,
                    OnlineMessageTypes.RequestDiagnostics,
                    OnlineMessageTypes.Ping
                ],
                RoomCount = _rooms.Count,
                TableCount = _rooms.Values.Sum(r => r.Tables.Count),
                ConnectionCount = _rooms.Values.Sum(r => r.Players.Count),
                ActiveConnectionCount = _diagnostics.ActiveConnectionCount,
                LastServerSeq = _diagnostics.LastServerSeq,
                LastAcceptedAction = _diagnostics.LastAcceptedAction,
                LastRejectReason = _diagnostics.LastRejectReason,
                LastStateHash = _diagnostics.LastStateHash,
                LastSnapshotBytes = _diagnostics.LastSnapshotBytes,
                ActionLogLength = _diagnostics.ActionLogLength,
                ProtocolErrorCount = _diagnostics.ProtocolErrorCount,
                AcceptedActionCount = _diagnostics.AcceptedActionCount,
                RejectedActionCount = _diagnostics.RejectedActionCount,
                ResyncCount = _diagnostics.ResyncCount,
                ActiveTableCount = _rooms.Values.Sum(room => room.Tables.Values.Count(table =>
                    table.State == OnlineTableState.InGame && table.Seats.Values.All(seat => seat.IsConnected))),
                ResumableTableCount = _rooms.Values.Sum(room => room.Tables.Values.Count(table =>
                    table.State == OnlineTableState.InGame && table.Seats.Values.Any(seat => !seat.IsConnected))),
                CompletedTableCount = _rooms.Values.Sum(room => room.Tables.Values.Count(table => table.State == OnlineTableState.Finished)),
                ExpiredTableCount = _diagnostics.ExpiredTableCount,
                CleanupRunCount = _diagnostics.CleanupRunCount,
                LastCleanupUtc = _diagnostics.LastCleanupUtc,
                LastCleanupRemovedCount = _diagnostics.LastCleanupRemovedCount
            };
        }
    }

    public bool ContainsTable(string roomId, string tableId)
    {
        lock (_gate)
        {
            return TryGetTable(roomId, tableId, out _, out _);
        }
    }

    public OnlineRoomCleanupResult RunCleanup(OnlineRoomCleanupPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        lock (_gate)
        {
            var classified = 0;
            var removed = 0;
            foreach (var room in _rooms.Values.ToArray())
            {
                foreach (var table in room.Tables.Values.ToArray())
                {
                    if (removed >= Math.Max(1, policy.MaxRemovals))
                    {
                        break;
                    }

                    if (table.State == OnlineTableState.InGame || table.Seats.Values.Any(seat => seat.IsConnected))
                    {
                        continue;
                    }

                    var lastActivity = table.LastActivityUtc == default ? table.CreatedAtUtc : table.LastActivityUtc;
                    if (table.State is OnlineTableState.WaitingForPlayers or OnlineTableState.ReadyCheck)
                    {
                        if (policy.NowUtc - lastActivity >= policy.WaitingIdle)
                        {
                            table.State = OnlineTableState.Abandoned;
                            table.LastActivityUtc = policy.NowUtc;
                            table.ExpiresUtc = policy.NowUtc + policy.AbandonedRetention;
                            classified++;
                        }
                        continue;
                    }

                    var malformedOrphan = table.Session == null && table.Seats.Count == 0 && table.ActionLog.Count == 0 &&
                        (string.IsNullOrWhiteSpace(table.RulesetId) || string.IsNullOrWhiteSpace(table.ProfileFileName));
                    var eligible = table.State == OnlineTableState.Finished
                        ? policy.NowUtc - (table.CompletedUtc ?? lastActivity) >= policy.CompletedRetention
                        : table.State == OnlineTableState.Abandoned
                            ? policy.NowUtc >= (table.ExpiresUtc ?? lastActivity + policy.AbandonedRetention)
                            : malformedOrphan && policy.NowUtc - lastActivity >= policy.MalformedOrphanGrace;
                    if (!eligible)
                    {
                        continue;
                    }

                    room.Tables.Remove(table.TableId);
                    table.Session?.Dispose();
                    removed++;
                }
            }

            _diagnostics.CleanupRunCount++;
            _diagnostics.LastCleanupUtc = policy.NowUtc.ToString("O");
            _diagnostics.LastCleanupRemovedCount = removed;
            _diagnostics.ExpiredTableCount += removed;
            return new OnlineRoomCleanupResult(classified, removed);
        }
    }

    public OnlineProtocolMessage RequestLobbySnapshot(OnlineMessageEnvelope envelope, OnlineLobbySnapshotRequest request)
    {
        lock (_gate)
        {
            var snapshot = BuildLobbySnapshot(request);
            return Reply(OnlineMessageTypes.LobbySnapshot, envelope, lobbySnapshot: snapshot);
        }
    }

    private OnlineLobbySnapshot BuildLobbySnapshot(OnlineLobbySnapshotRequest request)
    {
        var rulesetFilter = request.RulesetIdFilter.Trim();
        var rows = new List<OnlineLobbyTableRow>();
        foreach (var room in _rooms.Values.OrderBy(r => r.RoomId, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var table in room.Tables.Values.OrderBy(t => t.TableId, StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(rulesetFilter) &&
                    !string.Equals(table.RulesetId, rulesetFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!ShouldIncludeLobbyTable(table, request))
                {
                    continue;
                }

                rows.Add(new OnlineLobbyTableRow
                {
                    RoomId = room.RoomId,
                    TableId = table.TableId,
                    RulesetId = table.RulesetId,
                    TableState = table.State.ToString(),
                    SeatsOccupied = table.Seats.Count,
                    MaxSeats = table.SeatCount,
                    SpectatorCount = 0,
                    Started = table.State == OnlineTableState.InGame || table.StartedAtUtc.HasValue,
                    LastServerSeq = table.ServerSeq,
                    CreatedUtc = table.CreatedAtUtc.ToString("O"),
                    UpdatedUtc = LobbyUpdatedUtc(table),
                    SeatSummaries = table.Seats.Values
                        .OrderBy(s => s.SeatIndex)
                        .Select(s => new OnlineLobbySeatSummary
                        {
                            SeatIndex = s.SeatIndex,
                            SideId = s.SideId,
                            MacroPlayer = s.MacroPlayer,
                            Ready = s.IsReady,
                            Connected = s.IsConnected,
                            PlayerLabel = ShortPlayerLabel(s.PlayerId)
                        })
                        .ToList()
                });
            }
        }

        return new OnlineLobbySnapshot
        {
            CreatedUtc = UtcNow().ToString("O"),
            ServerSeq = _diagnostics.LastServerSeq,
            RoomCount = _rooms.Count,
            TableCount = _rooms.Values.Sum(r => r.Tables.Count),
            ActiveTableCount = rows.Count,
            WarningText = "Player labels are shortened; spectator counts are live process-local distinct-viewer counts.",
            Tables = rows
        };
    }

    private static bool ShouldIncludeLobbyTable(OnlineTable table, OnlineLobbySnapshotRequest request)
    {
        return table.State switch
        {
            OnlineTableState.WaitingForPlayers or OnlineTableState.ReadyCheck => request.IncludeWaitingTables,
            OnlineTableState.InGame => request.IncludeInGameTables,
            OnlineTableState.Finished or OnlineTableState.Abandoned => request.IncludeFinishedTables,
            _ => false
        };
    }

    private static string LobbyUpdatedUtc(OnlineTable table)
    {
        var lastAction = table.ActionLog.LastOrDefault();
        if (!string.IsNullOrWhiteSpace(lastAction?.CreatedAtUtc))
        {
            return lastAction.CreatedAtUtc;
        }
        return (table.StartedAtUtc ?? table.CreatedAtUtc).ToString("O");
    }

    private static string ShortPlayerLabel(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            return "";
        }
        var trimmed = playerId.Trim();
        return trimmed.Length <= 8 ? trimmed : trimmed[..8];
    }

    public OnlineAuthorityRuntimeDiagnostics GetAuthorityDiagnostics()
    {
        return _sessionFactory.GetDiagnostics();
    }

    public bool ProbeAuthorityReady()
    {
        var profile = RuleProfileCatalog.ResolveRequired(_profileRoot, RuleProfileCatalog.All[0].RulesetId);
        using var session = _sessionFactory.Create(profile, _profileRoot);
        return !string.IsNullOrWhiteSpace(session.StateHash);
    }

    public void SetActiveConnectionCount(int count)
    {
        lock (_gate)
        {
            _diagnostics.ActiveConnectionCount = Math.Max(0, count);
        }
    }

    public static string HashFromSaveGameJson(string saveGameJson)
    {
        return NativeChessOnlineGameSessionFactory.HashFromSaveGameJson(saveGameJson);
    }

    public string ReplayActionLogToHash(string rulesetId, IEnumerable<OnlineActionEvent> events)
    {
        var profile = RuleProfileCatalog.ResolveRequired(_profileRoot, rulesetId);
        using var session = _sessionFactory.Create(profile, _profileRoot);
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
        _diagnostics.RejectedActionCount++;
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
        OnlineResumeResult? resumeResult = null,
        OnlineJoinSpectatorResult? spectatorResult = null,
        OnlineLobbySnapshot? lobbySnapshot = null,
        OnlineLegalPreviewResult? legalPreview = null,
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
                ServerSeq = snapshot?.ServerSeq ?? actionLog?.ToServerSeq ?? lobbySnapshot?.ServerSeq ?? legalPreview?.ServerSeq ?? error?.ServerSeq ?? 0,
                SentAtUtc = DateTime.UtcNow.ToString("O")
            },
            Room = room,
            Table = table,
            Action = action,
            Snapshot = snapshot,
            ActionLog = actionLog,
            ResumeResult = resumeResult,
            SpectatorResult = spectatorResult,
            LobbySnapshot = lobbySnapshot,
            LegalPreview = legalPreview,
            Error = error,
            Text = text
        };
    }

    private OnlineProtocolMessage ResumeFailure(OnlineMessageEnvelope envelope, OnlineResumeRequest request, string reason, string text)
    {
        return Reply(OnlineMessageTypes.ResumeMatchResult, envelope, resumeResult: new OnlineResumeResult
        {
            Success = false,
            FailureReason = reason,
            FailureText = text,
            RoomId = request.RoomId,
            TableId = request.TableId,
            SeatIndex = request.SeatIndex,
            RulesetId = request.ExpectedRulesetId
        });
    }

    private OnlineProtocolMessage SpectatorFailure(OnlineMessageEnvelope envelope, OnlineJoinSpectatorRequest request, string reason, string text)
    {
        return Reply(OnlineMessageTypes.JoinSpectatorResult, envelope, spectatorResult: new OnlineJoinSpectatorResult
        {
            Success = false,
            FailureReason = reason,
            FailureText = text,
            RoomId = request.RoomId,
            TableId = request.TableId,
            RulesetId = request.ExpectedRulesetId,
            State = new OnlineSpectatorState
            {
                RoomId = request.RoomId,
                TableId = request.TableId,
                RulesetId = request.ExpectedRulesetId,
                ViewerPlayerId = request.PlayerId,
                SubmitDisabledReason = "Spectator mode is read-only."
            }
        });
    }

    private bool TryGetRoom(string roomId, out OnlineRoom room)
    {
        room = null!;
        return !string.IsNullOrWhiteSpace(roomId) && _rooms.TryGetValue(roomId, out room!);
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

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
    public DateTime LastActivityUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public DateTime? DisconnectedUtc { get; set; }
    public DateTime? ExpiresUtc { get; set; }
    public long ServerSeq { get; set; }
    public string LastStateHash { get; set; } = "";
    public Dictionary<int, OnlineSeat> Seats { get; } = new();
    public List<OnlineActionEvent> ActionLog { get; } = new();
    public IChessOnlineRulesAuthority? Session { get; set; }
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

public sealed class OnlineRoomCleanupPolicy
{
    public DateTime NowUtc { get; init; }
    public int MaxRemovals { get; init; } = 32;
    public TimeSpan WaitingIdle { get; init; } = TimeSpan.FromHours(6);
    public TimeSpan CompletedRetention { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan AbandonedRetention { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan MalformedOrphanGrace { get; init; } = TimeSpan.FromHours(1);
}

public sealed record OnlineRoomCleanupResult(
    int ClassifiedAbandonedCount,
    int RemovedTableCount);
