using System.Text.Json;
using ChessOnlineClient;
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
    OnlineClientSdkTests(test);
    AuthorityRuntimeDiagnosticsTests(test, profileRoot);
    AuthorityClassicTests(test, profileRoot);
    AuthorityProfileSmokeTests(test, profileRoot);
    LegalPreviewAuthorityTests(test, profileRoot);
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

    var onlineDiagnostics = registry.GetDiagnostics();
    test.Check(onlineDiagnostics.RequestLegalPreviewSupported, "online diagnostics exposes legal preview capability");
    test.Check(onlineDiagnostics.RealtimeResyncSupported, "online diagnostics exposes realtime resync capability");
    test.Check(onlineDiagnostics.ActionLogSupported, "online diagnostics exposes action log capability");
    test.Check(onlineDiagnostics.MatchmakingSupported, "online diagnostics exposes matchmaking capability");
    test.Check(onlineDiagnostics.SupportedHubMethods.Contains(OnlineMessageTypes.RequestLegalPreview), "online diagnostics lists RequestLegalPreview hub method");
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

    var emptyPreview = OnlineProtocolJson.Wrap(OnlineMessageTypes.LegalPreviewResult, "client-a", "player-a");
    emptyPreview.LegalPreview = new OnlineLegalPreviewResult
    {
        RoomId = "room-a",
        TableId = "table-a",
        RulesetId = "classic-six-side-3d-8x8x8-v0.1",
        StateHash = "hash-a",
        SourceX = 0,
        SourceY = 0,
        SourceZ = 0,
        ActorSide = 1,
        NoLegalActionReason = "empty source"
    };
    var emptyPreviewJson = OnlineProtocolJson.Serialize(emptyPreview);
    test.Check(OnlineProtocolJson.TryDeserialize(emptyPreviewJson, out var parsedEmptyPreview, out error) &&
        parsedEmptyPreview.Envelope.MessageType == OnlineMessageTypes.LegalPreviewResult &&
        parsedEmptyPreview.LegalPreview?.Options.Count == 0 &&
        parsedEmptyPreview.LegalPreview.NoLegalActionReason.Contains("empty", StringComparison.OrdinalIgnoreCase),
        "empty legal preview result serializes");

    var previewRequest = OnlineProtocolJson.Wrap(OnlineMessageTypes.RequestLegalPreview, "client-a", "player-a");
    previewRequest.LegalPreviewRequest = new OnlineLegalPreviewRequest
    {
        PlayerId = "player-a",
        RoomId = "room-a",
        TableId = "table-a",
        SourceX = 2,
        SourceY = 3,
        SourceZ = 0,
        ActorSide = 1,
        ExpectedStateHash = "hash-a"
    };
    var previewRequestJson = OnlineProtocolJson.Serialize(previewRequest);
    test.Check(OnlineProtocolJson.TryDeserialize(previewRequestJson, out var parsedPreviewRequest, out error) &&
        parsedPreviewRequest.Envelope.MessageType == OnlineMessageTypes.RequestLegalPreview &&
        parsedPreviewRequest.LegalPreviewRequest?.SourceX == 2 &&
        parsedPreviewRequest.LegalPreviewRequest.ExpectedStateHash == "hash-a",
        "legal preview request serializes");

    var preview = OnlineProtocolJson.Wrap(OnlineMessageTypes.LegalPreviewResult, "client-a", "player-a");
    preview.LegalPreview = new OnlineLegalPreviewResult
    {
        RoomId = "room-a",
        TableId = "table-a",
        RulesetId = "asgard-convergence-3d-8x8x8-v0.1",
        StateHash = "hash-b",
        ServerSeq = 42,
        SourceX = 2,
        SourceY = 3,
        SourceZ = 0,
        ActorSide = 1,
        Options =
        {
            new OnlineLegalActionOption
            {
                ActionKind = OnlineActionKinds.NormalMove,
                ActorSide = 1,
                From = new OnlineLegalTarget { X = 2, Y = 3, Z = 0 },
                To = new OnlineLegalTarget { X = 2, Y = 3, Z = 1 },
                DisplayLabel = "S1P (2,3,0)->(2,3,1)",
                PieceCode = 11
            },
            new OnlineLegalActionOption
            {
                ActionKind = OnlineActionKinds.RubikLayerTurn,
                ActorSide = 1,
                Axis = 0,
                Layer = 0,
                QuarterTurns = 1,
                IsSpecial = true,
                Capability = "layerTurn"
            },
            new OnlineLegalActionOption
            {
                ActionKind = OnlineActionKinds.HodgeProjectedMove,
                ActorSide = 1,
                MacroPlayer = 1,
                PrimarySide = 1,
                From = new OnlineLegalTarget { X = 2, Y = 3, Z = 0 },
                To = new OnlineLegalTarget { X = 2, Y = 3, Z = 1 },
                IsSpecial = true,
                Capability = "projectionComposite"
            },
            new OnlineLegalActionOption
            {
                ActionKind = OnlineActionKinds.ReserveRestore,
                Side = 1,
                PieceType = 1,
                ReserveTarget = new OnlineLegalTarget { X = 0, Y = 0, Z = 0 },
                IsSpecial = true,
                Capability = "reserveRestore"
            }
        }
    };
    var previewJson = OnlineProtocolJson.Serialize(preview);
    test.Check(OnlineProtocolJson.TryDeserialize(previewJson, out var parsedPreview, out error) &&
        parsedPreview.LegalPreview?.Options.Count == 4 &&
        parsedPreview.LegalPreview.Options.Any(o => o.ActionKind == OnlineActionKinds.NormalMove && o.To.Z == 1) &&
        parsedPreview.LegalPreview.Options.Any(o => o.ActionKind == OnlineActionKinds.RubikLayerTurn && o.Layer == 0) &&
        parsedPreview.LegalPreview.Options.Any(o => o.ActionKind == OnlineActionKinds.HodgeProjectedMove && o.PrimarySide == 1) &&
        parsedPreview.LegalPreview.Options.Any(o => o.ActionKind == OnlineActionKinds.ReserveRestore && o.ReserveTarget.X == 0),
        "legal preview options represent normal, Rubik, Hodge, and reserve actions");
}

