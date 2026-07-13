using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChessOnlineClient;
using ChessOnlineProtocol;
using Microsoft.AspNetCore.SignalR.Client;

var stopwatch = Stopwatch.StartNew();
SmokeOptions? parsedOptions = null;
try
{
    parsedOptions = SmokeOptions.Parse(args);
    using var runTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(parsedOptions.TimeoutSeconds));
    await RunAsync(parsedOptions, runTimeout.Token);
    Console.WriteLine($"SMOKE PASS scenario={parsedOptions.Scenario} runId={parsedOptions.RunId} duration={stopwatch.Elapsed}");
    return 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine($"STEP FAIL scenario={parsedOptions?.Scenario ?? "validation"} reason=timeout");
    Console.Error.WriteLine($"SMOKE FAIL scenario={parsedOptions?.Scenario ?? "validation"} result=timeout duration={stopwatch.Elapsed}");
    return 124;
}
catch (Exception ex)
{
    var safeReason = SanitizeLogText(ex.Message);
    Console.Error.WriteLine($"STEP FAIL scenario={parsedOptions?.Scenario ?? "validation"} reason={safeReason}");
    Console.Error.WriteLine($"SMOKE FAIL scenario={parsedOptions?.Scenario ?? "validation"} error={ex.GetType().Name} duration={stopwatch.Elapsed}");
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
    var capabilities = SmokeCapabilities.Parse(diagnostics.RootElement);
    RequireScenarioCapabilities(options, capabilities);
    Console.WriteLine($"STEP PASS health scenario={options.Scenario} profile={options.ProfileId}");

    var suffix = Guid.NewGuid().ToString("N")[..10];
    var password1 = $"Smoke-{suffix}-A!2026";
    var password2 = $"Smoke-{suffix}-B!2026";
    var user1 = $"smoke-a-{suffix}";
    var user2 = $"smoke-b-{suffix}";

    Console.WriteLine("STEP START register");
    var registered1 = await RegisterAsync(http, user1, "Smoke A", password1, cancellationToken);
    var registered2 = await RegisterAsync(http, user2, "Smoke B", password2, cancellationToken);
    Require(!string.IsNullOrWhiteSpace(registered1.PlayerId) && !string.IsNullOrWhiteSpace(registered2.PlayerId), "both players registered");
    Console.WriteLine($"STEP PASS register players={Short(registered1.PlayerId)},{Short(registered2.PlayerId)}");

    Console.WriteLine("STEP START login");
    var token1 = await LoginAsync(http, user1, password1, cancellationToken);
    var token2 = await LoginAsync(http, user2, password2, cancellationToken);
    Require(!string.IsNullOrWhiteSpace(token1.AccessToken) && !string.IsNullOrWhiteSpace(token2.AccessToken), "both login access tokens issued");
    Console.WriteLine($"STEP PASS login players={Short(token1.PlayerId)},{Short(token2.PlayerId)}");

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

    if (options.ShouldRun(OnlineUxSmokeScenarios.Lobby))
    {
        await RequestLobbySnapshotAsync(client1, token1, options, "initial", cancellationToken);
    }

    Console.WriteLine($"STEP START matchmaking ruleset={options.ProfileId}");
    var onePlayerProfile = options.ProfileId.Contains("single-side", StringComparison.OrdinalIgnoreCase);
    var queue1 = Message(OnlineMessageTypes.JoinMatchmaking, "smoke-client-a", token1.PlayerId);
    queue1.Matchmaking = new OnlineMatchmakingCommand { RequestedRulesetId = options.ProfileId, ExpireSeconds = 120 };
    var queued = await InvokeAsync(client1, "JoinMatchmaking", queue1, cancellationToken);

    OnlineProtocolMessage found;
    if (onePlayerProfile)
    {
        Require(queued.Envelope.MessageType == OnlineMessageTypes.MatchFound, "single-side first player matched");
        found = queued;
    }
    else
    {
        Require(queued.Envelope.MessageType == OnlineMessageTypes.MatchmakingJoined, "first player queued");
        var queue2 = Message(OnlineMessageTypes.JoinMatchmaking, "smoke-client-b", token2.PlayerId);
        queue2.Matchmaking = new OnlineMatchmakingCommand { RequestedRulesetId = options.ProfileId, ExpireSeconds = 120 };
        found = await InvokeAsync(client2, "JoinMatchmaking", queue2, cancellationToken);
        Require(found.Envelope.MessageType == OnlineMessageTypes.MatchFound, "second player matched");
    }
    var roomId = found.MatchmakingStatus?.RoomId ?? "";
    var tableId = found.MatchmakingStatus?.TableId ?? "";
    Require(!string.IsNullOrWhiteSpace(roomId) && !string.IsNullOrWhiteSpace(tableId), "match contains room/table");
    Console.WriteLine($"STEP PASS matchmaking room={roomId} table={tableId}");

    Console.WriteLine("STEP START game start");
    var ready1 = Message(OnlineMessageTypes.Ready, "smoke-client-a", token1.PlayerId, roomId, tableId);
    ready1.Table = new OnlineTableCommand { Ready = true };
    await InvokeAsync(client1, "Ready", ready1, cancellationToken);
    if (!onePlayerProfile)
    {
        var ready2 = Message(OnlineMessageTypes.Ready, "smoke-client-b", token2.PlayerId, roomId, tableId);
        ready2.Table = new OnlineTableCommand { Ready = true };
        await InvokeAsync(client2, "Ready", ready2, cancellationToken);
    }
    var started = await InvokeAsync(client1, "StartGame", Message(OnlineMessageTypes.StartGame, "smoke-client-a", token1.PlayerId, roomId, tableId), cancellationToken);
    Require(started.Envelope.MessageType == OnlineMessageTypes.GameStarted, "game started");
    Require(started.Snapshot?.RulesetId == options.ProfileId, "snapshot ruleset matches requested ruleset");
    var startedSnapshot = started.Snapshot ?? throw new InvalidOperationException("started snapshot is missing");
    Require(!string.IsNullOrWhiteSpace(startedSnapshot.StateHash), "snapshot has state hash");
    Console.WriteLine($"STEP PASS game start hash={startedSnapshot.StateHash}");

    var actionSubmitted = false;
    if (options.SkipActionSubmit)
    {
        Console.WriteLine("STEP SKIP action submit skipped by --skip-action-submit");
    }
    else
    {
        Console.WriteLine($"STEP START profile action ruleset={options.ProfileId}");
        var command = await BuildLegalActionCommandAsync(client1, token1, roomId, tableId, startedSnapshot, options, cancellationToken);
        if (command == null)
        {
            Console.WriteLine("STEP SKIP profile action submit skipped because no safe preview/fallback action is available");
        }
        else
        {
        var action = Message(OnlineMessageTypes.SubmitAction, "smoke-client-a", token1.PlayerId, roomId, tableId);
        action.Action = command;
        var accepted = await InvokeAsync(client1, "SubmitAction", action, cancellationToken);
        Require(accepted.Envelope.MessageType == OnlineMessageTypes.ActionAccepted, $"profile action accepted, reject={accepted.Error?.ReasonCode}");
        Require(accepted.ActionLog?.Events.Count >= 1, "action log contains accepted event");
        var acceptedLog = accepted.ActionLog ?? throw new InvalidOperationException("accepted action log is missing");
        Require(!string.IsNullOrWhiteSpace(acceptedLog.Events[^1].StateHashAfter), "accepted event has state hash");
            actionSubmitted = true;
        Console.WriteLine($"STEP PASS profile action notation={acceptedLog.Events[^1].Notation}");
        }
    }

    Console.WriteLine("STEP START snapshot/actionlog");
    var snapshot = await InvokeAsync(client1, "RequestSnapshot", Message(OnlineMessageTypes.RequestSnapshot, "smoke-client-a", token1.PlayerId, roomId, tableId), cancellationToken);
    Require(snapshot.Envelope.MessageType == OnlineMessageTypes.AuthoritativeSnapshot, "snapshot returned");
    if (!actionSubmitted)
    {
        Require(snapshot.Snapshot != null, "snapshot returned after skipped action submit");
    }
    else
    {
        Require(snapshot.Snapshot?.ActionCount >= 1, "snapshot action count updated");
    }
    var actionLog = await InvokeAsync(client1, "RequestActionLog", Message(OnlineMessageTypes.RequestActionLog, "smoke-client-a", token1.PlayerId, roomId, tableId), cancellationToken);
    Require(actionLog.ActionLog != null, "action log returned");
    Console.WriteLine($"STEP PASS snapshot/actionlog finalHash={snapshot.Snapshot?.StateHash}");

    var latestSnapshot = snapshot.Snapshot ?? startedSnapshot;
    var latestActionLog = actionLog.ActionLog ?? new OnlineActionLogChunk();

    if (options.ShouldRun(OnlineUxSmokeScenarios.Resume))
    {
        await RunResumeScenarioAsync(client1, token1, roomId, tableId, latestSnapshot, latestActionLog, options, cancellationToken);
    }

    if (options.ShouldRun(OnlineUxSmokeScenarios.Lobby))
    {
        await RunLobbyScenarioAsync(client1, token1, roomId, tableId, options, cancellationToken);
    }

    if (options.ShouldRun(OnlineUxSmokeScenarios.Spectator))
    {
        await RunSpectatorScenarioAsync(
            http,
            hubUrl,
            onePlayerProfile ? client1 : client2,
            onePlayerProfile ? token1 : token2,
            roomId,
            tableId,
            latestSnapshot,
            latestActionLog,
            options,
            cancellationToken);
    }

    if (options.Scenario == OnlineUxSmokeScenarios.All)
    {
        Console.WriteLine("STEP START final diagnostics");
        using var finalDiagnostics = await JsonDocument.ParseAsync(
            await http.GetStreamAsync("/chess3d/diagnostics", cancellationToken),
            cancellationToken: cancellationToken);
        Require(JsonBool(finalDiagnostics.RootElement, "authorityIsSupported"), "final diagnostics authority remains supported");
        Console.WriteLine("STEP PASS final diagnostics");
    }
}

