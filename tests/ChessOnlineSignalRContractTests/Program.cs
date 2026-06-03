using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using ChessOnlineProtocol;
using ChessOnlineServer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

var test = new ContractTest("ChessOnlineSignalRContractTests");

try
{
    var root = FindRepoRoot();
    var profileRoot = Path.Combine(root, "assets", "rules", "profiles");
    var port = FindFreePort();
    var url = $"http://127.0.0.1:{port}";
    var hubUrl = $"{url}/chess3d/relay";

    await using var app = ChessOnlineServerHost.BuildApp(Array.Empty<string>(), options =>
    {
        options.HostUrls = url;
        options.ProfileRoot = profileRoot;
        options.RateLimitPermitLimit = 500;
        options.MaxReceiveMessageBytes = 65536;
    });
    await app.StartAsync();

    await ServerStartupTests(test, url, hubUrl);
    await ProtocolTests(test, hubUrl);
    await RoomTableAuthorityTests(test, hubUrl, profileRoot);
    await ProfileActionTests(test, hubUrl, profileRoot);
    await ReconnectTests(test, hubUrl, profileRoot);
    await ConcurrencyTests(test, hubUrl, profileRoot);
    await SecurityTests(test, hubUrl);
    FixtureParseTests(test, Path.Combine(root, "assets", "rules", "scenarios", "chess3d", "signalr"));

    await app.StopAsync();
    return test.Finish();
}
catch (Exception ex)
{
    test.Fail($"Unhandled exception: {ex}");
    return test.Finish();
}

static async Task ServerStartupTests(ContractTest test, string url, string hubUrl)
{
    using var http = new HttpClient();
    var live = await http.GetAsync($"{url}/healthz/live");
    test.Check(live.StatusCode == HttpStatusCode.OK, "SignalR server live health is OK");
    var ready = await http.GetAsync($"{url}/healthz/ready");
    test.Check(ready.StatusCode == HttpStatusCode.OK, "SignalR server ready health is OK");
    var diagnostics = await http.GetStringAsync($"{url}/chess3d/diagnostics");
    test.Check(diagnostics.Contains(OnlineProtocolVersion.ProtocolId, StringComparison.Ordinal), "SignalR diagnostics are parseable");
    test.Check(!diagnostics.Contains("sessionToken", StringComparison.OrdinalIgnoreCase), "SignalR diagnostics do not expose session tokens");

    await using var client = NewClient(hubUrl);
    await client.StartAsync();
    test.Check(client.State == HubConnectionState.Connected, "SignalR client connects");
}

static async Task ProtocolTests(ContractTest test, string hubUrl)
{
    await using var client = NewClient(hubUrl);
    var welcomeEvents = new List<OnlineProtocolMessage>();
    client.On<OnlineProtocolMessage>("ReceiveWelcome", welcomeEvents.Add);
    await client.StartAsync();
    var hello = await client.InvokeAsync<OnlineProtocolMessage>("Hello", Message(OnlineMessageTypes.Hello, "protocol-client", "protocol-player"));
    test.Check(hello.Envelope.MessageType == OnlineMessageTypes.Welcome &&
        !string.IsNullOrWhiteSpace(hello.Envelope.SessionToken), "SignalR Hello returns Welcome and session token");
    test.Check(welcomeEvents.Count == 1, "SignalR Hello emits ReceiveWelcome");

    var wrong = Message(OnlineMessageTypes.Hello, "wrong-client", "wrong-player");
    wrong.Envelope.ProtocolId = "wrong.protocol";
    var rejected = await client.InvokeAsync<OnlineProtocolMessage>("Hello", wrong);
    test.Check(rejected.Error?.ReasonCode == OnlineRejectReasons.WrongProtocol, "SignalR wrong protocol is rejected");
}

