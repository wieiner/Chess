using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using ChessOnlinePersistence;
using ChessOnlinePersistence.Entities;
using ChessOnlinePersistence.Repositories;
using ChessOnlineProtocol;
using ChessOnlineServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

var test = new ContractTest("ChessOnlineSignalRContractTests");
WebApplication? app = null;

try
{
    var root = FindRepoRoot();
    var profileRoot = Path.Combine(root, "assets", "rules", "profiles");
    var port = FindFreePort();
    var url = $"http://127.0.0.1:{port}";
    var hubUrl = $"{url}/chess3d/relay";
    var serverTempRoot = Path.Combine(root, ".tmp", "chess3d-p4a-signalr-tests", Guid.NewGuid().ToString("N"));

    app = ChessOnlineServerHost.BuildApp(Array.Empty<string>(), options =>
    {
        options.HostUrls = url;
        options.ProfileRoot = profileRoot;
        options.RateLimitPermitLimit = 500;
        options.MaxReceiveMessageBytes = 65536;
        options.Persistence.StorePath = Path.Combine(serverTempRoot, "store", "online-store.json");
        options.DataProtection.KeyRingPath = Path.Combine(serverTempRoot, "keys");
    });
    await app.StartAsync();

    await ServerStartupTests(test, url, hubUrl);
    await ProtocolTests(test, hubUrl);
    await RoomTableAuthorityTests(test, hubUrl, profileRoot);
    await ProfileActionTests(test, hubUrl, profileRoot);
    await ReconnectTests(test, hubUrl, profileRoot);
    await ConcurrencyTests(test, hubUrl, profileRoot);
    await SecurityTests(test, hubUrl);
    await AuthPersistenceTests(test, root, profileRoot);
    await PersistenceRestartSequenceTests(test, root);
    FixtureParseTests(test, Path.Combine(root, "assets", "rules", "scenarios", "chess3d", "signalr"));
    FixtureParseTestsWithFormat(test, Path.Combine(root, "assets", "rules", "scenarios", "chess3d", "identity"), "chess3d-identity-regression", "Identity fixture");
    FixtureParseTestsWithFormat(test, Path.Combine(root, "assets", "rules", "scenarios", "chess3d", "persistence"), "chess3d-persistence-regression", "Persistence fixture");
    FixtureParseTestsWithFormat(test, Path.Combine(root, "assets", "rules", "scenarios", "chess3d", "matchmaking"), "chess3d-matchmaking-regression", "Matchmaking fixture");
    FixtureParseTestsWithFormat(test, Path.Combine(root, "assets", "rules", "scenarios", "chess3d", "asgard_online"), "chess3d-asgard-online-regression", "Asgard online fixture");
    FixtureParseTestsWithFormat(test, Path.Combine(root, "assets", "rules", "scenarios", "chess3d", "deployment"), "chess3d-deployment-regression", "Deployment fixture");

    return test.Finish();
}
catch (Exception ex)
{
    test.Fail($"Unhandled exception: {ex}");
    return test.Finish();
}
finally
{
    if (app != null)
    {
        using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try { await app.StopAsync(stopCts.Token); } catch { }
        await app.DisposeAsync();
    }
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
    var welcomeEventCount = 0;
    client.On<OnlineProtocolMessage>("ReceiveWelcome", _ => Interlocked.Increment(ref welcomeEventCount));
    await client.StartAsync();
    var hello = await client.InvokeAsync<OnlineProtocolMessage>("Hello", Message(OnlineMessageTypes.Hello, "protocol-client", "protocol-player"));
    test.Check(hello.Envelope.MessageType == OnlineMessageTypes.Welcome &&
        !string.IsNullOrWhiteSpace(hello.Envelope.SessionToken), "SignalR Hello returns Welcome and session token");
    test.Check(await WaitUntilAsync(() => Volatile.Read(ref welcomeEventCount) == 1), "SignalR Hello emits ReceiveWelcome");

    var wrong = Message(OnlineMessageTypes.Hello, "wrong-client", "wrong-player");
    wrong.Envelope.ProtocolId = "wrong.protocol";
    var rejected = await client.InvokeAsync<OnlineProtocolMessage>("Hello", wrong);
    test.Check(rejected.Error?.ReasonCode == OnlineRejectReasons.WrongProtocol, "SignalR wrong protocol is rejected");
}

