using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using ChessOnlineProtocol;
using Microsoft.AspNetCore.SignalR.Client;

var stopwatch = Stopwatch.StartNew();
try
{
    var options = SmokeOptions.Parse(args);
    using var runTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
    await RunAsync(options, runTimeout.Token);
    Console.WriteLine($"SMOKE PASS duration={stopwatch.Elapsed}");
    return 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine($"SMOKE TIMEOUT duration={stopwatch.Elapsed}");
    return 124;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"SMOKE FAIL {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine(ex);
    return 1;
}

static async Task RunAsync(SmokeOptions options, CancellationToken cancellationToken)
{
    var baseUrl = options.BaseUrl.TrimEnd('/');
    var hubUrl = $"{baseUrl}/chess3d/relay";
    using var http = new HttpClient
    {
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromSeconds(Math.Min(30, Math.Max(5, options.TimeoutSeconds)))
    };

    Console.WriteLine($"STEP START health baseUrl={baseUrl}");
    var live = await http.GetStringAsync("/healthz/live", cancellationToken);
    Require(live.Contains("Healthy", StringComparison.OrdinalIgnoreCase), "live health is Healthy");
    using var ready = await JsonDocument.ParseAsync(
        await http.GetStreamAsync("/healthz/ready", cancellationToken),
        cancellationToken: cancellationToken);
    Require(JsonString(ready.RootElement, "status") == "ready", "ready status is ready");
    Require(JsonInt(ready.RootElement, "profileCount") == 5, "ready profile count is 5");
    using var diagnostics = await JsonDocument.ParseAsync(
        await http.GetStreamAsync("/chess3d/diagnostics", cancellationToken),
        cancellationToken: cancellationToken);
    Require(JsonBool(diagnostics.RootElement, "authEnabled"), "auth is enabled");
    Require(JsonString(diagnostics.RootElement, "authorityNativeLibraryName") == "libChess3DEngine.so", "Linux native library is selected");
    Require(JsonBool(diagnostics.RootElement, "authorityIsSupported"), "native authority is supported");
    Console.WriteLine("STEP PASS health");

    var suffix = Guid.NewGuid().ToString("N")[..10];
    var password1 = $"Smoke-{suffix}-A!2026";
    var password2 = $"Smoke-{suffix}-B!2026";
    var user1 = $"smoke-a-{suffix}";
    var user2 = $"smoke-b-{suffix}";

    Console.WriteLine("STEP START register");
    var token1 = await RegisterAsync(http, user1, "Smoke A", password1, cancellationToken);
    var token2 = await RegisterAsync(http, user2, "Smoke B", password2, cancellationToken);
    Require(!string.IsNullOrWhiteSpace(token1.AccessToken) && !string.IsNullOrWhiteSpace(token2.AccessToken), "both access tokens issued");
    Console.WriteLine($"STEP PASS register players={Short(token1.PlayerId)},{Short(token2.PlayerId)}");

    await using var client1 = NewAuthenticatedClient(hubUrl, token1.AccessToken);
    await using var client2 = NewAuthenticatedClient(hubUrl, token2.AccessToken);

    Console.WriteLine("STEP START SignalR connect");
    await client1.StartAsync(cancellationToken);
    await client2.StartAsync(cancellationToken);
    var hello1 = await InvokeAsync(client1, "Hello", Message(OnlineMessageTypes.Hello, "smoke-client-a", token1.PlayerId), cancellationToken);
    var hello2 = await InvokeAsync(client2, "Hello", Message(OnlineMessageTypes.Hello, "smoke-client-b", token2.PlayerId), cancellationToken);
    Require(hello1.Envelope.MessageType == OnlineMessageTypes.Welcome, "player A welcomed");
    Require(hello2.Envelope.MessageType == OnlineMessageTypes.Welcome, "player B welcomed");
    Console.WriteLine("STEP PASS SignalR connect");

    Console.WriteLine($"STEP START matchmaking ruleset={options.RulesetId}");
    var queue1 = Message(OnlineMessageTypes.JoinMatchmaking, "smoke-client-a", token1.PlayerId);
    queue1.Matchmaking = new OnlineMatchmakingCommand { RequestedRulesetId = options.RulesetId, ExpireSeconds = 120 };
    var queued = await InvokeAsync(client1, "JoinMatchmaking", queue1, cancellationToken);
    Require(queued.Envelope.MessageType == OnlineMessageTypes.MatchmakingJoined, "first player queued");

    var queue2 = Message(OnlineMessageTypes.JoinMatchmaking, "smoke-client-b", token2.PlayerId);
    queue2.Matchmaking = new OnlineMatchmakingCommand { RequestedRulesetId = options.RulesetId, ExpireSeconds = 120 };
    var found = await InvokeAsync(client2, "JoinMatchmaking", queue2, cancellationToken);
    Require(found.Envelope.MessageType == OnlineMessageTypes.MatchFound, "second player matched");
    var roomId = found.MatchmakingStatus?.RoomId ?? "";
    var tableId = found.MatchmakingStatus?.TableId ?? "";
    Require(!string.IsNullOrWhiteSpace(roomId) && !string.IsNullOrWhiteSpace(tableId), "match contains room/table");
    Console.WriteLine($"STEP PASS matchmaking room={roomId} table={tableId}");

    Console.WriteLine("STEP START Asgard start");
    var ready1 = Message(OnlineMessageTypes.Ready, "smoke-client-a", token1.PlayerId, roomId, tableId);
    ready1.Table = new OnlineTableCommand { Ready = true };
    var ready2 = Message(OnlineMessageTypes.Ready, "smoke-client-b", token2.PlayerId, roomId, tableId);
    ready2.Table = new OnlineTableCommand { Ready = true };
    await InvokeAsync(client1, "Ready", ready1, cancellationToken);
    await InvokeAsync(client2, "Ready", ready2, cancellationToken);
    var started = await InvokeAsync(client1, "StartGame", Message(OnlineMessageTypes.StartGame, "smoke-client-a", token1.PlayerId, roomId, tableId), cancellationToken);
    Require(started.Envelope.MessageType == OnlineMessageTypes.GameStarted, "Asgard game started");
    Require(started.Snapshot?.RulesetId == options.RulesetId, "snapshot is Asgard");
    var startedSnapshot = started.Snapshot ?? throw new InvalidOperationException("started snapshot is missing");
    Require(!string.IsNullOrWhiteSpace(startedSnapshot.StateHash), "snapshot has state hash");
    Console.WriteLine($"STEP PASS Asgard start hash={startedSnapshot.StateHash}");

    Console.WriteLine("STEP START Asgard action");
    var action = Message(OnlineMessageTypes.SubmitAction, "smoke-client-a", token1.PlayerId, roomId, tableId);
    action.Action = new OnlineActionCommand
    {
        ActionKind = OnlineActionKinds.NormalMove,
        ActorSide = 1,
        ExpectedStateHashBefore = startedSnapshot.StateHash,
        FromX = options.FromX,
        FromY = options.FromY,
        FromZ = options.FromZ,
        ToX = options.ToX,
        ToY = options.ToY,
        ToZ = options.ToZ
    };
    var accepted = await InvokeAsync(client1, "SubmitAction", action, cancellationToken);
    Require(accepted.Envelope.MessageType == OnlineMessageTypes.ActionAccepted, $"Asgard action accepted, reject={accepted.Error?.ReasonCode}");
    Require(accepted.ActionLog?.Events.Count >= 1, "action log contains accepted event");
    var acceptedLog = accepted.ActionLog ?? throw new InvalidOperationException("accepted action log is missing");
    Require(!string.IsNullOrWhiteSpace(acceptedLog.Events[^1].StateHashAfter), "accepted event has state hash");
    Console.WriteLine($"STEP PASS Asgard action notation={acceptedLog.Events[^1].Notation}");

    Console.WriteLine("STEP START snapshot/actionlog");
    var snapshot = await InvokeAsync(client1, "RequestSnapshot", Message(OnlineMessageTypes.RequestSnapshot, "smoke-client-a", token1.PlayerId, roomId, tableId), cancellationToken);
    Require(snapshot.Envelope.MessageType == OnlineMessageTypes.AuthoritativeSnapshot, "snapshot returned");
    Require(snapshot.Snapshot?.ActionCount >= 1, "snapshot action count updated");
    var actionLog = await InvokeAsync(client1, "RequestActionLog", Message(OnlineMessageTypes.RequestActionLog, "smoke-client-a", token1.PlayerId, roomId, tableId), cancellationToken);
    Require(actionLog.ActionLog?.Events.Count >= 1, "action log returned");
    Console.WriteLine($"STEP PASS snapshot/actionlog finalHash={snapshot.Snapshot?.StateHash}");
}