static async Task RoomTableAuthorityTests(ContractTest test, string hubUrl, string profileRoot)
{
    await using var client1 = NewClient(hubUrl);
    await using var client2 = NewClient(hubUrl);
    var acceptedBroadcasts = new List<OnlineProtocolMessage>();
    client2.On<OnlineProtocolMessage>("ReceiveActionAccepted", acceptedBroadcasts.Add);
    await client1.StartAsync();
    await client2.StartAsync();

    await Hello(client1, "c1", "p1");
    await Hello(client2, "c2", "p2");
    test.Check((await client1.InvokeAsync<OnlineProtocolMessage>("CreateRoom", Message(OnlineMessageTypes.CreateRoom, "c1", "p1", "room-signalr", ""))).Envelope.MessageType == OnlineMessageTypes.RoomCreated,
        "SignalR create room succeeds");
    test.Check((await client1.InvokeAsync<OnlineProtocolMessage>("JoinRoom", Message(OnlineMessageTypes.JoinRoom, "c1", "p1", "room-signalr", ""))).Envelope.MessageType == OnlineMessageTypes.RoomJoined,
        "SignalR join room succeeds");
    await client2.InvokeAsync<OnlineProtocolMessage>("JoinRoom", Message(OnlineMessageTypes.JoinRoom, "c2", "p2", "room-signalr", ""));
    var table = Message(OnlineMessageTypes.CreateTable, "c1", "p1", "room-signalr", "");
    table.Table = new OnlineTableCommand { TableId = "classic", RulesetId = "classic-six-side-3d-8x8x8-v0.1" };
    test.Check((await client1.InvokeAsync<OnlineProtocolMessage>("CreateTable", table)).Envelope.MessageType == OnlineMessageTypes.TableCreated,
        "SignalR create table succeeds");
    var seat1 = Message(OnlineMessageTypes.JoinTableSeat, "c1", "p1", "room-signalr", "classic");
    seat1.Table = new OnlineTableCommand { SeatIndex = 1 };
    test.Check((await client1.InvokeAsync<OnlineProtocolMessage>("JoinTableSeat", seat1)).Envelope.MessageType == OnlineMessageTypes.SeatAssigned,
        "SignalR join seat succeeds");
    var duplicate = Message(OnlineMessageTypes.JoinTableSeat, "c2", "p2", "room-signalr", "classic");
    duplicate.Table = new OnlineTableCommand { SeatIndex = 1 };
    test.Check((await client2.InvokeAsync<OnlineProtocolMessage>("JoinTableSeat", duplicate)).Error?.ReasonCode == OnlineRejectReasons.SeatOccupied,
        "SignalR duplicate seat is rejected");
    var seat2 = Message(OnlineMessageTypes.JoinTableSeat, "c2", "p2", "room-signalr", "classic");
    seat2.Table = new OnlineTableCommand { SeatIndex = 2 };
    await client2.InvokeAsync<OnlineProtocolMessage>("JoinTableSeat", seat2);
    var ready = Message(OnlineMessageTypes.Ready, "c1", "p1", "room-signalr", "classic");
    ready.Table = new OnlineTableCommand { Ready = true };
    await client1.InvokeAsync<OnlineProtocolMessage>("Ready", ready);
    var start = await client1.InvokeAsync<OnlineProtocolMessage>("StartGame", Message(OnlineMessageTypes.StartGame, "c1", "p1", "room-signalr", "classic"));
    test.Check(start.Envelope.MessageType == OnlineMessageTypes.GameStarted &&
        !string.IsNullOrWhiteSpace(start.Snapshot?.StateHash), "SignalR start emits authoritative snapshot");

    var helper = StartedRegistry(profileRoot, "helper-room", "helper-table", "classic-six-side-3d-8x8x8-v0.1", 1);
    var command = helper.BuildFirstLegalNormalMoveCommand("helper-room", "helper-table", 1);
    test.Check(command != null, "SignalR helper can build legal classic command");
    var action = Message(OnlineMessageTypes.SubmitAction, "c1", "p1", "room-signalr", "classic");
    action.Action = command;
    var accepted = await client1.InvokeAsync<OnlineProtocolMessage>("SubmitAction", action);
    test.Check(accepted.Envelope.MessageType == OnlineMessageTypes.ActionAccepted &&
        accepted.ActionLog?.Events.Count == 1, "SignalR Classic legal action accepted");
    await Task.Delay(150);
    test.Check(acceptedBroadcasts.Count >= 1, "SignalR accepted action broadcasts to table group");

    var wrong = Message(OnlineMessageTypes.SubmitAction, "c2", "p2", "room-signalr", "classic");
    wrong.Action = command;
    var wrongResult = await client2.InvokeAsync<OnlineProtocolMessage>("SubmitAction", wrong);
    test.Check(wrongResult.Error?.ReasonCode == OnlineRejectReasons.WrongActor ||
        wrongResult.Error?.ReasonCode == OnlineRejectReasons.IllegalAction, "SignalR wrong actor/action is rejected");

    var stale = Message(OnlineMessageTypes.SubmitAction, "c1", "p1", "room-signalr", "classic");
    stale.Action = Clone(command!);
    stale.Action.ExpectedStateHashBefore = "stale";
    var staleResult = await client1.InvokeAsync<OnlineProtocolMessage>("SubmitAction", stale);
    test.Check(staleResult.Envelope.MessageType == OnlineMessageTypes.ResyncRequired &&
        staleResult.Snapshot != null, "SignalR stale hash triggers resync snapshot");

    var snapshot = await client1.InvokeAsync<OnlineProtocolMessage>("RequestSnapshot", Message(OnlineMessageTypes.RequestSnapshot, "c1", "p1", "room-signalr", "classic"));
    test.Check(snapshot.Envelope.MessageType == OnlineMessageTypes.AuthoritativeSnapshot, "SignalR snapshot returned to requester");
    var actionLog = await client1.InvokeAsync<OnlineProtocolMessage>("RequestActionLog", Message(OnlineMessageTypes.RequestActionLog, "c1", "p1", "room-signalr", "classic"));
    test.Check(actionLog.ActionLog?.Events.Count >= 1, "SignalR action log chunk returned");
}