static async Task<OnlineLobbySnapshot> RequestLobbySnapshotAsync(
    HubConnection client,
    AuthTokenResponse token,
    SmokeOptions options,
    string stage,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"STEP START lobby snapshot stage={stage}");
    var request = Message(OnlineMessageTypes.RequestLobbySnapshot, "smoke-client-a", token.PlayerId);
    request.LobbyRequest = new OnlineLobbySnapshotRequest
    {
        RulesetIdFilter = options.ProfileId,
        IncludeInGameTables = true,
        IncludeWaitingTables = true,
        IncludeFinishedTables = false
    };
    var response = await InvokeAsync(client, "RequestLobbySnapshot", request, cancellationToken);
    Require(response.Envelope.MessageType == OnlineMessageTypes.LobbySnapshot, $"{stage} lobby snapshot returned");
    var lobby = response.LobbySnapshot ?? throw new InvalidOperationException($"{stage} lobby snapshot missing");
    Require(lobby.Tables.All(row => row.SeatSummaries.All(seat => seat.PlayerLabel.Length <= 8)), "lobby exposes shortened player labels only");
    Console.WriteLine($"STEP PASS lobby snapshot stage={stage} tables={lobby.Tables.Count} seq={lobby.ServerSeq}");
    return lobby;
}

static async Task RunLobbyScenarioAsync(
    HubConnection client,
    AuthTokenResponse token,
    string roomId,
    string tableId,
    SmokeOptions options,
    CancellationToken cancellationToken)
{
    var lobby = await RequestLobbySnapshotAsync(client, token, options, "active-match", cancellationToken);
    var row = lobby.Tables.FirstOrDefault(t =>
        string.Equals(t.RoomId, roomId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(t.TableId, tableId, StringComparison.OrdinalIgnoreCase));
    if (row is null)
    {
        throw new InvalidOperationException("lobby contains current table");
    }
    Require(row.RulesetId == options.ProfileId, "lobby row ruleset matches");
    Require(row.Started, "lobby row reports started table");
    Console.WriteLine($"STEP PASS lobby current table state={row.TableState} seq={row.LastServerSeq}");
}

static async Task RunResumeScenarioAsync(
    HubConnection client,
    AuthTokenResponse token,
    string roomId,
    string tableId,
    OnlineSnapshot latestSnapshot,
    OnlineActionLogChunk latestActionLog,
    SmokeOptions options,
    CancellationToken cancellationToken)
{
    Console.WriteLine("STEP START resume");
    var beforeHash = latestSnapshot.StateHash;
    var beforeSeq = latestSnapshot.ServerSeq;
    var beforeActionCount = latestSnapshot.ActionCount;

    await client.StopAsync(cancellationToken);
    await client.StartAsync(cancellationToken);
    var hello = await InvokeAsync(client, "Hello", Message(OnlineMessageTypes.Hello, "smoke-client-a-resumed", token.PlayerId), cancellationToken);
    Require(hello.Envelope.MessageType == OnlineMessageTypes.Welcome, "resumed player welcomed");

    var request = Message(OnlineMessageTypes.RequestResumeMatch, "smoke-client-a-resumed", token.PlayerId, roomId, tableId);
    request.ResumeRequest = new OnlineResumeRequest
    {
        PlayerId = token.PlayerId,
        RoomId = roomId,
        TableId = tableId,
        SeatIndex = 1,
        ExpectedRulesetId = options.ProfileId,
        LastKnownStateHash = beforeHash,
        LastKnownServerSeq = 0
    };

    var response = await InvokeAsync(client, "RequestResumeMatch", request, cancellationToken);
    Require(response.Envelope.MessageType == OnlineMessageTypes.ResumeMatchResult, "resume result returned");
    var resume = response.ResumeResult ?? throw new InvalidOperationException("resume result missing");
    Require(resume.Success, $"resume succeeded, reason={resume.FailureReason}");
    Require(resume.RoomId == roomId && resume.TableId == tableId, "resume room/table unchanged");
    Require(resume.RulesetId == options.ProfileId, "resume ruleset unchanged");
    Require(resume.Snapshot?.StateHash == beforeHash, "resume snapshot hash unchanged");
    var resumeSnapshot = resume.Snapshot ?? throw new InvalidOperationException("resume snapshot missing");
    Require(resumeSnapshot.ActionCount == beforeActionCount, "resume action count unchanged");
    Require((resume.ActionLog?.Events.Count ?? 0) == latestActionLog.Events.Count, "resume action log matches authoritative history");
    Require(resumeSnapshot.ServerSeq == beforeSeq, "resume server sequence unchanged");
    Console.WriteLine($"STEP PASS resume seat={resume.SeatIndex} hash={resumeSnapshot.StateHash} seq={resumeSnapshot.ServerSeq}");
}

static async Task RunSpectatorScenarioAsync(
    HttpClient http,
    string hubUrl,
    HubConnection activePlayer,
    AuthTokenResponse activePlayerToken,
    string roomId,
    string tableId,
    OnlineSnapshot latestSnapshot,
    OnlineActionLogChunk latestActionLog,
    SmokeOptions options,
    CancellationToken cancellationToken)
{
    Console.WriteLine("STEP START spectator");
    var suffix = Guid.NewGuid().ToString("N")[..10];
    var user = $"smoke-s-{suffix}";
    var password = $"Smoke-{suffix}-S!2026";
    var registered = await RegisterAsync(http, user, "Smoke Spectator", password, cancellationToken);
    Require(!string.IsNullOrWhiteSpace(registered.PlayerId), "spectator registered");
    var token = await LoginAsync(http, user, password, cancellationToken);
    Require(!string.IsNullOrWhiteSpace(token.AccessToken), "spectator login access token issued");

    await using var spectator = NewAuthenticatedClient(hubUrl, token.AccessToken);
    var receivedAccepted = new TaskCompletionSource<OnlineProtocolMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
    using var acceptedSubscription = spectator.On<OnlineProtocolMessage>("ReceiveActionAccepted", message => receivedAccepted.TrySetResult(message));
    await spectator.StartAsync(cancellationToken);
    var hello = await InvokeAsync(spectator, "Hello", Message(OnlineMessageTypes.Hello, "smoke-client-s", token.PlayerId), cancellationToken);
    Require(hello.Envelope.MessageType == OnlineMessageTypes.Welcome, "spectator welcomed");

    var request = Message(OnlineMessageTypes.JoinSpectator, "smoke-client-s", token.PlayerId, roomId, tableId);
    request.SpectatorRequest = new OnlineJoinSpectatorRequest
    {
        PlayerId = token.PlayerId,
        RoomId = roomId,
        TableId = tableId,
        ExpectedRulesetId = options.ProfileId,
        LastKnownServerSeq = latestSnapshot.ServerSeq
    };

    var response = await InvokeAsync(spectator, "JoinSpectator", request, cancellationToken);
    Require(response.Envelope.MessageType == OnlineMessageTypes.JoinSpectatorResult, "spectator result returned");
    var result = response.SpectatorResult ?? throw new InvalidOperationException("spectator result missing");
    Require(result.Success, $"spectator join succeeded, reason={result.FailureReason}");
    Require(result.State.IsSpectator, "spectator state is read-only spectator");
    Require(result.RoomId == roomId && result.TableId == tableId, "spectator room/table unchanged");
    Require(result.RulesetId == options.ProfileId, "spectator ruleset unchanged");
    Require(result.Snapshot?.StateHash == latestSnapshot.StateHash, "spectator snapshot hash matches latest");
    var spectatorSnapshot = result.Snapshot ?? throw new InvalidOperationException("spectator snapshot missing");
    Require((result.ActionLog?.Events.Count ?? 0) <= latestActionLog.Events.Count, "spectator action log tail did not grow unexpectedly");
    Console.WriteLine($"STEP PASS spectator joined spectatorId={Short(result.SpectatorId)} hash={spectatorSnapshot.StateHash} readonly={result.State.SubmitDisabledReason}");

    Console.WriteLine("STEP START spectator read-only authority");
    var ready = Message(OnlineMessageTypes.Ready, "smoke-client-s", token.PlayerId, roomId, tableId);
    ready.Table = new OnlineTableCommand { Ready = true };
    var readyResult = await InvokeAsync(spectator, "Ready", ready, cancellationToken);
    Require(readyResult.Envelope.MessageType != OnlineMessageTypes.TableState, "spectator Ready rejected");
    var startResult = await InvokeAsync(spectator, "StartGame", Message(OnlineMessageTypes.StartGame, "smoke-client-s", token.PlayerId, roomId, tableId), cancellationToken);
    Require(startResult.Envelope.MessageType != OnlineMessageTypes.GameStarted, "spectator StartGame rejected");
    var forbiddenAction = Message(OnlineMessageTypes.SubmitAction, "smoke-client-s", token.PlayerId, roomId, tableId);
    forbiddenAction.Action = new OnlineActionCommand
    {
        ActionKind = OnlineActionKinds.NormalMove,
        ActorSide = 1,
        ExpectedStateHashBefore = latestSnapshot.StateHash
    };
    var forbiddenResult = await InvokeAsync(spectator, "SubmitAction", forbiddenAction, cancellationToken);
    Require(forbiddenResult.Envelope.MessageType != OnlineMessageTypes.ActionAccepted, "spectator SubmitAction rejected");
    var unchanged = await InvokeAsync(spectator, "RequestSnapshot", Message(OnlineMessageTypes.RequestSnapshot, "smoke-client-s", token.PlayerId, roomId, tableId), cancellationToken);
    Require(unchanged.Snapshot?.StateHash == latestSnapshot.StateHash, "spectator mutation attempts leave state unchanged");
    Console.WriteLine("STEP PASS spectator read-only authority");

    if (options.SkipActionSubmit)
    {
        Console.WriteLine("STEP SKIP spectator live update reason=--skip-action-submit");
        return;
    }

    Console.WriteLine("STEP START spectator live update");
    var command = await BuildLegalActionCommandAsync(activePlayer, activePlayerToken, roomId, tableId, latestSnapshot, options, cancellationToken);
    Require(command != null, "active player has a submit-ready action for spectator update");
    var playerAction = Message(OnlineMessageTypes.SubmitAction, "smoke-active-player", activePlayerToken.PlayerId, roomId, tableId);
    playerAction.Action = command;
    var accepted = await InvokeAsync(activePlayer, "SubmitAction", playerAction, cancellationToken);
    Require(accepted.Envelope.MessageType == OnlineMessageTypes.ActionAccepted, "active player action accepted while spectator follows");
    var broadcast = await receivedAccepted.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
    Require(broadcast.Envelope.MessageType == OnlineMessageTypes.ActionAccepted, "spectator received action broadcast");
    var updated = await InvokeAsync(spectator, "RequestSnapshot", Message(OnlineMessageTypes.RequestSnapshot, "smoke-client-s", token.PlayerId, roomId, tableId), cancellationToken);
    Require(updated.Snapshot != null && updated.Snapshot.StateHash != latestSnapshot.StateHash, "spectator sees updated authoritative state");
    Console.WriteLine($"STEP PASS spectator live update hash={updated.Snapshot!.StateHash} seq={updated.Snapshot.ServerSeq}");
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

static async Task<AuthTokenResponse> LoginAsync(HttpClient http, string userName, string password, CancellationToken cancellationToken)
{
    var response = await http.PostAsJsonAsync("/api/auth/login", new AuthLoginRequest(userName, password, "hetzner-smoke"), cancellationToken);
    var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(cancellationToken: cancellationToken);
    if (!response.IsSuccessStatusCode || token?.Success != true)
    {
        throw new InvalidOperationException($"login failed: status={(int)response.StatusCode}, code={token?.ErrorCode}");
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

static async Task<OnlineActionCommand?> BuildLegalActionCommandAsync(
    HubConnection client,
    AuthTokenResponse token,
    string roomId,
    string tableId,
    OnlineSnapshot snapshot,
    SmokeOptions options,
    CancellationToken cancellationToken)
{
    if (!OnlineChess3DBoardSnapshotParser.TryParse(snapshot, out var board, out var parseError))
    {
        throw new InvalidOperationException($"Cannot build legal action: {parseError}");
    }

    var candidates = CandidateCellsForCurrentTurn(board).ToList();
    if (candidates.Count == 0)
    {
        throw new InvalidOperationException("Cannot build legal action: current side has no occupied source cells.");
    }

    foreach (var cell in candidates)
    {
        var request = Message(OnlineMessageTypes.RequestLegalPreview, "smoke-client-a", token.PlayerId, roomId, tableId);
        request.LegalPreviewRequest = new OnlineLegalPreviewRequest
        {
            PlayerId = token.PlayerId,
            RoomId = roomId,
            TableId = tableId,
            SourceX = cell.X,
            SourceY = cell.Y,
            SourceZ = cell.Z,
            ActorSide = cell.Side,
            MacroPlayer = board.CurrentMacroPlayer,
            ExpectedStateHash = snapshot.StateHash
        };

        OnlineProtocolMessage response;
        try
        {
            response = await InvokeAsync(client, "RequestLegalPreview", request, cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("Method does not exist", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("STEP INFO legal-preview hub method unavailable; checking versioned smoke fallback action");
            return LegacyFallbackAction(options, snapshot.StateHash);
        }

        if (response.Envelope.MessageType != OnlineMessageTypes.LegalPreviewResult || response.LegalPreview == null)
        {
            Console.WriteLine($"STEP INFO preview source={cell.Coordinate} side={cell.Side} skipped response={response.Envelope.MessageType} reason={response.Error?.ReasonCode}");
            continue;
        }

        if (response.LegalPreview.IsStale)
        {
            throw new InvalidOperationException("Cannot build legal action: legal preview response was stale.");
        }

        var option = SelectSmokeActionOption(response.LegalPreview.Options);
        if (option == null)
        {
            Console.WriteLine($"STEP INFO preview source={cell.Coordinate} side={cell.Side} has no submit-ready option reason={response.LegalPreview.NoLegalActionReason}");
            continue;
        }

        Console.WriteLine(
            "STEP PASS action-source=server-preview " +
            $"source={cell.Coordinate} side={cell.Side} kind={option.ActionKind} " +
            $"from=({option.From.X},{option.From.Y},{option.From.Z}) to=({option.To.X},{option.To.Y},{option.To.Z}) " +
            $"label={option.DisplayLabel}");
        return CommandFromOption(option, snapshot.StateHash);
    }

    throw new InvalidOperationException("Cannot build legal action: no legal preview option was available for the current turn.");
}

static OnlineActionCommand? LegacyFallbackAction(SmokeOptions options, string expectedStateHash)
{
    switch (options.ProfileId)
    {
        case "classic-six-side-3d-8x8x8-v0.1":
        case "single-side-3d-8x8x8-v0.1":
            return FallbackCommand(
                expectedStateHash,
                OnlineActionKinds.NormalMove,
                actorSide: 1,
                fromX: 4,
                fromY: 4,
                fromZ: 0,
                toX: 3,
                toY: 5,
                toZ: 1);
        case "asgard-convergence-3d-8x8x8-v0.1":
            return FallbackCommand(
                expectedStateHash,
                OnlineActionKinds.NormalMove,
                actorSide: 1,
                fromX: options.FromX,
                fromY: options.FromY,
                fromZ: options.FromZ,
                toX: options.ToX,
                toY: options.ToY,
                toZ: options.ToZ);
        default:
            Console.WriteLine($"STEP SKIP action-source=compat-fallback unsupported-profile={options.ProfileId}");
            return null;
    }
}

static OnlineActionCommand FallbackCommand(
    string expectedStateHash,
    string actionKind,
    int actorSide,
    int fromX,
    int fromY,
    int fromZ,
    int toX,
    int toY,
    int toZ)
{
    var command = new OnlineActionCommand
    {
        ActionKind = actionKind,
        ActorSide = actorSide,
        ExpectedStateHashBefore = expectedStateHash,
        FromX = fromX,
        FromY = fromY,
        FromZ = fromZ,
        ToX = toX,
        ToY = toY,
        ToZ = toZ
    };
    Console.WriteLine(
        "STEP INFO action-source=compat-fallback " +
        $"kind={command.ActionKind} side={command.ActorSide} " +
        $"from=({command.FromX},{command.FromY},{command.FromZ}) to=({command.ToX},{command.ToY},{command.ToZ})");
    return command;
}

static IEnumerable<OnlineChess3DBoardCell> CandidateCellsForCurrentTurn(OnlineChess3DBoardSnapshot board)
{
    var currentSide = board.CurrentSide;
    if (currentSide > 0)
    {
        foreach (var cell in board.OccupiedCells.Where(cell => cell.Side == currentSide))
        {
            yield return cell;
        }
    }

    foreach (var cell in board.OccupiedCells.Where(cell => currentSide <= 0 || cell.Side != currentSide))
    {
        yield return cell;
    }
}

static OnlineLegalActionOption? SelectSmokeActionOption(IReadOnlyList<OnlineLegalActionOption> options)
{
    return options
        .Where(IsSubmitReadyAction)
        .OrderByDescending(option => option.IsRecommendedSafeTestAction)
        .ThenBy(option => option.ActionKind == OnlineActionKinds.NormalMove ? 0 : 1)
        .ThenBy(option => option.IsCapture ? 1 : 0)
        .ThenBy(option => option.IsSpecial ? 1 : 0)
        .FirstOrDefault();
}

static bool IsSubmitReadyAction(OnlineLegalActionOption option)
{
    return option.ActionKind is OnlineActionKinds.NormalMove or
        OnlineActionKinds.HodgeProjectedMove or
        OnlineActionKinds.ReserveRestore or
        OnlineActionKinds.RubikLayerTurn;
}

static OnlineActionCommand CommandFromOption(OnlineLegalActionOption option, string expectedStateHash)
{
    return new OnlineActionCommand
    {
        ActionKind = option.ActionKind,
        ActorSide = option.ActorSide != 0 ? option.ActorSide : option.Side,
        MacroPlayer = option.MacroPlayer,
        ExpectedStateHashBefore = expectedStateHash,
        FromX = option.From.X,
        FromY = option.From.Y,
        FromZ = option.From.Z,
        ToX = option.To.X,
        ToY = option.To.Y,
        ToZ = option.To.Z,
        PromotionType = option.PromotionType,
        Side = option.Side,
        PieceType = option.PieceType,
        X = option.ReserveTarget.X,
        Y = option.ReserveTarget.Y,
        Z = option.ReserveTarget.Z,
        PrimarySide = option.PrimarySide,
        Axis = option.Axis,
        Layer = option.Layer,
        QuarterTurns = option.QuarterTurns
    };
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

static void RequireScenarioCapabilities(SmokeOptions options, SmokeCapabilities capabilities)
{
    if (options.ShouldRun(OnlineUxSmokeScenarios.Resume))
    {
        Require(capabilities.ResumeMatch && capabilities.SupportedHubMethods.Contains(OnlineMessageTypes.RequestResumeMatch),
            "deployed server does not advertise RequestResumeMatch");
    }
    if (options.ShouldRun(OnlineUxSmokeScenarios.Spectator))
    {
        Require(capabilities.SpectatorMode && capabilities.SupportedHubMethods.Contains(OnlineMessageTypes.JoinSpectator),
            "deployed server does not advertise JoinSpectator");
    }
    if (options.ShouldRun(OnlineUxSmokeScenarios.Lobby))
    {
        Require(capabilities.LobbySnapshot && capabilities.SupportedHubMethods.Contains(OnlineMessageTypes.RequestLobbySnapshot),
            "deployed server does not advertise RequestLobbySnapshot");
    }
}

static string SanitizeLogText(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return "unspecified error";
    }
    var sanitized = Regex.Replace(
        value,
        "(?i)(access[_-]?token|refresh[_-]?token|authorization|bearer|password|private[_-]?key)\\s*[:=]?\\s*[^\\s,;]+",
        "$1=<redacted>");
    sanitized = Regex.Replace(sanitized, "(?i)(https?://[^\\s?]+)\\?[^\\s]+", "$1?<redacted-query>");
    return sanitized.Replace('\r', ' ').Replace('\n', ' ').Trim();
}

static string Short(string value)
{
    return value.Length <= 8 ? value : value[..8];
}

internal sealed record AuthRegisterRequest(string UserName, string DisplayName, string Password, string ClientName);

internal sealed record AuthLoginRequest(string UserName, string Password, string ClientName);

internal sealed class AuthTokenResponse
{
    public bool Success { get; set; }
    public string PlayerId { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string ErrorCode { get; set; } = "";
}

internal static class OnlineUxSmokeScenarios
{
    public const string Play = "play";
    public const string Resume = "resume";
    public const string Spectator = "spectator";
    public const string Lobby = "lobby";
    public const string All = "all";
}

internal static class OnlineUxSmokeProfiles
{
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        "classic-six-side-3d-8x8x8-v0.1",
        "single-side-3d-8x8x8-v0.1",
        "asgard-convergence-3d-8x8x8-v0.1",
        "rubik-convergence-3d-8x8x8-v0.1",
        "hodge-projection-duel-3d-8x8x8-v0.1"
    };
}

internal sealed record SmokeCapabilities(
    bool ResumeMatch,
    bool SpectatorMode,
    bool LobbySnapshot,
    HashSet<string> SupportedHubMethods)
{
    public static SmokeCapabilities Parse(JsonElement diagnostics)
    {
        return new SmokeCapabilities(
            ReadBool(diagnostics, "resumeMatch"),
            ReadBool(diagnostics, "spectatorMode"),
            ReadBool(diagnostics, "lobbySnapshot"),
            ReadStringSet(diagnostics, "supportedHubMethods"));
    }

    private static bool ReadBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.True;
    }

    private static HashSet<string> ReadStringSet(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? "")
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

internal sealed record SmokeOptions(
    string BaseUrl,
    string ProfileId,
    string Scenario,
    string RunId,
    bool NoSecretLog,
    int TimeoutSeconds,
    int FromX,
    int FromY,
    int FromZ,
    int ToX,
    int ToY,
    int ToZ,
    bool SkipActionSubmit)
{
    public bool ShouldRun(string scenario)
    {
        return Scenario.Equals(OnlineUxSmokeScenarios.All, StringComparison.OrdinalIgnoreCase) ||
            Scenario.Equals(scenario, StringComparison.OrdinalIgnoreCase);
    }

    public static SmokeOptions Parse(string[] args)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "base-url", "profile-id", "ruleset", "scenario", "run-id", "timeout",
            "from-x", "from-y", "from-z", "to-x", "to-y", "to-z",
            "skip-action-submit", "no-secret-log"
        };
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected positional argument '{args[i]}'.");
            }
            var key = args[i][2..];
            if (!known.Contains(key))
            {
                throw new ArgumentException($"Unsupported argument '--{key}'.");
            }
            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";
            map[key] = value;
        }

        var baseUrl = Required(map, "base-url").TrimEnd('/');
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("--base-url must be an absolute HTTP or HTTPS URL.");
        }
        var profileId = map.GetValueOrDefault("profile-id", map.GetValueOrDefault("ruleset", "asgard-convergence-3d-8x8x8-v0.1"));
        if (!OnlineUxSmokeProfiles.All.Contains(profileId))
        {
            throw new ArgumentException($"Unsupported profile '{profileId}'. Expected one of the five tracked Chess3D profiles.");
        }
        var runId = map.GetValueOrDefault("run-id", Guid.NewGuid().ToString("N")).Trim();
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("--run-id cannot be empty.");
        }
        return new SmokeOptions(
            baseUrl,
            profileId,
            NormalizeScenario(map.GetValueOrDefault("scenario", OnlineUxSmokeScenarios.All)),
            runId,
            map.ContainsKey("no-secret-log"),
            BoundedInt(map, "timeout", 180, 10, 900),
            Int(map, "from-x", 2),
            Int(map, "from-y", 3),
            Int(map, "from-z", 0),
            Int(map, "to-x", 2),
            Int(map, "to-y", 3),
            Int(map, "to-z", 1),
            map.ContainsKey("skip-action-submit"));
    }

    private static string NormalizeScenario(string scenario)
    {
        var normalized = scenario.Trim().ToLowerInvariant();
        return normalized switch
        {
            OnlineUxSmokeScenarios.Play or
            OnlineUxSmokeScenarios.Resume or
            OnlineUxSmokeScenarios.Spectator or
            OnlineUxSmokeScenarios.Lobby or
            OnlineUxSmokeScenarios.All => normalized,
            _ => throw new ArgumentException($"Unsupported --scenario '{scenario}'. Expected play, resume, spectator, lobby, or all.")
        };
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

    private static int BoundedInt(Dictionary<string, string> map, string key, int fallback, int minimum, int maximum)
    {
        var value = Int(map, key, fallback);
        if (value < minimum || value > maximum)
        {
            throw new ArgumentException($"--{key} must be between {minimum} and {maximum}.");
        }
        return value;
    }
}
