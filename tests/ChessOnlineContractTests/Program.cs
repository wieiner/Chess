using System.Text.Json;
using ChessOnlineProtocol;

var test = new ContractTest("ChessOnlineContractTests");

try
{
    var root = FindRepoRoot();
    var profileRoot = Path.Combine(root, "assets", "rules", "profiles");
    var onlineFixtureRoot = Path.Combine(root, "assets", "rules", "scenarios", "chess3d", "online");

    test.Check(Directory.Exists(profileRoot), "profile root exists");
    test.Check(RuleProfileCatalog.All.Count == 5, "exactly five Chess3D RuleProfiles are in online catalog");

    ProtocolRoundtripTests(test);
    AuthorityRuntimeDiagnosticsTests(test, profileRoot);
    AuthorityClassicTests(test, profileRoot);
    AuthorityProfileSmokeTests(test, profileRoot);
    SnapshotAndReplayTests(test, profileRoot);
    FixtureParseTests(test, onlineFixtureRoot);

    return test.Finish();
}
catch (Exception ex)
{
    test.Fail($"Unhandled exception: {ex}");
    return test.Finish();
}

static void AuthorityRuntimeDiagnosticsTests(ContractTest test, string profileRoot)
{
    var registry = new OnlineRoomRegistry(profileRoot);
    var diagnostics = registry.GetAuthorityDiagnostics();
    test.Check(diagnostics.RuntimeKind == AuthorityRuntimeKind.WindowsNative, "online authority runtime reports WindowsNative on Windows build");
    test.Check(diagnostics.IsPortableRuntime, "online authority runtime uses portable native resolver boundary");
    test.Check(diagnostics.NativeLibraryName == "Chess3DEngine.dll", "online authority diagnostics expose Windows native library name");
    test.Check(NativeChessOnlineGameSessionFactory.GetExpectedNativeLibraryNameForPlatform("Linux") == "libChess3DEngine.so", "online authority computes Linux native library name");
    var linuxPath = NativeChessOnlineGameSessionFactory.GetExpectedNativeLibraryPathForPlatform("Linux", "/opt/chessonline");
    test.Check(linuxPath.Replace('\\', '/').EndsWith("/opt/chessonline/libChess3DEngine.so", StringComparison.Ordinal), "online authority computes Linux native library path");
    test.Check(NativeChessOnlineGameSessionFactory.GetExpectedNativeLibraryNameForPlatform("Windows") == "Chess3DEngine.dll", "online authority keeps Windows native library name");
}

static void ProtocolRoundtripTests(ContractTest test)
{
    var hello = OnlineProtocolJson.Wrap(OnlineMessageTypes.Hello, "client-a", "player-a");
    var json = OnlineProtocolJson.Serialize(hello);
    test.Check(OnlineProtocolJson.TryDeserialize(json, out var parsed, out var error), "Hello message roundtrips");
    test.Check(parsed.Envelope.MessageType == OnlineMessageTypes.Hello, "Hello message type survives roundtrip");

    var withFuture = json.Insert(json.LastIndexOf('}'), ",\"futureField\":\"ignored\"");
    test.Check(OnlineProtocolJson.TryDeserialize(withFuture, out _, out _), "unknown future field is tolerated");

    var badType = json.Replace(OnlineMessageTypes.Hello, "FutureUnknownType");
    test.Check(!OnlineProtocolJson.TryDeserialize(badType, out _, out error) &&
        error.ReasonCode == OnlineRejectReasons.UnknownMessageType, "unknown message type is rejected");

    var badProtocol = json.Replace(OnlineProtocolVersion.ProtocolId, "wrong.protocol");
    test.Check(!OnlineProtocolJson.TryDeserialize(badProtocol, out _, out error) &&
        error.ReasonCode == OnlineRejectReasons.WrongProtocol, "wrong protocol id is rejected");

    var oversized = new string('x', OnlineProtocolVersion.MaxMessageBytes + 1);
    test.Check(!OnlineProtocolJson.TryDeserialize(oversized, out _, out error) &&
        error.ReasonCode == OnlineRejectReasons.OversizedMessage, "oversized message is rejected");
}