static void OnlineClientSdkTests(ContractTest test)
{
    var endpoint = ChessOnlineServerEndpoint.FromBaseUrl("http://example.test/chess3d/relay/");
    test.Check(endpoint.ToString() == "http://example.test", "online client endpoint normalizes hub URL to base URL");
    test.Check(endpoint.HubUri.ToString() == "http://example.test/chess3d/relay", "online client endpoint computes hub URL");
    test.Check(endpoint.LiveHealthUri.ToString() == "http://example.test/healthz/live", "online client endpoint computes live health URL");
    test.Check(endpoint.ReadyHealthUri.ToString() == "http://example.test/healthz/ready", "online client endpoint computes ready health URL");
    test.Check(endpoint.DiagnosticsUri.ToString() == "http://example.test/chess3d/diagnostics", "online client endpoint computes diagnostics URL");
    test.Check(endpoint.IsDiagnosticHttp, "online client flags non-loopback HTTP as diagnostic-only");

    var local = ChessOnlineServerEndpoint.FromBaseUrl("http://127.0.0.1:5077");
    test.Check(!local.IsDiagnosticHttp, "online client does not warn for loopback HTTP");

    var redacted = ChessOnlineSecretRedactor.Redact("accessToken=abc refreshToken=def password=s3 Authorization: Bearer secret-token");
    test.Check(!redacted.Contains("abc", StringComparison.Ordinal) &&
        !redacted.Contains("def", StringComparison.Ordinal) &&
        !redacted.Contains("s3", StringComparison.Ordinal) &&
        !redacted.Contains("secret-token", StringComparison.Ordinal), "online client redacts token-like log fields");

    var session = new ChessOnlineClientSession(endpoint, "test-client");
    test.Check(!session.IsAuthenticated && session.RedactedStatus == "anonymous", "online client session starts anonymous");
    session.SetToken(new ChessOnlineAuthTokenResponse
    {
        Success = true,
        PlayerId = "player-123456789",
        UserName = "sdk-user",
        AccessToken = "access-secret",
        RefreshToken = "refresh-secret"
    });
    test.Check(session.IsAuthenticated &&
        session.PlayerId == "player-123456789" &&
        !session.RedactedStatus.Contains("access-secret", StringComparison.Ordinal), "online client session tracks auth without exposing tokens");

    var eventLog = new ChessOnlineClientEventLog();
    eventLog.Add("Bearer token-value accessToken=hidden");
    test.Check(eventLog.Events.Count == 1 &&
        !eventLog.Events[0].Contains("token-value", StringComparison.Ordinal) &&
        !eventLog.Events[0].Contains("hidden", StringComparison.Ordinal), "online client event log redacts secrets");

    var connection = ChessOnlineRelayClient.CreateHubConnection(endpoint, () => "in-memory-token");
    test.Check(connection.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected, "online relay client can be constructed without network");
    connection.DisposeAsync().AsTask().GetAwaiter().GetResult();

    test.Check(ChessOnlineRelayEvents.All.Contains("ReceiveLegalPreviewResult"), "online relay client registers legal preview callback event");

    var previewMessage = OnlineProtocolJson.Wrap(OnlineMessageTypes.LegalPreviewResult, "client-a", "player-a");
    previewMessage.LegalPreview = new OnlineLegalPreviewResult
    {
        StateHash = "hash-preview",
        ServerSeq = 7,
        SourceX = 2,
        SourceY = 3,
        SourceZ = 0,
        ActorSide = 1,
        Options =
        {
            new OnlineLegalActionOption
            {
                ActionKind = OnlineActionKinds.NormalMove,
                ActorSide = 1,
                From = new OnlineLegalTarget { X = 2, Y = 3, Z = 0 },
                To = new OnlineLegalTarget { X = 2, Y = 3, Z = 1 },
                DisplayLabel = "S1P (2,3,0)->(2,3,1)",
                PieceCode = 11
            }
        }
    };
    var previewState = LegalPreviewState.FromMessage(previewMessage);
    test.Check(previewState.Options.Count == 1 &&
        previewState.Targets.Count == 1 &&
        previewState.Targets[0].Z == 1 &&
        previewState.Options[0].Command.ActionKind == OnlineActionKinds.NormalMove &&
        previewState.Options[0].Command.ToZ == 1,
        "online legal preview state builds target markers and submit command");

    var stalePreview = OnlineProtocolJson.Wrap(OnlineMessageTypes.LegalPreviewResult, "client-a", "player-a");
    stalePreview.LegalPreview = new OnlineLegalPreviewResult
    {
        IsStale = true,
        Error = new OnlineLegalPreviewError
        {
            ReasonCode = OnlineRejectReasons.StaleStateHash,
            ReasonText = "Client expected hash does not match authoritative state.",
            RequiresResync = true
        }
    };
    var stalePreviewState = LegalPreviewState.FromMessage(stalePreview);
    test.Check(stalePreviewState.IsStale &&
        stalePreviewState.Reason.Contains("expected hash", StringComparison.OrdinalIgnoreCase), "online legal preview state surfaces stale reason");

    test.Check(OnlinePreviewActionDispatchPolicy.CanSubmitFromGenericBoard(OnlineActionKinds.NormalMove, out var normalReason) &&
        string.IsNullOrWhiteSpace(normalReason), "generic online board dispatch accepts normal move options");
    test.Check(!OnlinePreviewActionDispatchPolicy.CanSubmitFromGenericBoard(OnlineActionKinds.RubikLayerTurn, out var rubikReason) &&
        rubikReason.Contains("Rubik Layer Actions", StringComparison.OrdinalIgnoreCase), "generic online board dispatch rejects Rubik layer options");
    test.Check(!OnlinePreviewActionDispatchPolicy.CanSubmitFromGenericBoard(OnlineActionKinds.HodgeProjectedMove, out var hodgeReason) &&
        hodgeReason.Contains("Hodge Projection Actions", StringComparison.OrdinalIgnoreCase), "generic online board dispatch rejects Hodge projection options");
    test.Check(!OnlinePreviewActionDispatchPolicy.CanSubmitFromGenericBoard(OnlineActionKinds.ReserveRestore, out var reserveReason) &&
        reserveReason.Contains("reserve restore", StringComparison.OrdinalIgnoreCase), "generic online board dispatch rejects reserve restore options");
    test.Check(!OnlinePreviewActionDispatchPolicy.CanSubmitFromGenericBoard("FutureAction", out var futureReason) &&
        futureReason.Contains("FutureAction", StringComparison.Ordinal), "generic online board dispatch rejects unknown future action kinds safely");
    test.Check(OnlinePreviewActionDispatchPolicy.ShouldShowRubikLayerPanel("rubik-convergence-3d-8x8x8-v0.1"),
        "Rubik special action panel appears for Rubik profile");
    test.Check(!OnlinePreviewActionDispatchPolicy.ShouldShowRubikLayerPanel("classic-six-side-3d-8x8x8-v0.1"),
        "Rubik special action panel does not appear for Classic profile");
    test.Check(OnlinePreviewActionDispatchPolicy.ShouldShowHodgeProjectionPanel("hodge-projection-duel-3d-8x8x8-v0.1"),
        "Hodge projection panel appears for Hodge profile");
    test.Check(!OnlinePreviewActionDispatchPolicy.ShouldShowHodgeProjectionPanel("classic-six-side-3d-8x8x8-v0.1"),
        "Hodge projection panel does not appear for Classic profile");

    var disconnectedTurn = OnlineSeatTurnState.Empty();
    test.Check(!disconnectedTurn.CanPrimaryAct &&
        disconnectedTurn.Summary.Contains("canAct=no", StringComparison.OrdinalIgnoreCase), "online seat turn state handles disconnected UI");

    var noMatchTurn = OnlineSeatTurnState.FromMatch(
        "classic-six-side-3d-8x8x8-v0.1",
        "player-a",
        "player-b",
        primarySeatIndex: 0,
        opponentSeatIndex: 0,
        board: null);
    test.Check(!noMatchTurn.CanPrimaryAct &&
        noMatchTurn.DisabledReason.Contains("seat", StringComparison.OrdinalIgnoreCase), "online seat turn state explains missing seat");

    var noSnapshotTurn = OnlineSeatTurnState.FromMatch(
        "classic-six-side-3d-8x8x8-v0.1",
        "player-a",
        "player-b",
        primarySeatIndex: 1,
        opponentSeatIndex: 2,
        board: null);
    test.Check(!noSnapshotTurn.CanPrimaryAct &&
        noSnapshotTurn.DisabledReason.Contains("snapshot", StringComparison.OrdinalIgnoreCase), "online seat turn state explains missing snapshot");

    var classicMyTurn = OnlineSeatTurnState.FromMatch(
        "classic-six-side-3d-8x8x8-v0.1",
        "player-a",
        "player-b",
        primarySeatIndex: 1,
        opponentSeatIndex: 2,
        board: Board("classic-six-side-3d-8x8x8-v0.1", currentSide: 1, currentMacroPlayer: 0));
    test.Check(classicMyTurn.CanPrimaryAct &&
        classicMyTurn.PrimarySideId == 1 &&
        classicMyTurn.Summary.Contains("canAct=yes", StringComparison.OrdinalIgnoreCase), "online seat turn state detects Classic primary turn");

    var classicOpponentTurn = OnlineSeatTurnState.FromMatch(
        "classic-six-side-3d-8x8x8-v0.1",
        "player-a",
        "player-b",
        primarySeatIndex: 1,
        opponentSeatIndex: 2,
        board: Board("classic-six-side-3d-8x8x8-v0.1", currentSide: 2, currentMacroPlayer: 0));
    test.Check(!classicOpponentTurn.CanPrimaryAct &&
        classicOpponentTurn.DisabledReason.Contains("side 2", StringComparison.OrdinalIgnoreCase), "online seat turn state detects opponent side turn");

    var hodgeMyTurn = OnlineSeatTurnState.FromMatch(
        "hodge-projection-duel-3d-8x8x8-v0.1",
        "player-a",
        "player-b",
        primarySeatIndex: 1,
        opponentSeatIndex: 2,
        board: Board("hodge-projection-duel-3d-8x8x8-v0.1", currentSide: 0, currentMacroPlayer: 1));
    test.Check(hodgeMyTurn.CanPrimaryAct &&
        hodgeMyTurn.IsHodge &&
        hodgeMyTurn.PrimaryMacroPlayer == 1, "online seat turn state detects Hodge primary macro turn");

    var hodgeOpponentTurn = OnlineSeatTurnState.FromMatch(
        "hodge-projection-duel-3d-8x8x8-v0.1",
        "player-a",
        "player-b",
        primarySeatIndex: 1,
        opponentSeatIndex: 2,
        board: Board("hodge-projection-duel-3d-8x8x8-v0.1", currentSide: 0, currentMacroPlayer: 2));
    test.Check(!hodgeOpponentTurn.CanPrimaryAct &&
        hodgeOpponentTurn.DisabledReason.Contains("macro-player 2", StringComparison.OrdinalIgnoreCase), "online seat turn state detects Hodge opponent macro turn");

    var statusTurn = OnlineSeatTurnState.FromStatus(new OnlineMatchmakingStatus
    {
        State = "Matched",
        RoomId = "room-a",
        TableId = "table-a",
        Tickets =
        {
            new OnlineMatchmakingTicket { PlayerId = "player-a", SeatIndex = 1, RequestedRulesetId = "classic-six-side-3d-8x8x8-v0.1" },
            new OnlineMatchmakingTicket { PlayerId = "player-b", SeatIndex = 2, RequestedRulesetId = "classic-six-side-3d-8x8x8-v0.1" }
        }
    }, "player-a", "player-b", Board("classic-six-side-3d-8x8x8-v0.1", currentSide: 1, currentMacroPlayer: 0));
    test.Check(statusTurn.PrimarySeatIndex == 1 &&
        statusTurn.OpponentSeatIndex == 2 &&
        statusTurn.CanPrimaryAct, "online seat turn state derives seats from matchmaking status");

    var windowA = new ChessOnlineClientSession(endpoint, "window-a");
    var windowB = new ChessOnlineClientSession(endpoint, "window-b");
    windowA.SetToken(new ChessOnlineAuthTokenResponse { Success = true, PlayerId = "player-window-a", UserName = "a", AccessToken = "token-a" });
    windowB.SetToken(new ChessOnlineAuthTokenResponse { Success = true, PlayerId = "player-window-b", UserName = "b", AccessToken = "token-b" });
    test.Check(windowA.PlayerId == "player-window-a" &&
        windowB.PlayerId == "player-window-b" &&
        windowA.PlayerId != windowB.PlayerId, "two-window client sessions keep independent player ids");

    var logA = new ChessOnlineClientEventLog();
    var logB = new ChessOnlineClientEventLog();
    logA.Add("window A event accessToken=secret-a");
    logB.Add("window B event accessToken=secret-b");
    test.Check(logA.Events.Count == 1 &&
        logB.Events.Count == 1 &&
        logA.Events[0].Contains("window A", StringComparison.Ordinal) &&
        logB.Events[0].Contains("window B", StringComparison.Ordinal) &&
        !logA.Events[0].Contains("secret-a", StringComparison.Ordinal) &&
        !logB.Events[0].Contains("secret-b", StringComparison.Ordinal), "two-window event logs stay separate and redacted");

    var realtime = new OnlineRealtimeSyncState();
    var event1 = OnlineProtocolJson.Wrap(OnlineMessageTypes.ActionAccepted, "client-a", "player-a");
    event1.Envelope.ServerSeq = 1;
    var observed1 = realtime.Observe(event1);
    test.Check(!observed1.IsDuplicate &&
        !observed1.HasGap &&
        realtime.LastServerSeq == 1, "online realtime sync accepts first sequenced event");

    var duplicate = OnlineProtocolJson.Wrap(OnlineMessageTypes.ActionAccepted, "client-a", "player-a");
    duplicate.Envelope.ServerSeq = 1;
    var observedDuplicate = realtime.Observe(duplicate);
    test.Check(observedDuplicate.IsDuplicate &&
        realtime.DuplicateEventCount == 1 &&
        realtime.LastServerSeq == 1, "online realtime sync detects duplicate events without advancing seq");

    var gap = OnlineProtocolJson.Wrap(OnlineMessageTypes.ActionAccepted, "client-a", "player-a");
    gap.Envelope.ServerSeq = 4;
    var observedGap = realtime.Observe(gap);
    test.Check(observedGap.HasGap &&
        observedGap.RequiresResync &&
        realtime.GapEventCount == 1 &&
        realtime.LastServerSeq == 4, "online realtime sync detects server seq gaps");

    var stale = OnlineProtocolJson.Wrap(OnlineMessageTypes.ResyncRequired, "client-a", "player-a");
    stale.Envelope.ServerSeq = 4;
    stale.Error = OnlineProtocolJson.Error(OnlineRejectReasons.StaleStateHash, "Client expected hash does not match authoritative state.");
    var observedStale = realtime.Observe(stale);
    test.Check(observedStale.IsDuplicate &&
        realtime.ResyncRequired, "online realtime sync keeps resync required after stale duplicate response");

    var snapshotEvent = OnlineProtocolJson.Wrap(OnlineMessageTypes.AuthoritativeSnapshot, "client-a", "player-a");
    snapshotEvent.Envelope.ServerSeq = 5;
    snapshotEvent.Snapshot = new OnlineSnapshot { StateHash = "snapshot-hash-123456", ServerSeq = 5 };
    var observedSnapshot = realtime.Observe(snapshotEvent);
    test.Check(!observedSnapshot.RequiresResync &&
        realtime.LastSnapshotHash == "snapshot-hash-123456" &&
        realtime.Summary.Contains("hash=", StringComparison.Ordinal), "online realtime sync clears resync after fresh snapshot");
}