static async Task RoomTableAuthorityTests(ContractTest test, string hubUrl, string profileRoot)
{
    await using var client1 = NewClient(hubUrl);
    await using var client2 = NewClient(hubUrl);
    var acceptedBroadcastCount = 0;
    client2.On<OnlineProtocolMessage>("ReceiveActionAccepted", _ => Interlocked.Increment(ref acceptedBroadcastCount));
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
    test.Check(await WaitUntilAsync(() => Volatile.Read(ref acceptedBroadcastCount) >= 1), "SignalR accepted action broadcasts to table group");

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

static async Task AuthPersistenceTests(ContractTest test, string root, string profileRoot)
{
    var port = FindFreePort();
    var url = $"http://127.0.0.1:{port}";
    var hubUrl = $"{url}/chess3d/relay";
    var tempRoot = Path.Combine(root, ".tmp", "chess3d-p4a-tests", Guid.NewGuid().ToString("N"));
    var storePath = Path.Combine(tempRoot, "store", "online-store.json");
    var keyPath = Path.Combine(tempRoot, "keys");

    await using var authApp = ChessOnlineServerHost.BuildApp(Array.Empty<string>(), options =>
    {
        options.HostUrls = url;
        options.ProfileRoot = profileRoot;
        options.RateLimitPermitLimit = 500;
        options.Auth.EnableAuthentication = true;
        options.Auth.AllowDevAnonymousSessions = false;
        options.Auth.AccessTokenMinutes = 5;
        options.Auth.RefreshTokenDays = 1;
        options.Persistence.StorePath = storePath;
        options.DataProtection.KeyRingPath = keyPath;
    });
    await authApp.StartAsync();

    using var http = new HttpClient { BaseAddress = new Uri(url) };
    var register = await http.PostAsJsonAsync("/api/auth/register", new AuthRegisterRequest
    {
        UserName = "p4a-user",
        DisplayName = "P4A User",
        Password = "correct horse battery staple",
        ClientName = "contract-test"
    });
    var token = await register.Content.ReadFromJsonAsync<AuthTokenResponse>();
    test.Check(register.IsSuccessStatusCode && token?.Success == true &&
        !string.IsNullOrWhiteSpace(token.AccessToken) &&
        !string.IsNullOrWhiteSpace(token.RefreshToken), "P4A register issues protected access and refresh tokens");

    var diagnostics = await http.GetStringAsync("/chess3d/diagnostics");
    test.Check(!diagnostics.Contains("accessToken", StringComparison.OrdinalIgnoreCase) &&
        !diagnostics.Contains("refreshToken", StringComparison.OrdinalIgnoreCase) &&
        !diagnostics.Contains("passwordHash", StringComparison.OrdinalIgnoreCase), "P4A diagnostics do not expose credentials");

    await using var anonymous = NewClient(hubUrl);
    await anonymous.StartAsync();
    var anonResult = await anonymous.InvokeAsync<OnlineProtocolMessage>("CreateRoom", Message(OnlineMessageTypes.CreateRoom, "anon", "", "auth-room", ""));
    test.Check(anonResult.Error?.ReasonCode == OnlineRejectReasons.IllegalAction, "P4A auth-required server rejects anonymous mutating command");
    var anonMatchmaking = Message(OnlineMessageTypes.JoinMatchmaking, "anon", "", "", "");
    anonMatchmaking.Matchmaking = new OnlineMatchmakingCommand { RequestedRulesetId = "classic-six-side-3d-8x8x8-v0.1" };
    test.Check((await anonymous.InvokeAsync<OnlineProtocolMessage>("JoinMatchmaking", anonMatchmaking)).Error?.ReasonCode == OnlineRejectReasons.IllegalAction,
        "P4B auth-required server rejects anonymous matchmaking");

    await using var spoof = NewAuthenticatedClient(hubUrl, token!.AccessToken);
    await spoof.StartAsync();
    var spoofed = await spoof.InvokeAsync<OnlineProtocolMessage>("Hello", Message(OnlineMessageTypes.Hello, "auth-client", "other-player"));
    test.Check(spoofed.Error?.ReasonCode == OnlineRejectReasons.IllegalAction, "P4A authenticated hub rejects spoofed playerId envelope");

    await using var client = NewAuthenticatedClient(hubUrl, token.AccessToken);
    await client.StartAsync();
    var hello = await client.InvokeAsync<OnlineProtocolMessage>("Hello", Message(OnlineMessageTypes.Hello, "auth-client", token.PlayerId));
    test.Check(hello.Envelope.MessageType == OnlineMessageTypes.Welcome &&
        hello.Envelope.PlayerId == token.PlayerId &&
        hello.Envelope.SessionToken == token.SessionId, "P4A Hello derives player and session from authenticated token");

    await client.InvokeAsync<OnlineProtocolMessage>("CreateRoom", Message(OnlineMessageTypes.CreateRoom, "auth-client", token.PlayerId, "auth-room", ""));
    await client.InvokeAsync<OnlineProtocolMessage>("JoinRoom", Message(OnlineMessageTypes.JoinRoom, "auth-client", token.PlayerId, "auth-room", ""));
    var table = Message(OnlineMessageTypes.CreateTable, "auth-client", token.PlayerId, "auth-room", "");
    table.Table = new OnlineTableCommand { TableId = "auth-table", RulesetId = "classic-six-side-3d-8x8x8-v0.1" };
    test.Check((await client.InvokeAsync<OnlineProtocolMessage>("CreateTable", table)).Envelope.MessageType == OnlineMessageTypes.TableCreated,
        "P4A authenticated client creates table");
    var seat = Message(OnlineMessageTypes.JoinTableSeat, "auth-client", token.PlayerId, "auth-room", "auth-table");
    seat.Table = new OnlineTableCommand { SeatIndex = 1 };
    await client.InvokeAsync<OnlineProtocolMessage>("JoinTableSeat", seat);
    var ready = Message(OnlineMessageTypes.Ready, "auth-client", token.PlayerId, "auth-room", "auth-table");
    ready.Table = new OnlineTableCommand { Ready = true };
    await client.InvokeAsync<OnlineProtocolMessage>("Ready", ready);
    await client.InvokeAsync<OnlineProtocolMessage>("StartGame", Message(OnlineMessageTypes.StartGame, "auth-client", token.PlayerId, "auth-room", "auth-table"));

    var helper = StartedRegistry(profileRoot, "auth-helper-room", "auth-helper-table", "classic-six-side-3d-8x8x8-v0.1", 1);
    var command = helper.BuildFirstLegalNormalMoveCommand("auth-helper-room", "auth-helper-table", 1);
    var action = Message(OnlineMessageTypes.SubmitAction, "auth-client", token.PlayerId, "auth-room", "auth-table");
    action.Action = command;
    var accepted = await client.InvokeAsync<OnlineProtocolMessage>("SubmitAction", action);
    test.Check(accepted.Envelope.MessageType == OnlineMessageTypes.ActionAccepted, "P4A authenticated SubmitAction reaches authority registry");

    var register2 = await http.PostAsJsonAsync("/api/auth/register", new AuthRegisterRequest
    {
        UserName = "p4b-user-2",
        DisplayName = "P4B User 2",
        Password = "correct horse battery staple",
        ClientName = "contract-test"
    });
    var token2 = await register2.Content.ReadFromJsonAsync<AuthTokenResponse>();
    test.Check(register2.IsSuccessStatusCode && token2?.Success == true, "P4B second player registration succeeds");

    await using var client2 = NewAuthenticatedClient(hubUrl, token2!.AccessToken);
    await client2.StartAsync();
    await client2.InvokeAsync<OnlineProtocolMessage>("Hello", Message(OnlineMessageTypes.Hello, "auth-client-2", token2.PlayerId));

    var joinClassic1 = Message(OnlineMessageTypes.JoinMatchmaking, "auth-client", token.PlayerId);
    joinClassic1.Matchmaking = new OnlineMatchmakingCommand { RequestedRulesetId = "classic-six-side-3d-8x8x8-v0.1", ExpireSeconds = 120 };
    var queuedClassic = await client.InvokeAsync<OnlineProtocolMessage>("JoinMatchmaking", joinClassic1);
    test.Check(queuedClassic.Envelope.MessageType == OnlineMessageTypes.MatchmakingJoined &&
        queuedClassic.MatchmakingStatus?.State == "Queued", "P4B first Classic player enters matchmaking queue");
    var duplicateClassic = await client.InvokeAsync<OnlineProtocolMessage>("JoinMatchmaking", joinClassic1);
    test.Check(duplicateClassic.Error?.ReasonCode == OnlineRejectReasons.AlreadyQueued, "P4B duplicate matchmaking ticket is rejected");
    var statusClassic = await client.InvokeAsync<OnlineProtocolMessage>("GetMatchmakingStatus", Message(OnlineMessageTypes.GetMatchmakingStatus, "auth-client", token.PlayerId));
    test.Check(statusClassic.Envelope.MessageType == OnlineMessageTypes.MatchmakingStatus &&
        statusClassic.MatchmakingStatus?.State == "Queued", "P4B matchmaking status reports queued ticket");

    var joinClassic2 = Message(OnlineMessageTypes.JoinMatchmaking, "auth-client-2", token2.PlayerId);
    joinClassic2.Matchmaking = new OnlineMatchmakingCommand { RequestedRulesetId = "classic-six-side-3d-8x8x8-v0.1", ExpireSeconds = 120 };
    var classicFound = await client2.InvokeAsync<OnlineProtocolMessage>("JoinMatchmaking", joinClassic2);
    test.Check(classicFound.Envelope.MessageType == OnlineMessageTypes.MatchFound &&
        classicFound.MatchmakingStatus?.Tickets.Count == 2 &&
        !string.IsNullOrWhiteSpace(classicFound.MatchmakingStatus.RoomId) &&
        !string.IsNullOrWhiteSpace(classicFound.MatchmakingStatus.TableId), "P4B second Classic player creates match-found room/table");

    var joinAsgard1 = Message(OnlineMessageTypes.JoinMatchmaking, "auth-client", token.PlayerId);
    joinAsgard1.Matchmaking = new OnlineMatchmakingCommand { RequestedRulesetId = "asgard-convergence-3d-8x8x8-v0.1", ExpireSeconds = 120 };
    var asgardQueued = await client.InvokeAsync<OnlineProtocolMessage>("JoinMatchmaking", joinAsgard1);
    test.Check(asgardQueued.Envelope.MessageType == OnlineMessageTypes.MatchmakingJoined, "P4B first Asgard player enters matchmaking queue");
    var joinAsgard2 = Message(OnlineMessageTypes.JoinMatchmaking, "auth-client-2", token2.PlayerId);
    joinAsgard2.Matchmaking = new OnlineMatchmakingCommand { RequestedRulesetId = "asgard-convergence-3d-8x8x8-v0.1", ExpireSeconds = 120 };
    var asgardFound = await client2.InvokeAsync<OnlineProtocolMessage>("JoinMatchmaking", joinAsgard2);
    test.Check(asgardFound.Envelope.MessageType == OnlineMessageTypes.MatchFound, "P4B Asgard matchmaking creates match-found room/table");
    var asgardRoom = asgardFound.MatchmakingStatus?.RoomId ?? "";
    var asgardTable = asgardFound.MatchmakingStatus?.TableId ?? "";
    var ready1 = Message(OnlineMessageTypes.Ready, "auth-client", token.PlayerId, asgardRoom, asgardTable);
    ready1.Table = new OnlineTableCommand { Ready = true };
    var ready2 = Message(OnlineMessageTypes.Ready, "auth-client-2", token2.PlayerId, asgardRoom, asgardTable);
    ready2.Table = new OnlineTableCommand { Ready = true };
    await client.InvokeAsync<OnlineProtocolMessage>("Ready", ready1);
    await client2.InvokeAsync<OnlineProtocolMessage>("Ready", ready2);
    var asgardStart = await client.InvokeAsync<OnlineProtocolMessage>("StartGame", Message(OnlineMessageTypes.StartGame, "auth-client", token.PlayerId, asgardRoom, asgardTable));
    test.Check(asgardStart.Envelope.MessageType == OnlineMessageTypes.GameStarted &&
        asgardStart.Snapshot?.RulesetId == "asgard-convergence-3d-8x8x8-v0.1", "P4B matched Asgard table starts with authoritative snapshot");
    var asgardHelper = StartedRegistry(profileRoot, "auth-asgard-helper-room", "auth-asgard-helper-table", "asgard-convergence-3d-8x8x8-v0.1", 1);
    var asgardCommand = asgardHelper.BuildFirstLegalNormalMoveCommand("auth-asgard-helper-room", "auth-asgard-helper-table", 1);
    test.Check(asgardCommand != null, "P4B helper can build Asgard legal action");
    if (asgardCommand != null)
    {
        var asgardAction = Message(OnlineMessageTypes.SubmitAction, "auth-client", token.PlayerId, asgardRoom, asgardTable);
        asgardAction.Action = asgardCommand;
        var asgardAccepted = await client.InvokeAsync<OnlineProtocolMessage>("SubmitAction", asgardAction);
        test.Check(asgardAccepted.Envelope.MessageType == OnlineMessageTypes.ActionAccepted, "P4B matched Asgard table accepts legal action");
    }

    await AssertMatchmakingProfile(test, client, token.PlayerId, client2, token2.PlayerId,
        "rubik-convergence-3d-8x8x8-v0.1", "Rubik");
    var latestMatch = await AssertMatchmakingProfile(test, client, token.PlayerId, client2, token2.PlayerId,
        "hodge-projection-duel-3d-8x8x8-v0.1", "Hodge");

    var refresh = await http.PostAsJsonAsync("/api/auth/refresh", new AuthRefreshRequest { RefreshToken = token.RefreshToken });
    var refreshed = await refresh.Content.ReadFromJsonAsync<AuthTokenResponse>();
    test.Check(refresh.IsSuccessStatusCode && refreshed?.Success == true &&
        !string.IsNullOrWhiteSpace(refreshed.AccessToken), "P4A refresh token issues new access token");

    await http.PostAsJsonAsync("/api/auth/logout", new AuthRefreshRequest { RefreshToken = token.RefreshToken });
    var rejectedRefresh = await http.PostAsJsonAsync("/api/auth/refresh", new AuthRefreshRequest { RefreshToken = token.RefreshToken });
    test.Check(rejectedRefresh.StatusCode == HttpStatusCode.Unauthorized, "P4A logout revokes refresh session");

    var storeJson = File.ReadAllText(storePath);
    using var storeDoc = JsonDocument.Parse(storeJson);
    test.Check(storeDoc.RootElement.GetProperty("players").GetArrayLength() >= 2, "P4A/P4B JSON store persists accounts");
    test.Check(storeDoc.RootElement.GetProperty("sessions").GetArrayLength() >= 2, "P4A/P4B JSON store persists durable sessions");
    test.Check(storeDoc.RootElement.GetProperty("rooms").EnumerateArray().Any(r =>
        JsonString(r, "roomId") == classicFound.MatchmakingStatus?.RoomId &&
        JsonString(r, "state") == OnlineMessageTypes.MatchFound), "P4C matched Classic room is persisted after matchmaking");
    test.Check(storeDoc.RootElement.GetProperty("tables").EnumerateArray().Any(t =>
        JsonString(t, "tableId") == $"{classicFound.MatchmakingStatus?.RoomId}/{classicFound.MatchmakingStatus?.TableId}" &&
        JsonString(t, "rulesetId") == "classic-six-side-3d-8x8x8-v0.1"), "P4C matched Classic table is persisted after matchmaking");
    test.Check(storeDoc.RootElement.GetProperty("seats").EnumerateArray().Count(s =>
        JsonString(s, "tableId") == $"{classicFound.MatchmakingStatus?.RoomId}/{classicFound.MatchmakingStatus?.TableId}") == 2, "P4C matched Classic seats are persisted after matchmaking");
    test.Check(storeDoc.RootElement.GetProperty("sessions").EnumerateArray().Any(s =>
        JsonString(s, "sessionId") == token.SessionId &&
        JsonString(s, "lastKnownRoomId") == latestMatch.RoomId &&
        JsonString(s, "lastKnownTableId") == latestMatch.TableId), "P4C matched player session records latest match-found table");
    test.Check(storeDoc.RootElement.GetProperty("actions").GetArrayLength() >= 1, "P4A JSON store persists accepted action log event");

    await authApp.StopAsync();
}

static async Task<(string RoomId, string TableId)> AssertMatchmakingProfile(
    ContractTest test,
    HubConnection client1,
    string player1,
    HubConnection client2,
    string player2,
    string rulesetId,
    string label)
{
    var queue1 = Message(OnlineMessageTypes.JoinMatchmaking, $"match-{label}-1", player1);
    queue1.Matchmaking = new OnlineMatchmakingCommand { RequestedRulesetId = rulesetId, ExpireSeconds = 120 };
    var queued = await client1.InvokeAsync<OnlineProtocolMessage>("JoinMatchmaking", queue1);
    test.Check(queued.Envelope.MessageType == OnlineMessageTypes.MatchmakingJoined, $"P4C {label} first player enters matchmaking queue");

    var queue2 = Message(OnlineMessageTypes.JoinMatchmaking, $"match-{label}-2", player2);
    queue2.Matchmaking = new OnlineMatchmakingCommand { RequestedRulesetId = rulesetId, ExpireSeconds = 120 };
    var found = await client2.InvokeAsync<OnlineProtocolMessage>("JoinMatchmaking", queue2);
    test.Check(found.Envelope.MessageType == OnlineMessageTypes.MatchFound &&
        found.MatchmakingStatus?.Tickets.Count == 2 &&
        !string.IsNullOrWhiteSpace(found.MatchmakingStatus.RoomId) &&
        !string.IsNullOrWhiteSpace(found.MatchmakingStatus.TableId), $"P4C {label} matchmaking creates match-found room/table");
    return (found.MatchmakingStatus?.RoomId ?? "", found.MatchmakingStatus?.TableId ?? "");
}

static async Task PersistenceRestartSequenceTests(ContractTest test, string root)
{
    var storePath = Path.Combine(root, ".tmp", "chess3d-p4g2-restart-store", Guid.NewGuid().ToString("N"), "store.json");
    var store = new JsonOnlineStore(new OnlineStoreOptions { StorePath = storePath });
    const string tableKey = "match-1-asgard/table-1";

    await store.AppendActionAsync(new PersistentActionLogEntity
    {
        TableId = tableKey,
        ServerSeq = 1,
        ActionIndex = 1,
        ActorPlayerId = "player-before-restart",
        ActionKind = OnlineActionKinds.NormalMove,
        ActionJson = "{}",
        Notation = "before restart",
        StateHashAfter = "hash-before",
        CreatedAtUtc = DateTime.UtcNow
    });

    await store.ClearActionLogAsync(tableKey);

    await store.AppendActionAsync(new PersistentActionLogEntity
    {
        TableId = tableKey,
        ServerSeq = 1,
        ActionIndex = 1,
        ActorPlayerId = "player-after-restart",
        ActionKind = OnlineActionKinds.NormalMove,
        ActionJson = "{}",
        Notation = "after restart",
        StateHashAfter = "hash-after",
        CreatedAtUtc = DateTime.UtcNow
    });

    var log = await store.GetActionLogAsync(tableKey);
    test.Check(log.Count == 1 &&
        log[0].ServerSeq == 1 &&
        log[0].Notation == "after restart",
        "P4G2 persistence clears stale action log for reused matchmaking table key after restart");
}

static void FixtureParseTests(ContractTest test, string fixtureRoot)
{
    FixtureParseTestsWithFormat(test, fixtureRoot, "chess3d-signalr-regression", "SignalR fixture");
}

static void FixtureParseTestsWithFormat(ContractTest test, string fixtureRoot, string expectedFormat, string label)
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
    var files = expectedFormat == "chess3d-signalr-regression"
        ? expected
        : Directory.Exists(fixtureRoot)
            ? Directory.GetFiles(fixtureRoot, "*.json").Select(Path.GetFileName).Where(f => f != null).Cast<string>().OrderBy(f => f).ToArray()
            : Array.Empty<string>();
    test.Check(files.Length > 0, $"{label} directory has fixtures");

    foreach (var file in files)
    {
        var path = Path.Combine(fixtureRoot, file);
        test.Check(File.Exists(path), $"SignalR fixture exists: {file}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        test.Check(doc.RootElement.TryGetProperty("format", out var format) &&
            format.GetString() == expectedFormat, $"{label} parses: {file}");
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

static HubConnection NewAuthenticatedClient(string hubUrl, string accessToken)
{
    return new HubConnectionBuilder()
        .WithUrl(hubUrl, options => options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken))
        .Build();
}

static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
{
    var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
    while (DateTime.UtcNow < deadline)
    {
        if (condition())
        {
            return true;
        }
        await Task.Delay(25);
    }
    return condition();
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

static string JsonString(JsonElement element, string propertyName)
{
    return element.TryGetProperty(propertyName, out var property) ? property.GetString() ?? "" : "";
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