static async Task<AuthTokenResponse> RegisterAsync(HttpClient http, string userName, string displayName, string password, CancellationToken cancellationToken)
{
    var response = await http.PostAsJsonAsync("/api/auth/register", new AuthRegisterRequest(userName, displayName, password, "hetzner-smoke"), cancellationToken);
    var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(cancellationToken: cancellationToken);
    if (!response.IsSuccessStatusCode || token?.Success != true)
    {
        throw new InvalidOperationException($"register failed: status={(int)response.StatusCode}, code={token?.ErrorCode}");
    }
    return token;
}

static HubConnection NewAuthenticatedClient(string hubUrl, string accessToken)
{
    return new HubConnectionBuilder()
        .WithUrl(hubUrl, options => options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken))
        .Build();
}

static async Task<OnlineProtocolMessage> InvokeAsync(HubConnection client, string methodName, OnlineProtocolMessage message, CancellationToken cancellationToken)
{
    return await client.InvokeAsync<OnlineProtocolMessage>(methodName, message, cancellationToken);
}

static OnlineProtocolMessage Message(string type, string clientId, string playerId, string roomId = "", string tableId = "")
{
    return new OnlineProtocolMessage
    {
        Envelope = new OnlineMessageEnvelope
        {
            MessageType = type,
            MessageId = Guid.NewGuid().ToString("N"),
            RoomId = roomId,
            TableId = tableId,
            ClientId = clientId,
            PlayerId = playerId,
            ClientSeq = DateTime.UtcNow.Ticks,
            SentAtUtc = DateTime.UtcNow.ToString("O")
        }
    };
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static string JsonString(JsonElement element, string propertyName)
{
    return element.TryGetProperty(propertyName, out var value) ? value.GetString() ?? "" : "";
}

static int JsonInt(JsonElement element, string propertyName)
{
    return element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result) ? result : 0;
}