static void AuthorityClassicTests(ContractTest test, string profileRoot)
{
    var registry = new OnlineRoomRegistry(profileRoot);
    var env = Envelope("CreateRoom", "room-classic", "classic", "client-1", "player-1");

    test.Check(registry.Hello(Envelope(OnlineMessageTypes.Hello, "", "", "client-1", "player-1")).Envelope.MessageType == OnlineMessageTypes.Welcome,
        "Hello returns Welcome");
    test.Check(registry.CreateRoom(env, new OnlineRoomCommand { RoomId = "room-classic", DisplayName = "Classic Room" }).Envelope.MessageType == OnlineMessageTypes.RoomCreated,
        "create room succeeds");
    test.Check(registry.JoinRoom(Envelope(OnlineMessageTypes.JoinRoom, "room-classic", "", "client-1", "player-1")).Envelope.MessageType == OnlineMessageTypes.RoomJoined,
        "join room succeeds");
    test.Check(registry.CreateTable(Envelope(OnlineMessageTypes.CreateTable, "room-classic", "", "client-1", "player-1"),
            new OnlineTableCommand { TableId = "classic", RulesetId = "classic-six-side-3d-8x8x8-v0.1" }).Envelope.MessageType == OnlineMessageTypes.TableCreated,
        "create classic table succeeds");
    test.Check(registry.JoinTableSeat(Envelope(OnlineMessageTypes.JoinTableSeat, "room-classic", "classic", "client-1", "player-1"),
            new OnlineTableCommand { SeatIndex = 1 }).Envelope.MessageType == OnlineMessageTypes.SeatAssigned,
        "join seat succeeds");
    test.Check(registry.JoinTableSeat(Envelope(OnlineMessageTypes.JoinTableSeat, "room-classic", "classic", "client-2", "player-2"),
            new OnlineTableCommand { SeatIndex = 1 }).Error?.ReasonCode == OnlineRejectReasons.SeatOccupied,
        "duplicate seat is rejected");
    test.Check(registry.Ready(Envelope(OnlineMessageTypes.Ready, "room-classic", "classic", "client-1", "player-1"),
            new OnlineTableCommand { Ready = true }).Envelope.MessageType == OnlineMessageTypes.TableState,
        "ready succeeds");
    var start = registry.StartGame(Envelope(OnlineMessageTypes.StartGame, "room-classic", "classic", "client-1", "player-1"));
    test.Check(start.Envelope.MessageType == OnlineMessageTypes.GameStarted &&
        !string.IsNullOrWhiteSpace(start.Snapshot?.StateHash), "start emits authoritative snapshot");

    var wrong = registry.SubmitAction(Envelope(OnlineMessageTypes.SubmitAction, "room-classic", "classic", "client-2", "player-2"),
        new OnlineActionCommand
        {
            ActionKind = OnlineActionKinds.NormalMove,
            ActorSide = 2,
            FromX = 3,
            FromY = 3,
            FromZ = 0,
            ToX = 3,
            ToY = 3,
            ToZ = 1
        });
    test.Check(wrong.Error?.ReasonCode == OnlineRejectReasons.PlayerNotSeated ||
        wrong.Error?.ReasonCode == OnlineRejectReasons.WrongActor, "wrong player/actor is rejected");

    var legalMove = registry.BuildFirstLegalNormalMoveCommand("room-classic", "classic", 1);
    test.Check(legalMove != null, "online authority can build first legal Classic move command");
    var staleCommand = Clone(legalMove!);
    staleCommand.ExpectedStateHashBefore = "stale";
    var stale = registry.SubmitAction(Envelope(OnlineMessageTypes.SubmitAction, "room-classic", "classic", "client-1", "player-1"), staleCommand);
    test.Check(stale.Envelope.MessageType == OnlineMessageTypes.ResyncRequired &&
        stale.Error?.ReasonCode == OnlineRejectReasons.StaleStateHash, "stale hash requests resync");

    var accepted = registry.SubmitAction(Envelope(OnlineMessageTypes.SubmitAction, "room-classic", "classic", "client-1", "player-1"), legalMove!);
    test.Check(accepted.Envelope.MessageType == OnlineMessageTypes.ActionAccepted &&
        accepted.ActionLog?.Events.Count == 1 &&
        !string.IsNullOrWhiteSpace(accepted.ActionLog.Events[0].StateHashAfter), "legal classic move is accepted and logged");

    var rejected = registry.SubmitAction(Envelope(OnlineMessageTypes.SubmitAction, "room-classic", "classic", "client-1", "player-1"),
        new OnlineActionCommand
        {
            ActionKind = OnlineActionKinds.NormalMove,
            ActorSide = 1,
            FromX = 7,
            FromY = 7,
            FromZ = 7,
            ToX = 7,
            ToY = 7,
            ToZ = 6
        });
    test.Check(rejected.Error?.ReasonCode == OnlineRejectReasons.IllegalAction ||
        rejected.Error?.ReasonCode == OnlineRejectReasons.WrongActor, "illegal or wrong-turn action is rejected");

    var snapshot = registry.RequestSnapshot(Envelope(OnlineMessageTypes.RequestSnapshot, "room-classic", "classic", "client-1", "player-1"));
    var acceptedEvent = accepted.ActionLog?.Events.FirstOrDefault();
    test.Check(snapshot.Envelope.MessageType == OnlineMessageTypes.AuthoritativeSnapshot &&
        acceptedEvent != null &&
        snapshot.Snapshot?.StateHash == acceptedEvent.StateHashAfter, "snapshot reflects authoritative accepted state");

    var log = registry.RequestActionLog(Envelope(OnlineMessageTypes.RequestActionLog, "room-classic", "classic", "client-1", "player-1"));
    test.Check(log.ActionLog?.Events.Count == 1 && log.ActionLog.Events[0].ServerSeq == 1, "action log chunk exposes serverSeq");
}