static OnlineChess3DBoardSnapshot Board(string rulesetId, int currentSide, int currentMacroPlayer)
{
    return new OnlineChess3DBoardSnapshot(
        rulesetId,
        "room-a",
        "table-a",
        serverSeq: 1,
        stateHash: $"hash-{rulesetId}-{currentSide}-{currentMacroPlayer}",
        actionCount: 0,
        lastActionNotation: "",
        width: 8,
        height: 8,
        depth: 8,
        currentSide,
        currentMacroPlayer,
        currentTurnKind: 1,
        projectedBoard: Enumerable.Repeat(0, 512).ToArray());
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

static void LegalPreviewAuthorityTests(ContractTest test, string profileRoot)
{
    var classic = StartedRegistry(profileRoot, "room-preview-classic", "preview-classic", "classic-six-side-3d-8x8x8-v0.1", 1, out var classicEnv);
    var command = classic.BuildFirstLegalNormalMoveCommand("room-preview-classic", "preview-classic", 1);
    test.Check(command != null, "online authority can build source command for legal preview test");
    var before = classic.RequestSnapshot(classicEnv(OnlineMessageTypes.RequestSnapshot)).Snapshot?.StateHash ?? "";
    var preview = classic.RequestLegalPreview(classicEnv(OnlineMessageTypes.RequestLegalPreview), new OnlineLegalPreviewRequest
    {
        SourceX = command?.FromX ?? 0,
        SourceY = command?.FromY ?? 0,
        SourceZ = command?.FromZ ?? 0,
        ActorSide = 1,
        ExpectedStateHash = before
    });
    var after = classic.RequestSnapshot(classicEnv(OnlineMessageTypes.RequestSnapshot)).Snapshot?.StateHash ?? "";
    test.Check(preview.Envelope.MessageType == OnlineMessageTypes.LegalPreviewResult &&
        preview.LegalPreview?.Options.Any(o =>
            o.ActionKind == OnlineActionKinds.NormalMove &&
            o.From.X == command?.FromX &&
            o.From.Y == command?.FromY &&
            o.From.Z == command?.FromZ &&
            o.To.X == command?.ToX &&
            o.To.Y == command?.ToY &&
            o.To.Z == command?.ToZ) == true,
        "legal preview returns matching Classic normal move option");
    test.Check(after == before, "legal preview does not mutate Classic state hash");

    var stale = classic.RequestLegalPreview(classicEnv(OnlineMessageTypes.RequestLegalPreview), new OnlineLegalPreviewRequest
    {
        SourceX = command?.FromX ?? 0,
        SourceY = command?.FromY ?? 0,
        SourceZ = command?.FromZ ?? 0,
        ActorSide = 1,
        ExpectedStateHash = "stale"
    });
    test.Check(stale.Envelope.MessageType == OnlineMessageTypes.LegalPreviewResult &&
        stale.LegalPreview?.IsStale == true &&
        stale.LegalPreview.Error?.ReasonCode == OnlineRejectReasons.StaleStateHash,
        "stale legal preview returns explicit resync hint");

    var empty = classic.RequestLegalPreview(classicEnv(OnlineMessageTypes.RequestLegalPreview), new OnlineLegalPreviewRequest
    {
        SourceX = 4,
        SourceY = 4,
        SourceZ = 4,
        ActorSide = 1,
        ExpectedStateHash = before
    });
    test.Check(empty.Envelope.MessageType == OnlineMessageTypes.LegalPreviewResult &&
        empty.LegalPreview?.Options.Count == 0 &&
        !string.IsNullOrWhiteSpace(empty.LegalPreview.NoLegalActionReason),
        "empty source legal preview returns clear no-action reason");

    var asgard = StartedRegistry(profileRoot, "room-preview-asgard", "preview-asgard", "asgard-convergence-3d-8x8x8-v0.1", 1, out var asgardEnv);
    var asgardCommand = asgard.BuildFirstLegalNormalMoveCommand("room-preview-asgard", "preview-asgard", 1);
    test.Check(asgardCommand != null, "online authority can build source Asgard command for legal preview test");
    var asgardHash = asgard.RequestSnapshot(asgardEnv(OnlineMessageTypes.RequestSnapshot)).Snapshot?.StateHash ?? "";
    var asgardPreview = asgard.RequestLegalPreview(asgardEnv(OnlineMessageTypes.RequestLegalPreview), new OnlineLegalPreviewRequest
    {
        SourceX = asgardCommand?.FromX ?? 0,
        SourceY = asgardCommand?.FromY ?? 0,
        SourceZ = asgardCommand?.FromZ ?? 0,
        ActorSide = 1,
        ExpectedStateHash = asgardHash
    });
    var asgardAfter = asgard.RequestSnapshot(asgardEnv(OnlineMessageTypes.RequestSnapshot)).Snapshot?.StateHash ?? "";
    test.Check(asgardPreview.Envelope.MessageType == OnlineMessageTypes.LegalPreviewResult &&
        asgardPreview.LegalPreview?.Options.Any(o => o.ActionKind == OnlineActionKinds.NormalMove) == true,
        "Asgard source legal preview returns at least one normal move option");
    test.Check(asgardAfter == asgardHash, "legal preview does not mutate Asgard state hash");
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
    test.Check(OnlineChess3DBoardSnapshotParser.TryParse(snapshot.Snapshot, out var board, out var boardError),
        $"online board snapshot parses from savegame: {boardError}");
    test.Check(board.Width == 8 && board.Height == 8 && board.Depth == 8 &&
        board.Cells.Count == 512, "online board snapshot exposes 8x8x8 projected cells");
    test.Check(board.CurrentSide >= 1 && board.GetCell(0, 0, 0).Index == 0 &&
        board.GetCell(7, 7, 7).Index == 511, "online board snapshot uses engine cell indexing");
    test.Check(board.OccupiedCells.Any(), "online board snapshot exposes occupied cells");
    var malformedSnapshot = new OnlineSnapshot { RulesetId = "classic-six-side-3d-8x8x8-v0.1", SaveGameJson = "{ not json }" };
    test.Check(!OnlineChess3DBoardSnapshotParser.TryParse(malformedSnapshot, out _, out var malformedError) &&
        malformedError.Contains("invalid", StringComparison.OrdinalIgnoreCase), "malformed online board snapshot fails cleanly");

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