static bool JsonBool(JsonElement element, string propertyName)
{
    return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.True;
}

static string Short(string value)
{
    return value.Length <= 8 ? value : value[..8];
}

internal sealed record AuthRegisterRequest(string UserName, string DisplayName, string Password, string ClientName);

internal sealed class AuthTokenResponse
{
    public bool Success { get; set; }
    public string PlayerId { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string ErrorCode { get; set; } = "";
}

internal sealed record SmokeOptions(
    string BaseUrl,
    string RulesetId,
    int TimeoutSeconds,
    int FromX,
    int FromY,
    int FromZ,
    int ToX,
    int ToY,
    int ToZ)
{
    public static SmokeOptions Parse(string[] args)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }
            var key = args[i][2..];
            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";
            map[key] = value;
        }

        var baseUrl = Required(map, "base-url");
        return new SmokeOptions(
            baseUrl,
            map.GetValueOrDefault("ruleset", "asgard-convergence-3d-8x8x8-v0.1"),
            Int(map, "timeout", 120),
            Int(map, "from-x", 2),
            Int(map, "from-y", 3),
            Int(map, "from-z", 0),
            Int(map, "to-x", 2),
            Int(map, "to-y", 3),
            Int(map, "to-z", 1));
    }

    private static string Required(Dictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Missing required argument --{key}");
        }
        return value;
    }

    private static int Int(Dictionary<string, string> map, string key, int fallback)
    {
        return map.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