static void AuthorityProfileSmokeTests(ContractTest test, string profileRoot)
{
    SmokeStartProfile(test, profileRoot, "single-side-3d-8x8x8-v0.1", 1);
    SmokeStartProfile(test, profileRoot, "asgard-convergence-3d-8x8x8-v0.1", 1);
    AsgardProfileIsolationTests(test, profileRoot);

    var rubik = StartedRegistry(profileRoot, "room-rubik", "rubik", "rubik-convergence-3d-8x8x8-v0.1", 1, out var rubikEnv);
    var layer = rubik.SubmitAction(rubikEnv(OnlineMessageTypes.SubmitAction), new OnlineActionCommand
    {
        ActionKind = OnlineActionKinds.RubikLayerTurn,
        ActorSide = 1,
        Axis = 0,
        Layer = 0,
        QuarterTurns = 1
    });
    test.Check(layer.Envelope.MessageType == OnlineMessageTypes.ActionAccepted, "Rubik layer turn is accepted only under Rubik profile");

    var hodge = StartedRegistry(profileRoot, "room-hodge", "hodge", "hodge-projection-duel-3d-8x8x8-v0.1", 1, out var hodgeEnv);
    var hodgeCommand = hodge.BuildFirstAiCandidateCommand("room-hodge", "hodge", OnlineActionKinds.HodgeProjectedMove);
    test.Check(hodgeCommand != null, "online authority can build Hodge composite command from candidates");
    var hodgeAction = hodge.SubmitAction(hodgeEnv(OnlineMessageTypes.SubmitAction), hodgeCommand!);
    test.Check(hodgeAction.Envelope.MessageType == OnlineMessageTypes.ActionAccepted &&
        hodgeAction.ActionLog?.Events[0].Notation.Contains("HPD", StringComparison.OrdinalIgnoreCase) == true,
        "Hodge projected composite is authoritative all-or-nothing action");

    var classic = StartedRegistry(profileRoot, "room-classic-layer", "classic-layer", "classic-six-side-3d-8x8x8-v0.1", 1, out var classicEnv);
    var disabledLayer = classic.SubmitAction(classicEnv(OnlineMessageTypes.SubmitAction), new OnlineActionCommand
    {
        ActionKind = OnlineActionKinds.RubikLayerTurn,
        ActorSide = 1,
        Axis = 0,
        Layer = 0,
        QuarterTurns = 1
    });
    test.Check(disabledLayer.Error?.ReasonCode == OnlineRejectReasons.IllegalAction, "Classic rejects Rubik layer turn command");
}