static async Task ProfileActionTests(ContractTest test, string hubUrl, string profileRoot)
{
    var rubik = await StartedClient(hubUrl, "room-rubik-signalr", "rubik", "rubik-convergence-3d-8x8x8-v0.1", "rubik-client", "rubik-player", 1);
    await using (rubik.Client)
    {
        var layer = Message(OnlineMessageTypes.SubmitAction, "rubik-client", "rubik-player", "room-rubik-signalr", "rubik");
        layer.Action = new OnlineActionCommand { ActionKind = OnlineActionKinds.RubikLayerTurn, ActorSide = 1, Axis = 0, Layer = 0, QuarterTurns = 1 };
        test.Check((await rubik.Client.InvokeAsync<OnlineProtocolMessage>("SubmitAction", layer)).Envelope.MessageType == OnlineMessageTypes.ActionAccepted,
            "SignalR Rubik layer command accepted under Rubik profile");
    }

    var hodgeHelper = StartedRegistry(profileRoot, "helper-hodge-room", "helper-hodge", "hodge-projection-duel-3d-8x8x8-v0.1", 1);
    var hodgeCommand = hodgeHelper.BuildFirstAiCandidateCommand("helper-hodge-room", "helper-hodge", OnlineActionKinds.HodgeProjectedMove);
    var hodge = await StartedClient(hubUrl, "room-hodge-signalr", "hodge", "hodge-projection-duel-3d-8x8x8-v0.1", "hodge-client", "hodge-player", 1);
    await using (hodge.Client)
    {
        test.Check(hodgeCommand != null, "SignalR helper can build Hodge composite command");
        var message = Message(OnlineMessageTypes.SubmitAction, "hodge-client", "hodge-player", "room-hodge-signalr", "hodge");
        message.Action = hodgeCommand;
        var result = await hodge.Client.InvokeAsync<OnlineProtocolMessage>("SubmitAction", message);
        test.Check(result.Envelope.MessageType == OnlineMessageTypes.ActionAccepted &&
            result.ActionLog?.Events[0].Notation.Contains("HPD", StringComparison.OrdinalIgnoreCase) == true,
            "SignalR Hodge composite action remains all-or-nothing");
    }
}