static void AsgardProfileIsolationTests(ContractTest test, string profileRoot)
{
    var asgard = StartedRegistry(profileRoot, "room-asgard-isolation", "asgard-isolation", "asgard-convergence-3d-8x8x8-v0.1", 1, out var asgardEnv);
    var initial = asgard.RequestSnapshot(asgardEnv(OnlineMessageTypes.RequestSnapshot)).Snapshot?.StateHash ?? "";

    var rubikAction = asgard.SubmitAction(asgardEnv(OnlineMessageTypes.SubmitAction), new OnlineActionCommand
    {
        ActionKind = OnlineActionKinds.RubikLayerTurn,
        ActorSide = 1,
        Axis = 0,
        Layer = 0,
        QuarterTurns = 1,
        ExpectedStateHashBefore = initial
    });
    var afterRubikReject = asgard.RequestSnapshot(asgardEnv(OnlineMessageTypes.RequestSnapshot)).Snapshot?.StateHash ?? "";
    test.Check(rubikAction.Error?.ReasonCode == OnlineRejectReasons.IllegalAction &&
        afterRubikReject == initial, "Asgard rejects Rubik layer turn without mutating state");

    var hodgeAction = asgard.SubmitAction(asgardEnv(OnlineMessageTypes.SubmitAction), new OnlineActionCommand
    {
        ActionKind = OnlineActionKinds.HodgeProjectedMove,
        ActorSide = 1,
        MacroPlayer = 1,
        FromX = 3,
        FromY = 3,
        FromZ = 0,
        ToX = 3,
        ToY = 3,
        ToZ = 1,
        ExpectedStateHashBefore = initial
    });
    var afterHodgeReject = asgard.RequestSnapshot(asgardEnv(OnlineMessageTypes.RequestSnapshot)).Snapshot?.StateHash ?? "";
    test.Check(hodgeAction.Error?.ReasonCode == OnlineRejectReasons.IllegalAction &&
        afterHodgeReject == initial, "Asgard rejects Hodge projected move without mutating state");

    var aiCandidate = asgard.BuildFirstAiCandidateCommand("room-asgard-isolation", "asgard-isolation");
    test.Check(aiCandidate != null, "Asgard online authority exposes at least one profile-aware AI candidate");
}

static void SnapshotAndReplayTests(ContractTest test, string profileRoot)
{
    var registry = StartedRegistry(profileRoot, "room-replay", "replay", "classic-six-side-3d-8x8x8-v0.1", 1, out var env);
    var command = registry.BuildFirstLegalNormalMoveCommand("room-replay", "replay", 1);
    test.Check(command != null, "online authority can build replay normal move command");
    var accepted = registry.SubmitAction(env(OnlineMessageTypes.SubmitAction), command!);
    var snapshot = registry.RequestSnapshot(env(OnlineMessageTypes.RequestSnapshot));
    var loadedHash = OnlineRoomRegistry.HashFromSaveGameJson(snapshot.Snapshot?.SaveGameJson ?? "");
    test.Check(loadedHash == snapshot.Snapshot?.StateHash, "snapshot savegame loads to same state hash");

    var events = accepted.ActionLog?.Events ?? new List<OnlineActionEvent>();
    var replayHash = registry.ReplayActionLogToHash("classic-six-side-3d-8x8x8-v0.1", events);
    test.Check(replayHash == accepted.ActionLog?.Events[0].StateHashAfter, "online action log replay reaches same hash");
}