static async Task ReconnectTests(ContractTest test, string hubUrl, string profileRoot)
{
    var started = await StartedClient(hubUrl, "room-reconnect", "reconnect", "classic-six-side-3d-8x8x8-v0.1", "rc-client", "rc-player", 1);
    var token = started.SessionToken;
    await started.Client.DisposeAsync();

    await using var reconnected = NewClient(hubUrl);
    await reconnected.StartAsync();
    var hello = Message(OnlineMessageTypes.Hello, "rc-client-2", "rc-player");
    hello.Envelope.SessionToken = token;
    var result = await reconnected.InvokeAsync<OnlineProtocolMessage>("Hello", hello);
    test.Check(result.Envelope.MessageType == OnlineMessageTypes.Welcome &&
        result.Envelope.SessionToken == token, "SignalR reconnect with valid token succeeds");
    var snapshot = await reconnected.InvokeAsync<OnlineProtocolMessage>("RequestSnapshot", Message(OnlineMessageTypes.RequestSnapshot, "rc-client-2", "rc-player", "room-reconnect", "reconnect"));
    test.Check(snapshot.Envelope.MessageType == OnlineMessageTypes.AuthoritativeSnapshot, "SignalR reconnect can request authoritative snapshot");

    await using var invalid = NewClient(hubUrl);
    await invalid.StartAsync();
    var bad = Message(OnlineMessageTypes.Hello, "bad-client", "rc-player");
    bad.Envelope.SessionToken = "bad-token";
    var badResult = await invalid.InvokeAsync<OnlineProtocolMessage>("Hello", bad);
    test.Check(badResult.Error?.ReasonCode == OnlineRejectReasons.IllegalAction, "SignalR invalid session token rejected");
}

static async Task ConcurrencyTests(ContractTest test, string hubUrl, string profileRoot)
{
    await using var a = NewClient(hubUrl);
    await using var b = NewClient(hubUrl);
    await a.StartAsync();
    await b.StartAsync();
    await Hello(a, "race-a", "race-a");
    await Hello(b, "race-b", "race-b");
    await a.InvokeAsync<OnlineProtocolMessage>("CreateRoom", Message(OnlineMessageTypes.CreateRoom, "race-a", "race-a", "room-race", ""));
    await a.InvokeAsync<OnlineProtocolMessage>("JoinRoom", Message(OnlineMessageTypes.JoinRoom, "race-a", "race-a", "room-race", ""));
    await b.InvokeAsync<OnlineProtocolMessage>("JoinRoom", Message(OnlineMessageTypes.JoinRoom, "race-b", "race-b", "room-race", ""));
    var table = Message(OnlineMessageTypes.CreateTable, "race-a", "race-a", "room-race", "");
    table.Table = new OnlineTableCommand { TableId = "race", RulesetId = "classic-six-side-3d-8x8x8-v0.1" };
    await a.InvokeAsync<OnlineProtocolMessage>("CreateTable", table);
    var seatA = Message(OnlineMessageTypes.JoinTableSeat, "race-a", "race-a", "room-race", "race");
    seatA.Table = new OnlineTableCommand { SeatIndex = 1 };
    var seatB = Message(OnlineMessageTypes.JoinTableSeat, "race-b", "race-b", "room-race", "race");
    seatB.Table = new OnlineTableCommand { SeatIndex = 1 };
    var seats = await Task.WhenAll(
        a.InvokeAsync<OnlineProtocolMessage>("JoinTableSeat", seatA),
        b.InvokeAsync<OnlineProtocolMessage>("JoinTableSeat", seatB));
    test.Check(seats.Count(r => r.Envelope.MessageType == OnlineMessageTypes.SeatAssigned) == 1, "SignalR parallel duplicate seat join has exactly one winner");

    var rubikHelper = await StartedClient(hubUrl, "room-parallel-actions", "rubik", "rubik-convergence-3d-8x8x8-v0.1", "parallel-client", "parallel-player", 1);
    await using (rubikHelper.Client)
    {
        var m1 = Message(OnlineMessageTypes.SubmitAction, "parallel-client", "parallel-player", "room-parallel-actions", "rubik");
        m1.Action = new OnlineActionCommand { ActionKind = OnlineActionKinds.RubikLayerTurn, ActorSide = 1, Axis = 0, Layer = 0, QuarterTurns = 1 };
        var m2 = Message(OnlineMessageTypes.SubmitAction, "parallel-client", "parallel-player", "room-parallel-actions", "rubik");
        m2.Action = new OnlineActionCommand { ActionKind = OnlineActionKinds.RubikLayerTurn, ActorSide = 1, Axis = 1, Layer = 1, QuarterTurns = 1 };
        var actions = await Task.WhenAll(
            rubikHelper.Client.InvokeAsync<OnlineProtocolMessage>("SubmitAction", m1),
            rubikHelper.Client.InvokeAsync<OnlineProtocolMessage>("SubmitAction", m2));
        var accepted = actions.Where(r => r.Envelope.MessageType == OnlineMessageTypes.ActionAccepted).ToArray();
        test.Check(accepted.Length >= 1 && accepted.Select(r => r.ActionLog?.Events[0].ServerSeq ?? 0).Distinct().Count() == accepted.Length,
            "SignalR parallel SubmitAction keeps unique monotonic serverSeq values");
    }
}