static void FixtureParseTests(ContractTest test, string fixtureRoot)
{
    var expected = new[]
    {
        "online_protocol_hello_v0_1.json",
        "online_room_create_join_v0_1.json",
        "online_table_classic_start_v0_1.json",
        "online_classic_move_accept_v0_1.json",
        "online_classic_wrong_actor_reject_v0_1.json",
        "online_stale_hash_resync_v0_1.json",
        "online_asgard_restore_smoke_v0_1.json",
        "online_rubik_layer_turn_smoke_v0_1.json",
        "online_hodge_composite_smoke_v0_1.json",
        "online_snapshot_roundtrip_v0_1.json",
        "online_action_log_replay_hash_v0_1.json",
        "online_reconnect_resync_v0_1.json",
        "online_malformed_message_reject_v0_1.json",
        "online_unknown_future_field_tolerated_v0_1.json"
    };

    foreach (var file in expected)
    {
        var path = Path.Combine(fixtureRoot, file);
        test.Check(File.Exists(path), $"online fixture exists: {file}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        test.Check(doc.RootElement.TryGetProperty("format", out var format) &&
            format.GetString() == "chess3d-online-regression", $"online fixture parses: {file}");
    }
}

static void SmokeStartProfile(ContractTest test, string profileRoot, string rulesetId, int seat)
{
    var registry = StartedRegistry(profileRoot, $"room-{seat}-{rulesetId}", $"table-{seat}", rulesetId, seat, out var env);
    var snapshot = registry.RequestSnapshot(env(OnlineMessageTypes.RequestSnapshot));
    test.Check(snapshot.Snapshot?.RulesetId == rulesetId, $"profile starts under online authority: {rulesetId}");
}

static OnlineRoomRegistry StartedRegistry(string profileRoot, string roomId, string tableId, string rulesetId, int seat, out Func<string, OnlineMessageEnvelope> envelope)
{
    var registry = new OnlineRoomRegistry(profileRoot);
    envelope = type => Envelope(type, roomId, tableId, $"client-{roomId}", $"player-{roomId}");
    registry.CreateRoom(envelope(OnlineMessageTypes.CreateRoom), new OnlineRoomCommand { RoomId = roomId, DisplayName = roomId });
    registry.JoinRoom(envelope(OnlineMessageTypes.JoinRoom));
    registry.CreateTable(envelope(OnlineMessageTypes.CreateTable), new OnlineTableCommand { TableId = tableId, RulesetId = rulesetId });
    registry.JoinTableSeat(envelope(OnlineMessageTypes.JoinTableSeat), new OnlineTableCommand { SeatIndex = seat });
    registry.Ready(envelope(OnlineMessageTypes.Ready), new OnlineTableCommand { Ready = true });
    registry.StartGame(envelope(OnlineMessageTypes.StartGame));
    return registry;
}

static OnlineMessageEnvelope Envelope(string messageType, string roomId, string tableId, string clientId, string playerId)
{
    return new OnlineMessageEnvelope
    {
        MessageType = messageType,
        MessageId = Guid.NewGuid().ToString("N"),
        RoomId = roomId,
        TableId = tableId,
        ClientId = clientId,
        PlayerId = playerId,
        ClientSeq = DateTime.UtcNow.Ticks,
        SentAtUtc = DateTime.UtcNow.ToString("O")
    };
}

static OnlineActionCommand Clone(OnlineActionCommand command)
{
    var json = JsonSerializer.Serialize(command, OnlineProtocolJson.Options);
    return JsonSerializer.Deserialize<OnlineActionCommand>(json, OnlineProtocolJson.Options) ?? new OnlineActionCommand();
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "assets", "rules", "profiles")))
        {
            return dir.FullName;
        }
        dir = dir.Parent;
    }
    throw new DirectoryNotFoundException("Could not locate repository root.");
}

internal sealed class ContractTest
{
    private readonly string _name;
    private int _failed;

    public ContractTest(string name)
    {
        _name = name;
    }

    public void Check(bool condition, string message)
    {
        if (condition)
        {
            Console.WriteLine($"PASS {message}");
        }
        else
        {
            Fail(message);
        }
    }

    public void Fail(string message)
    {
        _failed++;
        Console.WriteLine($"FAIL {message}");
    }

    public int Finish()
    {
        Console.WriteLine($"{_name}: {(_failed == 0 ? "PASS" : "FAIL")}");
        return _failed == 0 ? 0 : 1;
    }
}