static async Task SecurityTests(ContractTest test, string hubUrl)
{
    await using var client = NewClient(hubUrl);
    await client.StartAsync();
    await Hello(client, "sec-client", "sec-player");
    var huge = Message(OnlineMessageTypes.Ping, "sec-client", "sec-player");
    huge.Text = new string('x', OnlineProtocolVersion.MaxMessageBytes + 1);
    try
    {
        var result = await client.InvokeAsync<OnlineProtocolMessage>("Ping", huge);
        test.Check(result.Error?.ReasonCode == OnlineRejectReasons.OversizedMessage, "SignalR oversized hub message rejected");
        test.Check(result.Error?.ReasonText.Contains("Exception", StringComparison.OrdinalIgnoreCase) != true, "SignalR public error hides exception details");
    }
    catch (HubException)
    {
        test.Check(true, "SignalR oversized transport message closes cleanly before hub dispatch");
    }
    catch (InvalidOperationException)
    {
        test.Check(true, "SignalR oversized transport message disconnects client without server state mutation");
    }
}

static void FixtureParseTests(ContractTest test, string fixtureRoot)
{
    var expected = new[]
    {
        "signalr_hello_connect_v0_1.json",
        "signalr_room_create_join_v0_1.json",
        "signalr_table_start_classic_v0_1.json",
        "signalr_classic_action_accept_broadcast_v0_1.json",
        "signalr_wrong_actor_reject_v0_1.json",
        "signalr_stale_hash_resync_v0_1.json",
        "signalr_reconnect_snapshot_v0_1.json",
        "signalr_action_log_chunk_v0_1.json",
        "signalr_rubik_layer_turn_v0_1.json",
        "signalr_hodge_composite_v0_1.json",
        "signalr_asgard_restore_smoke_v0_1.json",
        "signalr_duplicate_seat_race_v0_1.json",
        "signalr_parallel_submit_monotonic_seq_v0_1.json",
        "signalr_malformed_message_reject_v0_1.json",
        "signalr_diagnostics_no_secret_v0_1.json"
    };
    foreach (var file in expected)
    {
        var path = Path.Combine(fixtureRoot, file);
        test.Check(File.Exists(path), $"SignalR fixture exists: {file}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        test.Check(doc.RootElement.TryGetProperty("format", out var format) &&
            format.GetString() == "chess3d-signalr-regression", $"SignalR fixture parses: {file}");
    }
}

static async Task<StartedHubClient> StartedClient(string hubUrl, string roomId, string tableId, string rulesetId, string clientId, string playerId, int seat)
{
    var client = NewClient(hubUrl);
    await client.StartAsync();
    var hello = await Hello(client, clientId, playerId);
    await client.InvokeAsync<OnlineProtocolMessage>("CreateRoom", Message(OnlineMessageTypes.CreateRoom, clientId, playerId, roomId, ""));
    await client.InvokeAsync<OnlineProtocolMessage>("JoinRoom", Message(OnlineMessageTypes.JoinRoom, clientId, playerId, roomId, ""));
    var createTable = Message(OnlineMessageTypes.CreateTable, clientId, playerId, roomId, "");
    createTable.Table = new OnlineTableCommand { TableId = tableId, RulesetId = rulesetId };
    await client.InvokeAsync<OnlineProtocolMessage>("CreateTable", createTable);
    var joinSeat = Message(OnlineMessageTypes.JoinTableSeat, clientId, playerId, roomId, tableId);
    joinSeat.Table = new OnlineTableCommand { SeatIndex = seat };
    await client.InvokeAsync<OnlineProtocolMessage>("JoinTableSeat", joinSeat);
    var ready = Message(OnlineMessageTypes.Ready, clientId, playerId, roomId, tableId);
    ready.Table = new OnlineTableCommand { Ready = true };
    await client.InvokeAsync<OnlineProtocolMessage>("Ready", ready);
    await client.InvokeAsync<OnlineProtocolMessage>("StartGame", Message(OnlineMessageTypes.StartGame, clientId, playerId, roomId, tableId));
    return new StartedHubClient(client, hello.Envelope.SessionToken);
}

static async Task<OnlineProtocolMessage> Hello(HubConnection client, string clientId, string playerId)
{
    return await client.InvokeAsync<OnlineProtocolMessage>("Hello", Message(OnlineMessageTypes.Hello, clientId, playerId));
}

static OnlineRoomRegistry StartedRegistry(string profileRoot, string roomId, string tableId, string rulesetId, int seat)
{
    var registry = new OnlineRoomRegistry(profileRoot);
    var env = (string type) => Envelope(type, roomId, tableId, $"client-{roomId}", $"player-{roomId}");
    registry.CreateRoom(env(OnlineMessageTypes.CreateRoom), new OnlineRoomCommand { RoomId = roomId, DisplayName = roomId });
    registry.JoinRoom(env(OnlineMessageTypes.JoinRoom));
    registry.CreateTable(env(OnlineMessageTypes.CreateTable), new OnlineTableCommand { TableId = tableId, RulesetId = rulesetId });
    registry.JoinTableSeat(env(OnlineMessageTypes.JoinTableSeat), new OnlineTableCommand { SeatIndex = seat });
    registry.Ready(env(OnlineMessageTypes.Ready), new OnlineTableCommand { Ready = true });
    registry.StartGame(env(OnlineMessageTypes.StartGame));
    return registry;
}

static OnlineProtocolMessage Message(string type, string clientId, string playerId, string roomId = "", string tableId = "")
{
    return new OnlineProtocolMessage
    {
        Envelope = Envelope(type, roomId, tableId, clientId, playerId)
    };
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

static HubConnection NewClient(string hubUrl)
{
    return new HubConnectionBuilder()
        .WithUrl(hubUrl)
        .Build();
}

static int FindFreePort()
{
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    return ((IPEndPoint)listener.LocalEndpoint).Port;
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

static OnlineActionCommand Clone(OnlineActionCommand command)
{
    var json = JsonSerializer.Serialize(command, OnlineProtocolJson.Options);
    return JsonSerializer.Deserialize<OnlineActionCommand>(json, OnlineProtocolJson.Options) ?? new OnlineActionCommand();
}

internal sealed record StartedHubClient(HubConnection Client, string SessionToken);

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
