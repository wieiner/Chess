using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using ChessApp;
using ChessOnlineClient;
using ChessOnlineProtocol;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChessOnlineApp;

public partial class MainWindow : Window
{
    private readonly IIntegrationAccountStore _accountStore = new JsonIntegrationAccountStore();
    private readonly ObservableCollection<IntegrationAccountProfile> _accountProfiles = new();
    private readonly OnlineRoomRegistry _p3eRegistry;
    private IcsTextChessClient? _icsClient;
    private Chess3DInternetRelayClient? _relayClient;
    private HubConnection? _p3fConnection;
    private string _p3fSessionToken = "";
    private ChessOnlineClientSession? _p4fPrimarySession;
    private ChessOnlineClientSession? _p4fSecondarySession;
    private ChessOnlineRelayClient? _p4fPrimaryRelay;
    private ChessOnlineRelayClient? _p4fSecondaryRelay;
    private string _p4fRoomId = "";
    private string _p4fTableId = "";
    private int _p4fPrimarySeatIndex;
    private int _p4fSecondarySeatIndex;
    private OnlineMatchmakingStatus? _p4fLastMatchmakingStatus;
    private OnlineSnapshot? _p4fLastSnapshot;
    private OnlineChess3DBoardSnapshot? _p4gBoardSnapshot;
    private OnlineChess3DBoardCell? _p4gSelectedCell;
    private OnlineChess3DBoardCell? _p4gMoveFrom;
    private OnlineChess3DBoardCell? _p4gMoveTo;
    private LegalPreviewState _p4gLegalPreview = LegalPreviewState.Empty();
    private OnlineSeatTurnState _p4fSeatTurnState = OnlineSeatTurnState.Empty();
    private OnlineRealtimeSyncState _p4fRealtimeSync = new();
    private bool _p4fResyncRefreshPending;
    private bool _p4gSubmitPending;
    private int _p4fAcceptedActionCount;
    private int _p4fRejectedActionCount;
    private long _p4fLastServerSeq;
    private const string P4FHetznerHttpBaseUrl = "http://178.105.220.117";

    public MainWindow()
    {
        InitializeComponent();
        _p3eRegistry = new OnlineRoomRegistry(ResolveP3EProfileRoot());
        PortalGrid.ItemsSource = ChessPortalRegistry.All;
        AccountListBox.ItemsSource = _accountProfiles;
        AccountStorePathText.Text = $"Store: {_accountStore.StorePath}";
        PortalGrid.SelectedIndex = 0;
        _ = ReloadAccountsAsync();
        Log("Online hub started. Main chess boards are intentionally separate.");
        P3EStatusText.Text = "P3E local authority harness idle.";
        RenderP4GBoard();
        UpdateP4FSeatTurnStatus();
        UpdateP4FRealtimeStatus();
        UpdateP4GSpecialActionPanels();
    }

    protected override void OnClosed(EventArgs e)
    {
        _icsClient?.Dispose();
        _relayClient?.Dispose();
        if (_p3fConnection != null)
        {
            _p3fConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        if (_p4fPrimaryRelay != null)
        {
            _p4fPrimaryRelay.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        if (_p4fSecondaryRelay != null)
        {
            _p4fSecondaryRelay.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        base.OnClosed(e);
    }

    private void PortalGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PortalGrid.SelectedItem is not ChessPortalDescriptor portal)
        {
            return;
        }

        PortalTitle.Text = portal.DisplayName;
        PortalHome.Text = portal.HomeUri.ToString();
        PortalNotes.Text = portal.Notes;
    }

    private async void SavePortalAccount_Click(object sender, RoutedEventArgs e)
    {
        if (PortalGrid.SelectedItem is not ChessPortalDescriptor portal)
        {
            Log("Select a portal first.");
            return;
        }

        if (string.Equals(portal.Id, "chessadvisor3d", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate(RelayUriBox.Text.Trim(), UriKind.Absolute, out var relayUri))
        {
            await SaveRelayProfileAsync(relayUri, "portal profile");
            return;
        }

        var username = AccountUserBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            username = GuessUsernameForPortal(portal.Id);
        }

        var profile = IntegrationProfileFactory.FromPortal(
            portal,
            username,
            AccountAliasBox.Text.Trim(),
            hasSecret: false,
            accessMode: GuessAccessModeForPortal(portal.Id));

        await SaveProfileAsync(profile, "manual portal profile");
    }

    private async void ReloadAccounts_Click(object sender, RoutedEventArgs e)
    {
        await ReloadAccountsAsync();
    }

    private async void LichessCheck_Click(object sender, RoutedEventArgs e)
    {
        var token = LichessTokenBox.Password.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            Log("Lichess token is empty.");
            return;
        }

        try
        {
            using var client = new LichessClient(token);
            var json = await client.GetAccountJsonAsync();
            Log("Lichess account OK:");
            Log(json);
            var username = TryReadJsonString(json, "username", "id");
            var mode = LichessModeBox.SelectedIndex == 1 ? "bot" : "board";
            await SaveProfileAsync(
                IntegrationProfileFactory.FromPortal(
                    ChessPortalRegistry.Get("lichess"),
                    username,
                    "Lichess",
                    hasSecret: true,
                    accessMode: mode),
                "verified Lichess account");
        }
        catch (Exception ex)
        {
            Log($"Lichess check failed: {ex.Message}");
        }
    }

    private void LichessStreamInfo_Click(object sender, RoutedEventArgs e)
    {
        var mode = LichessModeBox.SelectedIndex == 1 ? "Bot API" : "Board API";
        var gameId = LichessGameIdBox.Text.Trim();
        Log(string.IsNullOrWhiteSpace(gameId)
            ? $"Lichess {mode}: enter a game id to stream."
            : $"Lichess {mode}: stream path prepared for game {gameId}. Long-running stream UI will be wired to the advisor timeline next.");
    }

    private async void ChessComProfile_Click(object sender, RoutedEventArgs e)
    {
        var username = ChessComUserBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            Log("Chess.com username is empty.");
            return;
        }

        try
        {
            using var client = new ChessComPublishedDataClient();
            var json = await client.GetAccountOrProfileJsonAsync(username);
            Log(json);
            await SaveProfileAsync(
                IntegrationProfileFactory.FromPortal(
                    ChessPortalRegistry.Get("chesscom"),
                    TryReadJsonString(json, "username", "player_id") is { Length: > 0 } parsedUsername ? parsedUsername : username,
                    "Chess.com",
                    hasSecret: false),
                "Chess.com public profile");
        }
        catch (Exception ex)
        {
            Log($"Chess.com profile failed: {ex.Message}");
        }
    }

    private async void ChessComDaily_Click(object sender, RoutedEventArgs e)
    {
        var username = ChessComUserBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            Log("Chess.com username is empty.");
            return;
        }

        try
        {
            using var client = new ChessComPublishedDataClient();
            var count = 0;
            await foreach (var game in client.GetCurrentGamesAsync(username))
            {
                count++;
                Log($"Chess.com daily {count}: {game.GameId}");
                Log(game.Fen);
            }
            if (count == 0)
            {
                Log("Chess.com daily: no current public daily games.");
            }
        }
        catch (Exception ex)
        {
            Log($"Chess.com daily failed: {ex.Message}");
        }
    }

    private async void IcsConnect_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(IcsPortBox.Text, out var port))
        {
            port = 5000;
            IcsPortBox.Text = "5000";
        }

        _icsClient?.Dispose();
        _icsClient = new IcsTextChessClient();
        _icsClient.LineReceived += line => Dispatcher.Invoke(() => Log($"ICS> {line}"));
        _icsClient.StatusChanged += message => Dispatcher.Invoke(() => Log(message));
        try
        {
            var host = IcsHostBox.Text.Trim();
            await _icsClient.ConnectAsync(host, port, IcsUserBox.Text.Trim(), IcsPasswordBox.Password);
            var portalId = host.Contains("freechess", StringComparison.OrdinalIgnoreCase) ? "fics" : "icc";
            var profile = IntegrationProfileFactory.FromPortal(
                ChessPortalRegistry.Get(portalId),
                IcsUserBox.Text.Trim(),
                portalId == "fics" ? "FICS" : "ICC",
                hasSecret: !string.IsNullOrWhiteSpace(IcsPasswordBox.Password),
                accessMode: "ics-text");
            profile.Endpoint = $"{host}:{port}";
            await SaveProfileAsync(profile, "ICS connection profile");
        }
        catch (Exception ex)
        {
            Log($"ICS connect failed: {ex.Message}");
        }
    }

    private async void IcsSend_Click(object sender, RoutedEventArgs e)
    {
        if (_icsClient == null)
        {
            Log("ICS is not connected.");
            return;
        }

        await _icsClient.SendMoveAsync(IcsCommandBox.Text);
        Log($"ICS< {IcsCommandBox.Text}");
    }

    private async void RelayConnect_Click(object sender, RoutedEventArgs e)
    {
        if (!Uri.TryCreate(RelayUriBox.Text.Trim(), UriKind.Absolute, out var uri))
        {
            Log("Relay URI is invalid.");
            return;
        }

        var seat = ReadInt(RelaySeatBox, 1, 0, 6);
        var group = ReadInt(RelayGroupBox, 0, 0, 6);
        _relayClient?.Dispose();
        _relayClient = new Chess3DInternetRelayClient();
        _relayClient.StatusChanged += message => Dispatcher.Invoke(() => Log(message));
        _relayClient.EnvelopeReceived += envelope => Dispatcher.Invoke(() => Log($"Relay {envelope.Type}: {envelope.MessageId}"));
        _relayClient.MessageReceived += message => Dispatcher.Invoke(() => Log($"3D message {message.Type}, seat {message.Seat}"));

        try
        {
            await _relayClient.ConnectAsync(new Chess3DRelayConnectOptions
            {
                WebSocketUri = uri,
                RoomId = RelayRoomBox.Text.Trim(),
                Seat = seat,
                GroupSlot = group,
                AccessToken = RelayTokenBox.Password,
                Role = group > 0 ? "group" : "player"
            });
            await SaveRelayProfileAsync(uri, "connected 3D relay profile");
        }
        catch (Exception ex)
        {
            Log($"Relay connect failed: {ex.Message}");
        }
    }

    private async void SaveRelayProfile_Click(object sender, RoutedEventArgs e)
    {
        if (!Uri.TryCreate(RelayUriBox.Text.Trim(), UriKind.Absolute, out var uri))
        {
            Log("Relay URI is invalid.");
            return;
        }

        await SaveRelayProfileAsync(uri, "manual 3D relay profile");
    }

    private void P3ECreateRoom_Click(object sender, RoutedEventArgs e)
    {
        var envelope = P3EEnvelope(OnlineMessageTypes.CreateRoom);
        var result = _p3eRegistry.CreateRoom(envelope, new OnlineRoomCommand
        {
            RoomId = P3ERoomBox.Text.Trim(),
            DisplayName = P3ERoomBox.Text.Trim(),
            MaxTables = 8
        });
        LogP3EResult("CreateRoom", result);
    }

    private void P3ECreateTable_Click(object sender, RoutedEventArgs e)
    {
        EnsureP3ERoomJoined();
        var envelope = P3EEnvelope(OnlineMessageTypes.CreateTable);
        var result = _p3eRegistry.CreateTable(envelope, new OnlineTableCommand
        {
            TableId = P3ETableBox.Text.Trim(),
            RulesetId = SelectedP3ERuleset()
        });
        LogP3EResult("CreateTable", result);
    }

    private void P3EJoinSeat_Click(object sender, RoutedEventArgs e)
    {
        EnsureP3ERoomJoined();
        var envelope = P3EEnvelope(OnlineMessageTypes.JoinTableSeat);
        var result = _p3eRegistry.JoinTableSeat(envelope, new OnlineTableCommand
        {
            SeatIndex = ReadInt(P3ESeatBox, 1, 1, 6)
        });
        LogP3EResult("JoinSeat", result);
    }

    private void P3EReadyStart_Click(object sender, RoutedEventArgs e)
    {
        EnsureP3ERoomJoined();
        var ready = _p3eRegistry.Ready(P3EEnvelope(OnlineMessageTypes.Ready), new OnlineTableCommand
        {
            Ready = true
        });
        LogP3EResult("Ready", ready);
        var started = _p3eRegistry.StartGame(P3EEnvelope(OnlineMessageTypes.StartGame));
        LogP3EResult("StartGame", started);
    }

    private void P3ESubmitMove_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseCell(P3EFromBox.Text, out var from) || !TryParseCell(P3EToBox.Text, out var to))
        {
            Log("P3E move text must be x,y,z.");
            return;
        }

        var seat = ReadInt(P3ESeatBox, 1, 1, 6);
        var result = _p3eRegistry.SubmitAction(P3EEnvelope(OnlineMessageTypes.SubmitAction), new OnlineActionCommand
        {
            ActionKind = OnlineActionKinds.NormalMove,
            ActorSide = seat,
            FromX = from.X,
            FromY = from.Y,
            FromZ = from.Z,
            ToX = to.X,
            ToY = to.Y,
            ToZ = to.Z
        });
        LogP3EResult("SubmitMove", result);
    }

    private void P3ESnapshot_Click(object sender, RoutedEventArgs e)
    {
        var result = _p3eRegistry.RequestSnapshot(P3EEnvelope(OnlineMessageTypes.RequestSnapshot));
        LogP3EResult("Snapshot", result);
    }

    private void P3EActionLog_Click(object sender, RoutedEventArgs e)
    {
        var result = _p3eRegistry.RequestActionLog(P3EEnvelope(OnlineMessageTypes.RequestActionLog));
        LogP3EResult("ActionLog", result);
    }

    private void P3EDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        var diagnostics = _p3eRegistry.GetDiagnostics();
        var json = JsonSerializer.Serialize(diagnostics, OnlineProtocolJson.Options);
        P3EStatusText.Text = json;
        Log(json);
    }

    private void P4FUseHetznerHttp_Click(object sender, RoutedEventArgs e)
    {
        P4FBaseUrlBox.Text = P4FHetznerHttpBaseUrl;
        ApplyP4FEndpointToHubUrl();
        P4FServerStatusText.Text = "Hetzner diagnostic HTTP selected. TLS/443 is intentionally deferred.";
        Log("P4F selected Hetzner HTTP diagnostic endpoint.");
    }

    private async void P4FCheckHealth_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var endpoint = ResolveP4FEndpointForConnect();
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var health = new ChessOnlineHealthClient(http, endpoint);
            var live = await health.GetLiveAsync();
            var ready = await health.GetReadyAsync();
            var warning = endpoint.IsDiagnosticHttp ? " HTTP diagnostic-only." : "";
            P4FServerStatusText.Text = $"live={live.Trim()} ready={ready.Status} profileCount={ready.ProfileCount} auth={ready.AuthEnabled}.{warning}";
            ApplyP4FEndpointToHubUrl(endpoint);
            Log($"P4F health OK: {P4FServerStatusText.Text}");
        }
        catch (Exception ex)
        {
            P4FServerStatusText.Text = $"Health check failed: {ex.Message}";
            Log(P4FServerStatusText.Text);
        }
    }

    private async void P4FCheckDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var endpoint = ResolveP4FEndpoint();
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var health = new ChessOnlineHealthClient(http, endpoint);
            var diagnostics = await health.GetDiagnosticsAsync();
            var d = diagnostics.Diagnostics;
            P4FServerStatusText.Text =
                $"diagnostics auth={diagnostics.AuthEnabled} authority={diagnostics.AuthorityPlatform}/{diagnostics.AuthorityNativeLibraryName} supported={diagnostics.AuthorityIsSupported} " +
                $"active={d.ActiveConnectionCount} rooms={d.RoomCount} tables={d.TableCount} accepted={d.AcceptedActionCount} rejected={d.RejectedActionCount}";
            ApplyP4FEndpointToHubUrl(endpoint);
            Log($"P4F diagnostics OK: {P4FServerStatusText.Text}");
        }
        catch (Exception ex)
        {
            P4FServerStatusText.Text = $"Diagnostics failed: {ex.Message}";
            Log(P4FServerStatusText.Text);
        }
    }

    private async void P4FRegisterTemp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var endpoint = ResolveP4FEndpoint();
            using var http = CreateP4FHttpClient(endpoint);
            var auth = new ChessOnlineAuthClient(http, endpoint);
            var token = await auth.RegisterTemporaryUserAsync(clientName: "ChessOnlineApp-P4F");
            RequireSuccessfulAuth(token, "register temp");
            _p4fPrimarySession = new ChessOnlineClientSession(endpoint, "ChessOnlineApp-P4F");
            _p4fPrimarySession.SetToken(token);
            P4FAuthUserBox.Text = token.UserName;
            P4FAuthPasswordBox.Password = "";
            UpdateP4FAuthStatus("Temporary user registered and authenticated.");
        }
        catch (Exception ex)
        {
            P4FAuthStatusText.Text = $"Register temp failed: {ex.Message}";
            Log(P4FAuthStatusText.Text);
        }
    }

    private async void P4FLogin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var endpoint = ResolveP4FEndpoint();
            var userName = P4FAuthUserBox.Text.Trim();
            var password = P4FAuthPasswordBox.Password;
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Username and password are required for manual login.");
            }

            using var http = CreateP4FHttpClient(endpoint);
            var auth = new ChessOnlineAuthClient(http, endpoint);
            var token = await auth.LoginAsync(userName, password, "ChessOnlineApp-P4F");
            RequireSuccessfulAuth(token, "login");
            _p4fPrimarySession = new ChessOnlineClientSession(endpoint, "ChessOnlineApp-P4F");
            _p4fPrimarySession.SetToken(token);
            UpdateP4FAuthStatus("Login succeeded.");
        }
        catch (Exception ex)
        {
            P4FAuthStatusText.Text = $"Login failed: {ex.Message}";
            Log(P4FAuthStatusText.Text);
        }
    }

    private async void P4FLogout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_p4fPrimarySession?.Token == null || string.IsNullOrWhiteSpace(_p4fPrimarySession.Token.RefreshToken))
            {
                P4FAuthStatusText.Text = "No authenticated session to logout.";
                return;
            }

            using var http = CreateP4FHttpClient(_p4fPrimarySession.Endpoint);
            var auth = new ChessOnlineAuthClient(http, _p4fPrimarySession.Endpoint);
            await auth.LogoutAsync(_p4fPrimarySession.Token.RefreshToken);
            _p4fPrimarySession.ClearToken();
            _p4fPrimarySession = null;
            UpdateP4FAuthStatus("Logged out.");
        }
        catch (Exception ex)
        {
            P4FAuthStatusText.Text = $"Logout failed: {ex.Message}";
            Log(P4FAuthStatusText.Text);
        }
    }

    private async void P4FCreateTwoTestPlayers_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var endpoint = ResolveP4FEndpoint();
            using var http = CreateP4FHttpClient(endpoint);
            var auth = new ChessOnlineAuthClient(http, endpoint);

            var tokenA = await auth.RegisterTemporaryUserAsync("p4f_a", "ChessOnlineApp-P4F-A");
            var tokenB = await auth.RegisterTemporaryUserAsync("p4f_b", "ChessOnlineApp-P4F-B");
            RequireSuccessfulAuth(tokenA, "register player A");
            RequireSuccessfulAuth(tokenB, "register player B");

            _p4fPrimarySession = new ChessOnlineClientSession(endpoint, "ChessOnlineApp-P4F-A");
            _p4fSecondarySession = new ChessOnlineClientSession(endpoint, "ChessOnlineApp-P4F-B");
            _p4fPrimarySession.SetToken(tokenA);
            _p4fSecondarySession.SetToken(tokenB);
            P4FAuthUserBox.Text = tokenA.UserName;
            P4FAuthPasswordBox.Password = "";
            UpdateP4FAuthStatus($"Two temporary players ready: A={ShortId(tokenA.PlayerId)} B={ShortId(tokenB.PlayerId)}.");
        }
        catch (Exception ex)
        {
            P4FAuthStatusText.Text = $"Create two test players failed: {ex.Message}";
            Log(P4FAuthStatusText.Text);
        }
    }

    private void P4FClearSession_Click(object sender, RoutedEventArgs e)
    {
        _p4fPrimaryRelay?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _p4fSecondaryRelay?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _p4fPrimaryRelay = null;
        _p4fSecondaryRelay = null;
        _p4fPrimarySession = null;
        _p4fSecondarySession = null;
        _p3fSessionToken = "";
        _p4fRoomId = "";
        _p4fTableId = "";
        _p4fPrimarySeatIndex = 0;
        _p4fSecondarySeatIndex = 0;
        _p4fLastMatchmakingStatus = null;
        _p4fSeatTurnState = OnlineSeatTurnState.Empty();
        _p4fRealtimeSync = new OnlineRealtimeSyncState();
        _p4fResyncRefreshPending = false;
        _p4fLastSnapshot = null;
        _p4gBoardSnapshot = null;
        _p4gSelectedCell = null;
        _p4gMoveFrom = null;
        _p4gMoveTo = null;
        ClearP4GLegalPreview("Legal preview: session cleared.");
        _p4gSubmitPending = false;
        _p4fAcceptedActionCount = 0;
        _p4fRejectedActionCount = 0;
        _p4fLastServerSeq = 0;
        P4FActionLogList.Items.Clear();
        P4FSnapshotStatusText.Text = "Snapshot: none.";
        RenderP4GBoard();
        UpdateP4FSeatTurnStatus();
        UpdateP4FRealtimeStatus();
        UpdateP4FActionCounters();
        P4FAuthPasswordBox.Password = "";
        P4FMatchStatusText.Text = "Match status: none.";
        UpdateP4FAuthStatus("Session cleared.");
    }

    private async void P3FConnect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_p3fConnection != null)
            {
                await _p3fConnection.DisposeAsync();
            }

            var endpoint = ResolveP4FEndpointForConnect();
            ApplyP4FEndpointToHubUrl(endpoint);
            _p3fConnection = new HubConnectionBuilder()
                .WithUrl(endpoint.HubUri, options =>
                {
                    if (_p4fPrimarySession?.Token is { AccessToken.Length: > 0 } token)
                    {
                        options.AccessTokenProvider = () => Task.FromResult<string?>(token.AccessToken);
                    }
                })
                .Build();
            RegisterP3FEvents(_p3fConnection);
            await _p3fConnection.StartAsync();
            P3FStatusText.Text = "SignalR connected.";
            Log($"P3F SignalR connected: {endpoint.HubUri}");
        }
        catch (Exception ex)
        {
            P3FStatusText.Text = $"SignalR connect failed: {ex.Message}";
            Log(P3FStatusText.Text);
        }
    }

    private async void P3FDisconnect_Click(object sender, RoutedEventArgs e)
    {
        if (_p3fConnection == null)
        {
            return;
        }
        await _p3fConnection.DisposeAsync();
        _p3fConnection = null;
        P3FStatusText.Text = "SignalR disconnected.";
        Log("P3F SignalR disconnected.");
    }

    private async void P3FHello_Click(object sender, RoutedEventArgs e)
    {
        var result = await P3FInvokeAsync("Hello", P3FMessage(OnlineMessageTypes.Hello));
        if (!string.IsNullOrWhiteSpace(result.Envelope.SessionToken))
        {
            _p3fSessionToken = result.Envelope.SessionToken;
        }
    }

    private async void P3FCreateRoom_Click(object sender, RoutedEventArgs e)
    {
        var message = P3FMessage(OnlineMessageTypes.CreateRoom);
        message.Room = new OnlineRoomCommand
        {
            RoomId = P3ERoomBox.Text.Trim(),
            DisplayName = P3ERoomBox.Text.Trim(),
            MaxTables = 8
        };
        await P3FInvokeAsync("CreateRoom", message);
    }

    private async void P3FCreateTable_Click(object sender, RoutedEventArgs e)
    {
        await EnsureP3FHelloAsync();
        await P3FInvokeAsync("JoinRoom", P3FMessage(OnlineMessageTypes.JoinRoom));
        var message = P3FMessage(OnlineMessageTypes.CreateTable);
        message.Table = new OnlineTableCommand
        {
            TableId = P3ETableBox.Text.Trim(),
            RulesetId = SelectedP3ERuleset()
        };
        await P3FInvokeAsync("CreateTable", message);
    }

    private async void P3FJoinSeat_Click(object sender, RoutedEventArgs e)
    {
        await EnsureP3FHelloAsync();
        await P3FInvokeAsync("JoinRoom", P3FMessage(OnlineMessageTypes.JoinRoom));
        var message = P3FMessage(OnlineMessageTypes.JoinTableSeat);
        message.Table = new OnlineTableCommand { SeatIndex = ReadInt(P3ESeatBox, 1, 1, 6) };
        await P3FInvokeAsync("JoinTableSeat", message);
    }

    private async void P3FReadyStart_Click(object sender, RoutedEventArgs e)
    {
        var ready = P3FMessage(OnlineMessageTypes.Ready);
        ready.Table = new OnlineTableCommand { Ready = true };
        await P3FInvokeAsync("Ready", ready);
        await P3FInvokeAsync("StartGame", P3FMessage(OnlineMessageTypes.StartGame));
    }

    private async void P3FSubmitMove_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseCell(P3EFromBox.Text, out var from) || !TryParseCell(P3EToBox.Text, out var to))
        {
            Log("P3F move text must be x,y,z.");
            return;
        }

        var message = P3FMessage(OnlineMessageTypes.SubmitAction);
        var seat = ReadInt(P3ESeatBox, 1, 1, 6);
        message.Action = new OnlineActionCommand
        {
            ActionKind = OnlineActionKinds.NormalMove,
            ActorSide = seat,
            FromX = from.X,
            FromY = from.Y,
            FromZ = from.Z,
            ToX = to.X,
            ToY = to.Y,
            ToZ = to.Z
        };
        await P3FInvokeAsync("SubmitAction", message);
    }

    private async void P3FSnapshot_Click(object sender, RoutedEventArgs e)
    {
        await P3FInvokeAsync("RequestSnapshot", P3FMessage(OnlineMessageTypes.RequestSnapshot));
    }

    private async void P3FActionLog_Click(object sender, RoutedEventArgs e)
    {
        await P3FInvokeAsync("RequestActionLog", P3FMessage(OnlineMessageTypes.RequestActionLog));
    }

    private async void P3FDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        await P3FInvokeAsync("Diagnostics", P3FMessage(OnlineMessageTypes.RequestDiagnostics));
    }

    private async void P3FJoinMatchmaking_Click(object sender, RoutedEventArgs e)
    {
        await EnsureP3FHelloAsync();
        var message = P3FMessage(OnlineMessageTypes.JoinMatchmaking);
        message.Matchmaking = new OnlineMatchmakingCommand
        {
            RequestedRulesetId = SelectedP3FMatchmakingRuleset(),
            ExpireSeconds = 120
        };
        await P3FInvokeAsync("JoinMatchmaking", message);
    }

    private async void P3FCancelMatchmaking_Click(object sender, RoutedEventArgs e)
    {
        await EnsureP3FHelloAsync();
        await P3FInvokeAsync("CancelMatchmaking", P3FMessage(OnlineMessageTypes.CancelMatchmaking));
    }

    private async void P3FMatchmakingStatus_Click(object sender, RoutedEventArgs e)
    {
        await EnsureP3FHelloAsync();
        await P3FInvokeAsync("GetMatchmakingStatus", P3FMessage(OnlineMessageTypes.GetMatchmakingStatus));
    }

    private async void P4FCreateTestMatch_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await EnsureP4FTwoSessionsAsync();
            await ResetP4FRelaysAsync();

            _p4fPrimaryRelay = new ChessOnlineRelayClient(_p4fPrimarySession!);
            _p4fSecondaryRelay = new ChessOnlineRelayClient(_p4fSecondarySession!);
            _p4fPrimaryRelay.MessageReceived += P4FRelayMessageReceived;
            _p4fSecondaryRelay.MessageReceived += P4FRelayMessageReceived;
            await _p4fPrimaryRelay.ConnectAsync();
            await _p4fSecondaryRelay.ConnectAsync();
            await _p4fPrimaryRelay.HelloAsync("p4f-client-a");
            await _p4fSecondaryRelay.HelloAsync("p4f-client-b");
            _p4fRealtimeSync.MarkConnectionState("single-app relays connected");
            UpdateP4FRealtimeStatus();

            var rulesetId = SelectedP3FMatchmakingRuleset();
            var queued = await _p4fPrimaryRelay.JoinMatchmakingAsync("p4f-client-a", rulesetId);
            var found = await _p4fSecondaryRelay.JoinMatchmakingAsync("p4f-client-b", rulesetId);
            var status = found.MatchmakingStatus ?? queued.MatchmakingStatus ?? _p4fSecondaryRelay.LastMatchmakingStatus;
            if (found.Envelope.MessageType != OnlineMessageTypes.MatchFound || status == null)
            {
                throw new InvalidOperationException($"Matchmaking did not produce MatchFound: {found.Error?.ReasonCode} {found.Error?.ReasonText}".Trim());
            }

            RememberP4FMatchmakingStatus(status);
            _p4fRoomId = status.RoomId;
            _p4fTableId = status.TableId;
            P3ERoomBox.Text = _p4fRoomId;
            P3ETableBox.Text = _p4fTableId;
            P4FMatchStatusText.Text = $"MatchFound ruleset={rulesetId} room={_p4fRoomId} table={_p4fTableId} primary={ShortId(_p4fPrimarySession!.PlayerId)} seat={DisplaySeat(_p4fPrimarySeatIndex)} secondary={ShortId(_p4fSecondarySession!.PlayerId)} seat={DisplaySeat(_p4fSecondarySeatIndex)}";
            Log($"P4F {P4FMatchStatusText.Text}");
        }
        catch (Exception ex)
        {
            P4FMatchStatusText.Text = $"Create test match failed: {ex.Message}";
            Log(P4FMatchStatusText.Text);
        }
    }

    private async void P4FReadyBoth_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureP4FTestPairReady();
            await _p4fPrimaryRelay!.ReadyAsync("p4f-client-a", _p4fRoomId, _p4fTableId);
            await _p4fSecondaryRelay!.ReadyAsync("p4f-client-b", _p4fRoomId, _p4fTableId);
            P4FMatchStatusText.Text = $"Ready both: room={_p4fRoomId} table={_p4fTableId}";
            Log($"P4F {P4FMatchStatusText.Text}");
        }
        catch (Exception ex)
        {
            P4FMatchStatusText.Text = $"Ready both failed: {ex.Message}";
            Log(P4FMatchStatusText.Text);
        }
    }

    private async void P4FManualJoinMatchmaking_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ResetP4FRelaysAsync();
            await EnsureP4FPrimaryRelayAsync("p4f-manual");

            var rulesetId = SelectedP3FMatchmakingRuleset();
            var joined = await _p4fPrimaryRelay!.JoinMatchmakingAsync("p4f-manual", rulesetId);
            RememberP4FServerSeq(joined);
            if (joined.MatchmakingStatus != null)
            {
                RememberP4FMatchmakingStatus(joined.MatchmakingStatus);
            }

            var state = joined.MatchmakingStatus?.State ?? joined.Envelope.MessageType;
            P4FMatchStatusText.Text = joined.Envelope.MessageType == OnlineMessageTypes.MatchFound
                ? $"Manual MatchFound ruleset={rulesetId} room={_p4fRoomId} table={_p4fTableId} seat={DisplaySeat(_p4fPrimarySeatIndex)}"
                : $"Manual matchmaking {state}: ruleset={rulesetId}. Start a second ChessOnlineApp window with the same profile.";
            Log($"P4F {P4FMatchStatusText.Text}");
        }
        catch (Exception ex)
        {
            P4FMatchStatusText.Text = $"Manual matchmaking failed: {ex.Message}";
            Log(P4FMatchStatusText.Text);
        }
    }

    private async void P4FReadyThisWindow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureP4FMatchReady();
            var ready = await _p4fPrimaryRelay!.ReadyAsync("p4f-manual", _p4fRoomId, _p4fTableId);
            RememberP4FServerSeq(ready);
            P4FMatchStatusText.Text = ready.Envelope.MessageType == OnlineMessageTypes.TableState
                ? $"Ready this window: seat={DisplaySeat(_p4fPrimarySeatIndex)} room={_p4fRoomId} table={_p4fTableId}"
                : $"Ready this window rejected: {ready.Error?.ReasonCode} {ready.Error?.ReasonText}".Trim();
            Log($"P4F {P4FMatchStatusText.Text}");
        }
        catch (Exception ex)
        {
            P4FMatchStatusText.Text = $"Ready this window failed: {ex.Message}";
            Log(P4FMatchStatusText.Text);
        }
    }

    private async void P4FStartGame_Click(object sender, RoutedEventArgs e)
    {
        await StartP4FGameForPrimaryAsync("p4f-client-a", "Start");
    }

    private async void P4FStartThisWindow_Click(object sender, RoutedEventArgs e)
    {
        await StartP4FGameForPrimaryAsync("p4f-manual", "Start this window");
    }

    private async void P4FRequestSnapshot_Click(object sender, RoutedEventArgs e)
    {
        await RequestP4FSnapshotForPrimaryAsync("p4f-client-a", "Snapshot");
    }

    private async void P4FSnapshotThisWindow_Click(object sender, RoutedEventArgs e)
    {
        await RequestP4FSnapshotForPrimaryAsync("p4f-manual", "Snapshot this window");
    }

    private async Task StartP4FGameForPrimaryAsync(string clientId, string label)
    {
        try
        {
            EnsureP4FMatchReady();
            var started = await _p4fPrimaryRelay!.StartGameAsync(clientId, _p4fRoomId, _p4fTableId);
            RememberP4FServerSeq(started);
            if (started.Snapshot != null)
            {
                RememberP4FSnapshot(started);
            }
            P4FMatchStatusText.Text = started.Envelope.MessageType == OnlineMessageTypes.GameStarted
                ? $"{label}: {started.Envelope.MessageType} ruleset={started.Snapshot?.RulesetId} hash={started.Snapshot?.StateHash}"
                : $"{label} rejected: {started.Error?.ReasonCode} {started.Error?.ReasonText}".Trim();
            Log($"P4F {P4FMatchStatusText.Text}");
        }
        catch (Exception ex)
        {
            P4FMatchStatusText.Text = $"{label} failed: {ex.Message}";
            Log(P4FMatchStatusText.Text);
        }
    }

    private async Task RequestP4FSnapshotForPrimaryAsync(string clientId, string label)
    {
        try
        {
            EnsureP4FMatchReady();
            var snapshot = await _p4fPrimaryRelay!.RequestSnapshotAsync(clientId, _p4fRoomId, _p4fTableId);
            RememberP4FSnapshot(snapshot);
            P4FMatchStatusText.Text = $"{label}: ruleset={snapshot.Snapshot?.RulesetId} seq={snapshot.Envelope.ServerSeq} actions={snapshot.Snapshot?.ActionCount} hash={snapshot.Snapshot?.StateHash}";
            Log($"P4F {P4FMatchStatusText.Text}");
        }
        catch (Exception ex)
        {
            P4FMatchStatusText.Text = $"{label} failed: {ex.Message}";
            Log(P4FMatchStatusText.Text);
        }
    }

    private async void P4FRequestActionLog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureP4FMatchReady();
            var actionLog = await _p4fPrimaryRelay!.RequestActionLogAsync("p4f-client-a", _p4fRoomId, _p4fTableId);
            RememberP4FServerSeq(actionLog);
            var count = actionLog.ActionLog?.Events.Count ?? 0;
            P4FMatchStatusText.Text = $"ActionLog: seq={actionLog.Envelope.ServerSeq} events={count}";
            Log($"P4F {P4FMatchStatusText.Text}");
            if (actionLog.ActionLog != null)
            {
                P4FActionLogList.Items.Clear();
                foreach (var actionEvent in actionLog.ActionLog.Events)
                {
                    var line = $"#{actionEvent.ServerSeq}: {actionEvent.Notation} hash={actionEvent.StateHashAfter}";
                    P4FActionLogList.Items.Add(line);
                    Log($"P4F event {line}");
                }
            }
        }
        catch (Exception ex)
        {
            P4FMatchStatusText.Text = $"Action log failed: {ex.Message}";
            Log(P4FMatchStatusText.Text);
        }
    }

    private async void P4FSubmitSafeAsgardAction_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureP4FMatchReady();
            if (!string.Equals(SelectedP3FMatchmakingRuleset(), "asgard-convergence-3d-8x8x8-v0.1", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Safe test action is currently defined only for the Asgard profile.");
            }

            if (_p4fLastSnapshot == null)
            {
                var snapshot = await _p4fPrimaryRelay!.RequestSnapshotAsync("p4f-client-a", _p4fRoomId, _p4fTableId);
                RememberP4FSnapshot(snapshot);
            }
            if (!CanP4FPrimaryAct(out var disabledReason))
            {
                throw new InvalidOperationException($"Primary player cannot submit now: {disabledReason}");
            }

            var action = new OnlineActionCommand
            {
                ActionKind = OnlineActionKinds.NormalMove,
                ActorSide = 1,
                ExpectedStateHashBefore = _p4fLastSnapshot?.StateHash ?? "",
                FromX = 2,
                FromY = 3,
                FromZ = 0,
                ToX = 2,
                ToY = 3,
                ToZ = 1
            };

            var result = await _p4fPrimaryRelay!.SubmitActionAsync("p4f-client-a", _p4fRoomId, _p4fTableId, action);
            RememberP4FServerSeq(result);
            if (result.Envelope.MessageType == OnlineMessageTypes.ActionAccepted)
            {
                _p4fAcceptedActionCount++;
                var notation = result.ActionLog?.Events.LastOrDefault()?.Notation ?? "accepted";
                P4FMatchStatusText.Text = $"Action accepted: {notation}";
                if (!string.IsNullOrWhiteSpace(notation))
                {
                    P4FActionLogList.Items.Add($"#{result.Envelope.ServerSeq}: {notation}");
                }
                var snapshot = await _p4fPrimaryRelay!.RequestSnapshotAsync("p4f-client-a", _p4fRoomId, _p4fTableId);
                RememberP4FSnapshot(snapshot);
            }
            else
            {
                _p4fRejectedActionCount++;
                P4FMatchStatusText.Text = $"Action rejected: {result.Error?.ReasonCode} {result.Error?.ReasonText}".Trim();
            }
            UpdateP4FActionCounters();
            Log($"P4F {P4FMatchStatusText.Text}");
        }
        catch (Exception ex)
        {
            _p4fRejectedActionCount++;
            UpdateP4FActionCounters();
            P4FMatchStatusText.Text = $"Submit safe action failed: {ex.Message}";
            Log(P4FMatchStatusText.Text);
        }
    }

    private void P4FClearEventLog_Click(object sender, RoutedEventArgs e)
    {
        P4FActionLogList.Items.Clear();
        LogBox.Clear();
        Log("P4F event log cleared.");
    }

    private void P4FSaveSessionReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var root = Path.Combine(FindRepoRoot(), ".tmp", "manual-smoke");
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, $"p4f-online-client-session-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            var report = new
            {
                format = "p4f-online-client-session",
                createdUtc = DateTime.UtcNow.ToString("O"),
                playMode = SelectedP4FPlayMode(),
                baseUrl = P4FBaseUrlBox.Text.Trim(),
                hubUrl = P3FServerUrlBox.Text.Trim(),
                rulesetId = SelectedP3FMatchmakingRuleset(),
                roomId = _p4fRoomId,
                tableId = _p4fTableId,
                primaryPlayer = ShortId(_p4fPrimarySession?.PlayerId ?? ""),
                secondaryPlayer = ShortId(_p4fSecondarySession?.PlayerId ?? ""),
                primarySeat = _p4fPrimarySeatIndex,
                secondarySeat = _p4fSecondarySeatIndex,
                seatTurn = new
                {
                    _p4fSeatTurnState.CurrentSide,
                    _p4fSeatTurnState.CurrentMacroPlayer,
                    _p4fSeatTurnState.CanPrimaryAct,
                    _p4fSeatTurnState.DisabledReason,
                    _p4fSeatTurnState.Summary
                },
                snapshot = _p4fLastSnapshot == null ? null : new
                {
                    _p4fLastSnapshot.RulesetId,
                    _p4fLastSnapshot.ServerSeq,
                    _p4fLastSnapshot.StateHash,
                    _p4fLastSnapshot.ActionCount,
                    _p4fLastSnapshot.LastActionNotation
                },
                acceptedActionCount = _p4fAcceptedActionCount,
                rejectedActionCount = _p4fRejectedActionCount,
                lastServerSeq = _p4fLastServerSeq,
                actionLogItems = P4FActionLogList.Items.Cast<object>().Select(item => item.ToString()).ToArray()
            };
            File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            P4FMatchStatusText.Text = $"Session report saved: {path}";
            Log($"P4F session report saved: {path}");
        }
        catch (Exception ex)
        {
            P4FMatchStatusText.Text = $"Save session report failed: {ex.Message}";
            Log(P4FMatchStatusText.Text);
        }
    }

    private ChessOnlineServerEndpoint ResolveP4FEndpoint()
    {
        var baseUrl = P4FBaseUrlBox.Text.Trim();
        if (baseUrl.Contains("<HETZNER_HOST>", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Set a real Base URL first, or click Use Hetzner HTTP.");
        }
        return ChessOnlineServerEndpoint.FromBaseUrl(baseUrl);
    }

    private ChessOnlineServerEndpoint ResolveP4FEndpointForConnect()
    {
        var baseUrl = P4FBaseUrlBox.Text.Trim();
        return baseUrl.Contains("<HETZNER_HOST>", StringComparison.OrdinalIgnoreCase)
            ? ChessOnlineServerEndpoint.FromBaseUrl(P3FServerUrlBox.Text.Trim())
            : ChessOnlineServerEndpoint.FromBaseUrl(baseUrl);
    }

    private static HttpClient CreateP4FHttpClient(ChessOnlineServerEndpoint endpoint)
    {
        return new HttpClient
        {
            BaseAddress = endpoint.BaseUri,
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    private void RequireSuccessfulAuth(ChessOnlineAuthTokenResponse token, string operation)
    {
        if (!token.Success || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException($"{operation} failed: {token.ErrorCode} {token.ErrorText}".Trim());
        }
    }

    private void UpdateP4FAuthStatus(string prefix)
    {
        var primary = _p4fPrimarySession?.RedactedStatus ?? "anonymous";
        var secondary = _p4fSecondarySession?.RedactedStatus ?? "none";
        P4FAuthStatusText.Text = $"Auth status: {prefix} primary={primary}; secondary={secondary}";
        Log(P4FAuthStatusText.Text);
    }

    private static string ShortId(string value)
    {
        return value.Length <= 8 ? value : value[..8];
    }

    private string SelectedP4FPlayMode()
    {
        return P4FPlayModeBox?.SelectedItem is ComboBoxItem item && item.Content is string text
            ? text
            : "Single-App Test Pair";
    }

    private async Task EnsureP4FTwoSessionsAsync()
    {
        if (_p4fPrimarySession?.IsAuthenticated == true && _p4fSecondarySession?.IsAuthenticated == true)
        {
            return;
        }

        var endpoint = ResolveP4FEndpoint();
        using var http = CreateP4FHttpClient(endpoint);
        var auth = new ChessOnlineAuthClient(http, endpoint);
        var tokenA = await auth.RegisterTemporaryUserAsync("p4f_a", "ChessOnlineApp-P4F-A");
        var tokenB = await auth.RegisterTemporaryUserAsync("p4f_b", "ChessOnlineApp-P4F-B");
        RequireSuccessfulAuth(tokenA, "register player A");
        RequireSuccessfulAuth(tokenB, "register player B");
        _p4fPrimarySession = new ChessOnlineClientSession(endpoint, "ChessOnlineApp-P4F-A");
        _p4fSecondarySession = new ChessOnlineClientSession(endpoint, "ChessOnlineApp-P4F-B");
        _p4fPrimarySession.SetToken(tokenA);
        _p4fSecondarySession.SetToken(tokenB);
        UpdateP4FAuthStatus($"Two temporary players ready: A={ShortId(tokenA.PlayerId)} B={ShortId(tokenB.PlayerId)}.");
    }

    private async Task EnsureP4FPrimarySessionAsync()
    {
        if (_p4fPrimarySession?.IsAuthenticated == true)
        {
            return;
        }

        var endpoint = ResolveP4FEndpoint();
        using var http = CreateP4FHttpClient(endpoint);
        var auth = new ChessOnlineAuthClient(http, endpoint);
        var token = await auth.RegisterTemporaryUserAsync("p4f_manual", "ChessOnlineApp-P4F-Manual");
        RequireSuccessfulAuth(token, "register manual player");
        _p4fPrimarySession = new ChessOnlineClientSession(endpoint, "ChessOnlineApp-P4F-Manual");
        _p4fPrimarySession.SetToken(token);
        P4FAuthUserBox.Text = token.UserName;
        P4FAuthPasswordBox.Password = "";
        UpdateP4FAuthStatus($"Manual temporary player ready: {ShortId(token.PlayerId)}.");
    }

    private async Task EnsureP4FPrimaryRelayAsync(string clientId)
    {
        await EnsureP4FPrimarySessionAsync();
        if (_p4fPrimaryRelay != null)
        {
            return;
        }

        _p4fPrimaryRelay = new ChessOnlineRelayClient(_p4fPrimarySession!);
        _p4fPrimaryRelay.MessageReceived += P4FRelayMessageReceived;
        await _p4fPrimaryRelay.ConnectAsync();
        await _p4fPrimaryRelay.HelloAsync(clientId);
        _p4fRealtimeSync.MarkConnectionState("primary relay connected");
        UpdateP4FRealtimeStatus();
    }

    private async Task ResetP4FRelaysAsync()
    {
        if (_p4fPrimaryRelay != null)
        {
            await _p4fPrimaryRelay.DisposeAsync();
            _p4fPrimaryRelay = null;
        }
        if (_p4fSecondaryRelay != null)
        {
            await _p4fSecondaryRelay.DisposeAsync();
            _p4fSecondaryRelay = null;
        }
        _p4fRoomId = "";
        _p4fTableId = "";
        _p4fPrimarySeatIndex = 0;
        _p4fSecondarySeatIndex = 0;
        _p4fLastMatchmakingStatus = null;
        _p4fSeatTurnState = OnlineSeatTurnState.Empty();
        _p4fRealtimeSync = new OnlineRealtimeSyncState();
        _p4fResyncRefreshPending = false;
        _p4fLastSnapshot = null;
        _p4gBoardSnapshot = null;
        _p4gSelectedCell = null;
        _p4gMoveFrom = null;
        _p4gMoveTo = null;
        ClearP4GLegalPreview("Legal preview: session cleared.");
        _p4gSubmitPending = false;
        _p4fAcceptedActionCount = 0;
        _p4fRejectedActionCount = 0;
        _p4fLastServerSeq = 0;
        P4FActionLogList.Items.Clear();
        P4FSnapshotStatusText.Text = "Snapshot: none.";
        RenderP4GBoard();
        UpdateP4FSeatTurnStatus();
        UpdateP4FRealtimeStatus();
        UpdateP4FActionCounters();
    }

    private void EnsureP4FMatchReady()
    {
        if (_p4fPrimaryRelay == null ||
            string.IsNullOrWhiteSpace(_p4fRoomId) ||
            string.IsNullOrWhiteSpace(_p4fTableId))
        {
            throw new InvalidOperationException("Join matchmaking and wait for MatchFound first.");
        }
    }

    private void EnsureP4FTestPairReady()
    {
        if (_p4fPrimaryRelay == null || _p4fSecondaryRelay == null ||
            string.IsNullOrWhiteSpace(_p4fRoomId) ||
            string.IsNullOrWhiteSpace(_p4fTableId))
        {
            throw new InvalidOperationException("Create a two-client test match first.");
        }
    }

    private void RememberP4FMatchmakingStatus(OnlineMatchmakingStatus status)
    {
        _p4fLastMatchmakingStatus = status;
        if (!string.IsNullOrWhiteSpace(status.RoomId))
        {
            _p4fRoomId = status.RoomId;
            P3ERoomBox.Text = status.RoomId;
        }
        if (!string.IsNullOrWhiteSpace(status.TableId))
        {
            _p4fTableId = status.TableId;
            P3ETableBox.Text = status.TableId;
        }

        var primarySeat = OnlineSeatTurnState.FindSeat(status, _p4fPrimarySession?.PlayerId ?? "");
        var secondarySeat = OnlineSeatTurnState.FindSeat(status, _p4fSecondarySession?.PlayerId ?? "");
        if (primarySeat > 0)
        {
            _p4fPrimarySeatIndex = primarySeat;
        }
        if (secondarySeat > 0)
        {
            _p4fSecondarySeatIndex = secondarySeat;
        }

        UpdateP4FSeatTurnStatus();
    }

    private bool CanP4FPrimaryAct(out string reason)
    {
        UpdateP4FSeatTurnStatus();
        reason = _p4fSeatTurnState.DisabledReason;
        return _p4fSeatTurnState.CanPrimaryAct;
    }

    private void UpdateP4FSeatTurnStatus()
    {
        var rulesetId = _p4gBoardSnapshot?.RulesetId ??
            _p4fLastSnapshot?.RulesetId ??
            _p4fLastMatchmakingStatus?.RequestedRulesetId ??
            _p4fLastMatchmakingStatus?.Tickets.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.RequestedRulesetId))?.RequestedRulesetId ??
            SelectedP3FMatchmakingRuleset();

        _p4fSeatTurnState = OnlineSeatTurnState.FromMatch(
            rulesetId,
            _p4fPrimarySession?.PlayerId ?? "",
            _p4fSecondarySession?.PlayerId ?? "",
            _p4fPrimarySeatIndex,
            _p4fSecondarySeatIndex,
            _p4gBoardSnapshot);

        if (P4FSeatTurnStatusText != null)
        {
            P4FSeatTurnStatusText.Text = _p4fSeatTurnState.Summary;
            P4FSeatTurnStatusText.Foreground = _p4fSeatTurnState.CanPrimaryAct ? Brush("#B8F7C6") : Brush("#F4D58D");
        }
    }

    private OnlineRealtimeObservation ObserveP4FRealtime(string label, OnlineProtocolMessage message)
    {
        var observation = _p4fRealtimeSync.Observe(message);
        UpdateP4FRealtimeStatus();
        if (observation.IsDuplicate)
        {
            Log($"P4G realtime duplicate ignored from {label}: {observation.Reason}");
        }
        else if (observation.HasGap || observation.RequiresResync)
        {
            Log($"P4G realtime resync hint from {label}: {observation.Reason}");
        }
        return observation;
    }

    private void UpdateP4FRealtimeStatus()
    {
        if (P4FRealtimeStatusText != null)
        {
            P4FRealtimeStatusText.Text = _p4fRealtimeSync.Summary;
            P4FRealtimeStatusText.Foreground = _p4fRealtimeSync.ResyncRequired ? Brush("#F4D58D") : Brush("#AFC0D0");
        }
    }

    private async Task RefreshP4FAfterRealtimeResyncAsync(string reason)
    {
        if (_p4fResyncRefreshPending || _p4fPrimaryRelay == null ||
            string.IsNullOrWhiteSpace(_p4fRoomId) ||
            string.IsNullOrWhiteSpace(_p4fTableId))
        {
            return;
        }

        _p4fResyncRefreshPending = true;
        try
        {
            P4FRealtimeStatusText.Text = $"Realtime: resync refresh starting ({reason}).";
            var snapshot = await _p4fPrimaryRelay.RequestSnapshotAsync("p4f-resync", _p4fRoomId, _p4fTableId);
            RememberP4FSnapshot(snapshot);
            var actionLog = await _p4fPrimaryRelay.RequestActionLogAsync("p4f-resync", _p4fRoomId, _p4fTableId);
            RememberP4FServerSeq(actionLog);
            if (actionLog.ActionLog != null)
            {
                foreach (var actionEvent in actionLog.ActionLog.Events.TakeLast(10))
                {
                    var line = $"resync #{actionEvent.ServerSeq}: {actionEvent.Notation}";
                    if (!P4FActionLogList.Items.Contains(line))
                    {
                        P4FActionLogList.Items.Add(line);
                    }
                }
            }
            _p4fRealtimeSync.ClearResync();
            UpdateP4FRealtimeStatus();
        }
        catch (Exception ex)
        {
            _p4fRealtimeSync.MarkConnectionState($"resync failed: {ex.Message}");
            UpdateP4FRealtimeStatus();
            Log($"P4G realtime resync failed: {ex.Message}");
        }
        finally
        {
            _p4fResyncRefreshPending = false;
        }
    }

    private static string DisplaySeat(int seat) => seat > 0 ? seat.ToString() : "none";

    private void RememberP4FSnapshot(OnlineProtocolMessage message)
    {
        RememberP4FServerSeq(message);
        if (message.Snapshot == null)
        {
            return;
        }
        _p4fLastSnapshot = message.Snapshot;
        if (!string.IsNullOrWhiteSpace(_p4gLegalPreview.StateHash) &&
            !string.Equals(_p4gLegalPreview.StateHash, message.Snapshot.StateHash, StringComparison.Ordinal))
        {
            ClearP4GLegalPreview("Legal preview: cleared after authoritative snapshot update.");
        }
        P4FSnapshotStatusText.Text =
            $"Snapshot: ruleset={message.Snapshot.RulesetId} seq={message.Snapshot.ServerSeq} turn={message.Snapshot.TurnSummary} actions={message.Snapshot.ActionCount} hash={message.Snapshot.StateHash}";
        if (OnlineChess3DBoardSnapshotParser.TryParse(message.Snapshot, out var board, out var boardError))
        {
            _p4gBoardSnapshot = board;
            P4GBoardStatusText.Text =
                $"Board: {board.RulesetId} seq={board.ServerSeq} occupied={board.OccupiedCells.Count()} side={board.CurrentSide} macro={board.CurrentMacroPlayer}";
        }
        else
        {
            _p4gBoardSnapshot = null;
            _p4gSelectedCell = null;
            ClearP4GLegalPreview("Legal preview: board parse failed.");
            P4GBoardStatusText.Text = $"Board parse failed: {boardError}";
        }
        UpdateP4FSeatTurnStatus();
        RenderP4GBoard();
        UpdateP4GSpecialActionPanels();
        UpdateP4FActionCounters();
    }

    private void P4FRelayMessageReceived(string label, OnlineProtocolMessage message)
    {
        if (!label.StartsWith("Receive", StringComparison.Ordinal))
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            var observation = ObserveP4FRealtime(label, message);
            if (observation.IsDuplicate)
            {
                return;
            }
            RememberP4FServerSeq(message);
            if (message.Snapshot != null)
            {
                RememberP4FSnapshot(message);
            }
            if (message.MatchmakingStatus != null)
            {
                RememberP4FMatchmakingStatus(message.MatchmakingStatus);
            }
            if (message.ActionLog?.Events.Count > 0)
            {
                foreach (var actionEvent in message.ActionLog.Events)
                {
                    var line = $"event seq={actionEvent.ServerSeq} {actionEvent.Notation}";
                    if (!P4FActionLogList.Items.Contains(line))
                    {
                        P4FActionLogList.Items.Add(line);
                    }
                }
            }
            Log($"P4G realtime event {label}: {message.Envelope.MessageType} seq={message.Envelope.ServerSeq}");
            if (observation.HasGap || observation.RequiresResync)
            {
                _ = RefreshP4FAfterRealtimeResyncAsync(observation.Reason);
            }
        });
    }

    private void P4GRefreshBoard_Click(object sender, RoutedEventArgs e)
    {
        RenderP4GBoard();
    }

    private async void P4GRefreshLegalPreview_Click(object sender, RoutedEventArgs e)
    {
        if (_p4gMoveFrom == null && _p4gSelectedCell?.IsOccupied == true)
        {
            _p4gMoveFrom = _p4gSelectedCell;
        }
        if (_p4gMoveFrom == null)
        {
            ClearP4GLegalPreview("Legal preview: select an occupied source cell first.");
            RenderP4GBoard();
            return;
        }

        await RequestP4GLegalPreviewAsync(_p4gMoveFrom);
    }

    private async void P4GSubmitSelectedPreviewAction_Click(object sender, RoutedEventArgs e)
    {
        if (P4GLegalPreviewOptionBox.SelectedItem is not LegalActionOptionViewModel option)
        {
            P4GMoveStatusText.Text = "Online move: choose a legal preview action first.";
            return;
        }

        await SubmitP4GPreviewOptionAsync(option);
    }

    private void P4GBoardLayerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (P4GBoardGrid != null)
        {
            RenderP4GBoard();
        }
    }

    private void P3FMatchmakingProfileBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateP4GSpecialActionPanels();
    }

    private async void P4GBoardCell_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: OnlineChess3DBoardCell cell })
        {
            _p4gSelectedCell = cell;
            P4GSelectedCellText.Text = $"Selected online cell: {cell.Coordinate} piece={PieceLabel(cell.PieceCode)} index={cell.Index}";
            if (cell.IsOccupied)
            {
                _p4gMoveFrom = cell;
                _p4gMoveTo = null;
                P4GMoveStatusText.Text = $"Online move: source={cell.Coordinate}. Requesting legal targets...";
                await RequestP4GLegalPreviewAsync(cell);
                return;
            }

            _p4gMoveTo = cell;
            var matches = MatchingP4GPreviewOptions(cell).ToArray();
            if (matches.Length == 1)
            {
                await SubmitP4GPreviewOptionAsync(matches[0]);
                return;
            }
            if (matches.Length > 1)
            {
                P4GLegalPreviewOptionBox.SelectedItem = matches[0];
                P4GMoveStatusText.Text = $"Online move: {matches.Length} legal actions target {cell.Coordinate}; choose one and submit.";
                RenderP4GBoard();
                return;
            }

            P4GMoveStatusText.Text = $"Online move: selected target {cell.Coordinate}. From={DescribeMoveCell(_p4gMoveFrom)}.";
            RenderP4GBoard();
        }
    }

    private void P4GUseSelectedFrom_Click(object sender, RoutedEventArgs e)
    {
        if (_p4gSelectedCell == null)
        {
            P4GMoveStatusText.Text = "Online move: select a cell first.";
            return;
        }

        _p4gMoveFrom = _p4gSelectedCell;
        P4GMoveStatusText.Text = $"Online move: From={DescribeMoveCell(_p4gMoveFrom)} To={DescribeMoveCell(_p4gMoveTo)}.";
    }

    private void P4GUseSelectedTo_Click(object sender, RoutedEventArgs e)
    {
        if (_p4gSelectedCell == null)
        {
            P4GMoveStatusText.Text = "Online move: select a cell first.";
            return;
        }

        _p4gMoveTo = _p4gSelectedCell;
        P4GMoveStatusText.Text = $"Online move: From={DescribeMoveCell(_p4gMoveFrom)} To={DescribeMoveCell(_p4gMoveTo)}.";
    }

    private async void P4GSubmitNormalMove_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureP4FMatchReady();
            if (_p4gBoardSnapshot == null || _p4fLastSnapshot == null)
            {
                throw new InvalidOperationException("Request a snapshot before submitting a board move.");
            }
            if (_p4gMoveFrom == null || _p4gMoveTo == null)
            {
                throw new InvalidOperationException("Choose From and To cells first.");
            }
            if (!_p4gMoveFrom.IsOccupied)
            {
                throw new InvalidOperationException("From cell is empty.");
            }
            if (!CanP4FPrimaryAct(out var disabledReason))
            {
                throw new InvalidOperationException($"Primary player cannot submit now: {disabledReason}");
            }

            var action = new OnlineActionCommand
            {
                ActionKind = OnlineActionKinds.NormalMove,
                ActorSide = _p4gMoveFrom.Side,
                ExpectedStateHashBefore = _p4fLastSnapshot.StateHash,
                FromX = _p4gMoveFrom.X,
                FromY = _p4gMoveFrom.Y,
                FromZ = _p4gMoveFrom.Z,
                ToX = _p4gMoveTo.X,
                ToY = _p4gMoveTo.Y,
                ToZ = _p4gMoveTo.Z
            };

            var result = await _p4fPrimaryRelay!.SubmitActionAsync("p4f-client-a", _p4fRoomId, _p4fTableId, action);
            RememberP4FServerSeq(result);
            if (result.Envelope.MessageType == OnlineMessageTypes.ActionAccepted)
            {
                _p4fAcceptedActionCount++;
                var notation = result.ActionLog?.Events.LastOrDefault()?.Notation ?? "accepted";
                P4GMoveStatusText.Text = $"Online move accepted: {notation}";
                P4FActionLogList.Items.Add($"#{result.Envelope.ServerSeq}: {notation}");
                var snapshot = await _p4fPrimaryRelay!.RequestSnapshotAsync("p4f-client-a", _p4fRoomId, _p4fTableId);
                RememberP4FSnapshot(snapshot);
            }
            else
            {
                _p4fRejectedActionCount++;
                P4GMoveStatusText.Text = $"Online move rejected: {result.Error?.ReasonCode} {result.Error?.ReasonText}".Trim();
            }
            UpdateP4FActionCounters();
            Log($"P4G {P4GMoveStatusText.Text}");
        }
        catch (Exception ex)
        {
            _p4fRejectedActionCount++;
            UpdateP4FActionCounters();
            P4GMoveStatusText.Text = $"Online move failed: {ex.Message}";
            Log(P4GMoveStatusText.Text);
        }
    }

    private async Task RequestP4GLegalPreviewAsync(OnlineChess3DBoardCell source)
    {
        try
        {
            EnsureP4FMatchReady();
            if (_p4fLastSnapshot == null)
            {
                ClearP4GLegalPreview("Legal preview: request a snapshot first.");
                RenderP4GBoard();
                return;
            }
            if (!source.IsOccupied)
            {
                ClearP4GLegalPreview("Legal preview: selected source is empty.");
                RenderP4GBoard();
                return;
            }

            var result = await _p4fPrimaryRelay!.RequestLegalPreviewAsync(
                "p4f-client-a",
                _p4fRoomId,
                _p4fTableId,
                new OnlineLegalPreviewRequest
                {
                    SourceX = source.X,
                    SourceY = source.Y,
                    SourceZ = source.Z,
                    ActorSide = source.Side,
                    ExpectedStateHash = _p4fLastSnapshot.StateHash
                });
            RememberP4FServerSeq(result);
            if (result.Envelope.MessageType != OnlineMessageTypes.LegalPreviewResult || result.LegalPreview == null)
            {
                ClearP4GLegalPreview($"Legal preview failed: {result.Error?.ReasonCode} {result.Error?.ReasonText}".Trim());
                RenderP4GBoard();
                return;
            }

            _p4gLegalPreview = LegalPreviewState.FromMessage(result);
            RenderP4GLegalPreviewList();
            if (_p4gLegalPreview.IsStale)
            {
                P4GLegalPreviewStatusText.Text = $"Legal preview stale: {_p4gLegalPreview.Reason}. Requesting snapshot...";
                var snapshot = await _p4fPrimaryRelay!.RequestSnapshotAsync("p4f-client-a", _p4fRoomId, _p4fTableId);
                RememberP4FSnapshot(snapshot);
                return;
            }

            P4GLegalPreviewStatusText.Text = _p4gLegalPreview.Options.Count > 0
                ? $"Legal preview: {_p4gLegalPreview.Options.Count} legal action(s) from {source.Coordinate}."
                : $"Legal preview: {_p4gLegalPreview.Reason}";
            P4GMoveStatusText.Text = $"Online move: source={source.Coordinate}; click a highlighted target or use manual To.";
            RenderP4GBoard();
        }
        catch (Exception ex)
        {
            ClearP4GLegalPreview($"Legal preview failed: {ex.Message}");
            RenderP4GBoard();
            Log(P4GLegalPreviewStatusText.Text);
        }
    }

    private IEnumerable<LegalActionOptionViewModel> MatchingP4GPreviewOptions(OnlineChess3DBoardCell target)
    {
        return _p4gLegalPreview.Options.Where(o =>
            o.ToX == target.X &&
            o.ToY == target.Y &&
            o.ToZ == target.Z);
    }

    private async Task SubmitP4GPreviewOptionAsync(LegalActionOptionViewModel option)
    {
        if (_p4gSubmitPending)
        {
            P4GMoveStatusText.Text = "Online move: submit already pending.";
            return;
        }
        if (_p4fLastSnapshot == null)
        {
            P4GMoveStatusText.Text = "Online move: request a snapshot before submitting.";
            return;
        }
        if (!OnlinePreviewActionDispatchPolicy.CanSubmitFromGenericBoard(option.ActionKind, out var unsupportedReason))
        {
            P4GMoveStatusText.Text = $"Online move: {unsupportedReason}";
            Log($"P4G {P4GMoveStatusText.Text}");
            return;
        }
        if (!CanP4FPrimaryAct(out var disabledReason))
        {
            P4GMoveStatusText.Text = $"Online move disabled: {disabledReason}";
            Log($"P4G {P4GMoveStatusText.Text}");
            return;
        }

        _p4gSubmitPending = true;
        try
        {
            EnsureP4FMatchReady();
            var action = option.Command;
            action.ExpectedStateHashBefore = _p4fLastSnapshot.StateHash;
            var result = await _p4fPrimaryRelay!.SubmitActionAsync("p4f-client-a", _p4fRoomId, _p4fTableId, action);
            RememberP4FServerSeq(result);
            if (result.Envelope.MessageType == OnlineMessageTypes.ActionAccepted)
            {
                _p4fAcceptedActionCount++;
                var notation = result.ActionLog?.Events.LastOrDefault()?.Notation ?? option.DisplayLabel;
                P4GMoveStatusText.Text = $"Online move accepted: {notation}";
                P4FActionLogList.Items.Add($"#{result.Envelope.ServerSeq}: {notation}");
                ClearP4GLegalPreview("Legal preview: cleared after accepted action.");
                var snapshot = await _p4fPrimaryRelay!.RequestSnapshotAsync("p4f-client-a", _p4fRoomId, _p4fTableId);
                RememberP4FSnapshot(snapshot);
            }
            else if (result.Envelope.MessageType == OnlineMessageTypes.ResyncRequired)
            {
                _p4fRejectedActionCount++;
                P4GMoveStatusText.Text = $"Online move requires resync: {result.Error?.ReasonCode} {result.Error?.ReasonText}".Trim();
                if (result.Snapshot != null)
                {
                    RememberP4FSnapshot(result);
                }
            }
            else
            {
                _p4fRejectedActionCount++;
                P4GMoveStatusText.Text = $"Online move rejected: {result.Error?.ReasonCode} {result.Error?.ReasonText}".Trim();
            }
            UpdateP4FActionCounters();
            Log($"P4G {P4GMoveStatusText.Text}");
            RenderP4GBoard();
        }
        catch (Exception ex)
        {
            _p4fRejectedActionCount++;
            UpdateP4FActionCounters();
            P4GMoveStatusText.Text = $"Online move failed: {ex.Message}";
            Log(P4GMoveStatusText.Text);
        }
        finally
        {
            _p4gSubmitPending = false;
        }
    }

    private void ClearP4GLegalPreview(string reason = "")
    {
        _p4gLegalPreview = LegalPreviewState.Empty(reason);
        if (P4GLegalPreviewList != null)
        {
            P4GLegalPreviewList.Items.Clear();
        }
        if (P4GLegalPreviewOptionBox != null)
        {
            P4GLegalPreviewOptionBox.ItemsSource = null;
        }
        if (P4GLegalPreviewStatusText != null)
        {
            P4GLegalPreviewStatusText.Text = string.IsNullOrWhiteSpace(reason)
                ? "Legal preview: select an occupied source cell."
                : reason;
        }
    }

    private void RenderP4GLegalPreviewList()
    {
        P4GLegalPreviewList.Items.Clear();
        P4GLegalPreviewOptionBox.ItemsSource = _p4gLegalPreview.Options;
        P4GLegalPreviewOptionBox.DisplayMemberPath = nameof(LegalActionOptionViewModel.DisplayLabel);
        if (_p4gLegalPreview.Options.Count > 0)
        {
            P4GLegalPreviewOptionBox.SelectedIndex = 0;
        }
        foreach (var option in _p4gLegalPreview.Options)
        {
            var suffix = option.IsCapture ? " capture" : option.IsSpecial ? " special" : "";
            P4GLegalPreviewList.Items.Add($"{option.DisplayLabel}{suffix}");
        }
    }

    private void UpdateP4GSpecialActionPanels()
    {
        if (P4GRubikLayerPanel == null)
        {
            return;
        }

        var rulesetId = _p4gBoardSnapshot?.RulesetId ??
            _p4fLastSnapshot?.RulesetId ??
            SelectedP3FMatchmakingRuleset();
        var showRubik = OnlinePreviewActionDispatchPolicy.ShouldShowRubikLayerPanel(rulesetId);
        P4GRubikLayerPanel.Visibility = showRubik ? Visibility.Visible : Visibility.Collapsed;
        if (showRubik && P4GRubikStatusText != null)
        {
            P4GRubikStatusText.Text = "Rubik layer-turn online dispatch is not finalized yet; layer actions are not sent as NormalMove.";
        }
    }

    private void RenderP4GBoard()
    {
        if (P4GBoardGrid == null)
        {
            return;
        }

        P4GBoardGrid.Children.Clear();
        if (_p4gBoardSnapshot == null)
        {
            P4GBoardStatusText.Text = "Board: no snapshot.";
            P4GSelectedCellText.Text = "Selected online cell: none.";
            P4GMoveStatusText.Text = "Online move: choose From and To.";
            return;
        }

        var layer = SelectedP4GLayer();
        if (layer < 0 || layer >= _p4gBoardSnapshot.Depth)
        {
            layer = 0;
        }

        for (var y = _p4gBoardSnapshot.Height - 1; y >= 0; y--)
        {
            for (var x = 0; x < _p4gBoardSnapshot.Width; x++)
            {
                var cell = _p4gBoardSnapshot.GetCell(x, y, layer);
                var isSelected = _p4gSelectedCell?.Index == cell.Index;
                var isFrom = _p4gMoveFrom?.Index == cell.Index;
                var isTo = _p4gMoveTo?.Index == cell.Index;
                var legalMarker = _p4gLegalPreview.Targets.FirstOrDefault(t => t.X == cell.X && t.Y == cell.Y && t.Z == cell.Z);
                var isLegalTarget = legalMarker != null;
                var button = new Button
                {
                    Content = cell.IsOccupied ? PieceLabel(cell.PieceCode) : ".",
                    Tag = cell,
                    MinWidth = 34,
                    MinHeight = 28,
                    Margin = new Thickness(1),
                    FontSize = 11,
                    Foreground = Brushes.White,
                    Background = isFrom ? Brush("#3F8F5F") :
                        isTo ? Brush("#9A6A3A") :
                        isSelected ? Brush("#3F7FBF") :
                        legalMarker?.IsCapture == true ? Brush("#A84E32") :
                        legalMarker?.IsSpecial == true ? Brush("#6F4FA8") :
                        isLegalTarget ? Brush("#2D5F9A") :
                        CellBrush(cell),
                    BorderBrush = isSelected || isFrom || isTo || isLegalTarget ? Brush("#D8F0FF") : Brush("#263442"),
                    ToolTip = isLegalTarget
                        ? $"{cell.Coordinate} index={cell.Index} piece={PieceLabel(cell.PieceCode)} legal={legalMarker!.DisplayLabel}"
                        : $"{cell.Coordinate} index={cell.Index} piece={PieceLabel(cell.PieceCode)}"
                };
                AutomationProperties.SetAutomationId(button, $"P4GCell_{cell.X}_{cell.Y}_{cell.Z}");
                AutomationProperties.SetName(button, $"Cell {cell.Coordinate} {PieceLabel(cell.PieceCode)}");
                AutomationProperties.SetHelpText(button, isLegalTarget
                    ? $"{cell.Coordinate} index={cell.Index} piece={PieceLabel(cell.PieceCode)} legal={legalMarker!.DisplayLabel}"
                    : $"{cell.Coordinate} index={cell.Index} piece={PieceLabel(cell.PieceCode)}");
                button.Click += P4GBoardCell_Click;
                P4GBoardGrid.Children.Add(button);
            }
        }

        P4GBoardStatusText.Text =
            $"Board: layer Z={layer} ruleset={_p4gBoardSnapshot.RulesetId} seq={_p4gBoardSnapshot.ServerSeq} occupied={_p4gBoardSnapshot.OccupiedCells.Count()} legalTargets={_p4gLegalPreview.Targets.Count} hash={_p4gBoardSnapshot.StateHash}";
    }

    private int SelectedP4GLayer()
    {
        if (P4GBoardLayerBox?.SelectedItem is ComboBoxItem item &&
            int.TryParse(item.Content?.ToString(), out var layer))
        {
            return layer;
        }
        return P4GBoardLayerBox?.SelectedIndex ?? 0;
    }

    private static Brush CellBrush(OnlineChess3DBoardCell cell)
    {
        if (!cell.IsOccupied)
        {
            return (cell.X + cell.Y + cell.Z) % 2 == 0 ? Brush("#17202A") : Brush("#1F2B37");
        }

        return cell.Side switch
        {
            1 => Brush("#5B6F89"),
            2 => Brush("#6F5B89"),
            3 => Brush("#5B8970"),
            4 => Brush("#897A5B"),
            5 => Brush("#895B5B"),
            6 => Brush("#5B8589"),
            _ => Brush("#4B5563")
        };
    }

    private static Brush Brush(string color)
    {
        return (Brush)new BrushConverter().ConvertFromString(color)!;
    }

    private static string PieceLabel(int pieceCode)
    {
        if (pieceCode == 0)
        {
            return ".";
        }

        var side = pieceCode / 10;
        var type = pieceCode % 10;
        var piece = type switch
        {
            1 => "P",
            2 => "N",
            3 => "B",
            4 => "R",
            5 => "Q",
            6 => "K",
            _ => "?"
        };
        return $"S{side}{piece}";
    }

    private static string DescribeMoveCell(OnlineChess3DBoardCell? cell)
    {
        return cell == null ? "none" : $"{cell.Coordinate} {PieceLabel(cell.PieceCode)}";
    }

    private void RememberP4FServerSeq(OnlineProtocolMessage message)
    {
        if (message.Envelope.ServerSeq > _p4fLastServerSeq)
        {
            _p4fLastServerSeq = message.Envelope.ServerSeq;
        }
        UpdateP4FActionCounters();
    }

    private void UpdateP4FActionCounters()
    {
        P4FActionCountersText.Text = $"Accepted={_p4fAcceptedActionCount} Rejected={_p4fRejectedActionCount} LastSeq={_p4fLastServerSeq}";
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "assets", "rules", "profiles")) ||
                File.Exists(Path.Combine(dir.FullName, "Chess.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return AppContext.BaseDirectory;
    }

    private void ApplyP4FEndpointToHubUrl()
    {
        ApplyP4FEndpointToHubUrl(ResolveP4FEndpoint());
    }

    private void ApplyP4FEndpointToHubUrl(ChessOnlineServerEndpoint endpoint)
    {
        P4FBaseUrlBox.Text = endpoint.ToString();
        P3FServerUrlBox.Text = endpoint.HubUri.ToString();
    }

    private async Task SaveRelayProfileAsync(Uri uri, string source)
    {
        var seat = ReadInt(RelaySeatBox, 1, 0, 6);
        var group = ReadInt(RelayGroupBox, 0, 0, 6);
        var profile = IntegrationProfileFactory.FromChess3DRelay(
            uri,
            RelayRoomBox.Text.Trim(),
            seat,
            group,
            hasSecret: !string.IsNullOrWhiteSpace(RelayTokenBox.Password));
        await SaveProfileAsync(profile, source);
    }

    private void EnsureP3ERoomJoined()
    {
        var create = _p3eRegistry.CreateRoom(P3EEnvelope(OnlineMessageTypes.CreateRoom), new OnlineRoomCommand
        {
            RoomId = P3ERoomBox.Text.Trim(),
            DisplayName = P3ERoomBox.Text.Trim()
        });
        if (create.Error?.ReasonCode is not null && create.Error.ReasonCode != OnlineRejectReasons.None)
        {
            // Existing room is fine for the local harness.
        }
        _p3eRegistry.JoinRoom(P3EEnvelope(OnlineMessageTypes.JoinRoom));
    }

    private async Task EnsureP3FHelloAsync()
    {
        if (_p3fConnection == null || _p3fConnection.State != HubConnectionState.Connected)
        {
            await Dispatcher.InvokeAsync(() => P3FStatusText.Text = "Connect to SignalR first.");
            return;
        }
        if (string.IsNullOrWhiteSpace(_p3fSessionToken))
        {
            var result = await P3FInvokeAsync("Hello", P3FMessage(OnlineMessageTypes.Hello));
            _p3fSessionToken = result.Envelope.SessionToken;
        }
    }

    private void RegisterP3FEvents(HubConnection connection)
    {
        var events = new[]
        {
            "ReceiveWelcome",
            "ReceiveRoomCreated",
            "ReceiveRoomJoined",
            "ReceiveRoomLeft",
            "ReceiveRoomList",
            "ReceiveTableCreated",
            "ReceiveTableState",
            "ReceiveSeatAssigned",
            "ReceiveGameStarted",
            "ReceiveActionAccepted",
            "ReceiveActionRejected",
            "ReceiveAuthoritativeSnapshot",
            "ReceiveActionLogChunk",
            "ReceiveResyncRequired",
            "ReceiveMatchmakingStatus",
            "ReceiveMatchmakingCancelled",
            "ReceiveMatchFound",
            "ReceiveMatchmakingError",
            "ReceivePong",
            "ReceiveError",
            "ReceiveDiagnostics"
        };
        foreach (var eventName in events)
        {
            connection.On<OnlineProtocolMessage>(eventName, message => Dispatcher.Invoke(() => LogP3FResult(eventName, message)));
        }
        connection.Closed += error =>
        {
            Dispatcher.Invoke(() =>
            {
                P3FStatusText.Text = error == null ? "SignalR closed." : $"SignalR closed: {error.Message}";
                Log(P3FStatusText.Text);
            });
            return Task.CompletedTask;
        };
    }

    private async Task<OnlineProtocolMessage> P3FInvokeAsync(string methodName, OnlineProtocolMessage message)
    {
        if (_p3fConnection == null || _p3fConnection.State != HubConnectionState.Connected)
        {
            var error = $"SignalR is not connected. Start ChessOnlineServer and click Connect.";
            P3FStatusText.Text = error;
            Log(error);
            return new OnlineProtocolMessage { Error = OnlineProtocolJson.Error(OnlineRejectReasons.IllegalAction, error) };
        }

        try
        {
            var result = await _p3fConnection.InvokeAsync<OnlineProtocolMessage>(methodName, message);
            LogP3FResult(methodName, result);
            if (!string.IsNullOrWhiteSpace(result.Envelope.SessionToken))
            {
                _p3fSessionToken = result.Envelope.SessionToken;
            }
            return result;
        }
        catch (Exception ex)
        {
            P3FStatusText.Text = $"{methodName} failed: {ex.Message}";
            Log(P3FStatusText.Text);
            return new OnlineProtocolMessage { Error = OnlineProtocolJson.Error(OnlineRejectReasons.InternalError, ex.Message) };
        }
    }

    private OnlineProtocolMessage P3FMessage(string messageType)
    {
        var message = new OnlineProtocolMessage
        {
            Envelope = P3EEnvelope(messageType)
        };
        message.Envelope.SessionToken = _p3fSessionToken;
        return message;
    }

    private void LogP3FResult(string label, OnlineProtocolMessage message)
    {
        var summary = message.Error is { ReasonCode.Length: > 0 } error
            ? $"P3F {label}: {message.Envelope.MessageType} {error.ReasonCode} {error.ReasonText}"
            : $"P3F {label}: {message.Envelope.MessageType} seq={message.Envelope.ServerSeq}";
        P3FStatusText.Text = summary;
        Log(summary);
        if (!string.IsNullOrWhiteSpace(message.Envelope.SessionToken))
        {
            Log("P3F session token received for local reconnect; token is not printed.");
        }
        if (message.Snapshot != null)
        {
            Log($"P3F snapshot hash {message.Snapshot.StateHash}, actions {message.Snapshot.ActionCount}");
        }
        if (message.ActionLog?.Events.Count > 0)
        {
            foreach (var actionEvent in message.ActionLog.Events)
            {
                Log($"P3F event #{actionEvent.ServerSeq}: {actionEvent.Notation} hash={actionEvent.StateHashAfter}");
            }
        }
        if (message.MatchmakingStatus != null)
        {
            var mm = message.MatchmakingStatus;
            if (!string.IsNullOrWhiteSpace(mm.RoomId))
            {
                P3ERoomBox.Text = mm.RoomId;
            }
            if (!string.IsNullOrWhiteSpace(mm.TableId))
            {
                P3ETableBox.Text = mm.TableId;
            }
            Log($"P3F matchmaking {mm.State}: ruleset={mm.RequestedRulesetId} queue={mm.QueueCount} room={mm.RoomId} table={mm.TableId} seat={mm.SeatIndex}");
        }
        if (message.Diagnostics != null)
        {
            Log(JsonSerializer.Serialize(message.Diagnostics, OnlineProtocolJson.Options));
        }
    }

    private string SelectedP3FMatchmakingRuleset()
    {
        if (P3FMatchmakingProfileBox.SelectedItem is ComboBoxItem item && item.Content is string text)
        {
            return text;
        }
        return "classic-six-side-3d-8x8x8-v0.1";
    }

    private OnlineMessageEnvelope P3EEnvelope(string messageType)
    {
        return new OnlineMessageEnvelope
        {
            MessageType = messageType,
            MessageId = Guid.NewGuid().ToString("N"),
            RoomId = P3ERoomBox.Text.Trim(),
            TableId = P3ETableBox.Text.Trim(),
            ClientId = "online-app-local",
            PlayerId = string.IsNullOrWhiteSpace(P3EPlayerBox.Text) ? "player-1" : P3EPlayerBox.Text.Trim(),
            ClientSeq = DateTime.UtcNow.Ticks,
            SentAtUtc = DateTime.UtcNow.ToString("O")
        };
    }

    private string SelectedP3ERuleset()
    {
        if (P3EProfileBox.SelectedItem is ComboBoxItem item && item.Content is string text)
        {
            return text;
        }
        return "classic-six-side-3d-8x8x8-v0.1";
    }

    private void LogP3EResult(string label, OnlineProtocolMessage message)
    {
        var summary = message.Error is { ReasonCode.Length: > 0 } error
            ? $"{label}: {message.Envelope.MessageType} {error.ReasonCode} {error.ReasonText}"
            : $"{label}: {message.Envelope.MessageType} seq={message.Envelope.ServerSeq}";
        P3EStatusText.Text = summary;
        Log(summary);
        if (message.Snapshot != null)
        {
            Log($"P3E snapshot hash {message.Snapshot.StateHash}, actions {message.Snapshot.ActionCount}");
        }
        if (message.ActionLog?.Events.Count > 0)
        {
            foreach (var actionEvent in message.ActionLog.Events)
            {
                Log($"P3E event #{actionEvent.ServerSeq}: {actionEvent.Notation} hash={actionEvent.StateHashAfter}");
            }
        }
    }

    private static string ResolveP3EProfileRoot()
    {
        var outputRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "Rules3D", "Profiles");
        if (Directory.Exists(outputRoot))
        {
            return outputRoot;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "rules", "profiles");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        return outputRoot;
    }

    private static bool TryParseCell(string text, out (int X, int Y, int Z) cell)
    {
        cell = default;
        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out var x) ||
            !int.TryParse(parts[1], out var y) ||
            !int.TryParse(parts[2], out var z))
        {
            return false;
        }
        cell = (x, y, z);
        return x >= 0 && x < 8 && y >= 0 && y < 8 && z >= 0 && z < 8;
    }

    private async Task SaveProfileAsync(IntegrationAccountProfile profile, string source)
    {
        try
        {
            var saved = await _accountStore.UpsertAsync(profile);
            await ReloadAccountsAsync();
            Log($"Saved integration profile ({source}): {saved.Summary}");
            Log($"Integration store: {_accountStore.StorePath}");
            if (saved.HasSecret)
            {
                Log("Secret value is not written to JSON; the profile only records that a session secret was used.");
            }
        }
        catch (Exception ex)
        {
            Log($"Saving integration profile failed: {ex.Message}");
        }
    }

    private async Task ReloadAccountsAsync()
    {
        try
        {
            var document = await _accountStore.LoadAsync();
            _accountProfiles.Clear();
            foreach (var profile in document.Accounts)
            {
                _accountProfiles.Add(profile);
            }

            AccountStorePathText.Text = $"Store: {_accountStore.StorePath}";
        }
        catch (Exception ex)
        {
            Log($"Loading integration profiles failed: {ex.Message}");
        }
    }

    private void Log(string message)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => Log(message));
            return;
        }

        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogBox.ScrollToEnd();
    }

    private static int ReadInt(TextBox box, int fallback, int min, int max)
    {
        if (!int.TryParse(box.Text, out var value))
        {
            value = fallback;
        }
        value = Math.Clamp(value, min, max);
        box.Text = value.ToString();
        return value;
    }

    private string GuessUsernameForPortal(string portalId)
    {
        if (string.Equals(portalId, "chesscom", StringComparison.OrdinalIgnoreCase))
        {
            return ChessComUserBox.Text.Trim();
        }

        if (string.Equals(portalId, "fics", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(portalId, "icc", StringComparison.OrdinalIgnoreCase))
        {
            return IcsUserBox.Text.Trim();
        }

        return "";
    }

    private string GuessAccessModeForPortal(string portalId)
    {
        if (string.Equals(portalId, "lichess", StringComparison.OrdinalIgnoreCase))
        {
            return LichessModeBox.SelectedIndex == 1 ? "bot" : "board";
        }

        return "";
    }

    private static string TryReadJsonString(string json, params string[] propertyNames)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            foreach (var propertyName in propertyNames)
            {
                if (document.RootElement.TryGetProperty(propertyName, out var property))
                {
                    return property.ValueKind == JsonValueKind.String
                        ? property.GetString() ?? ""
                        : property.ToString();
                }
            }
        }
        catch (JsonException)
        {
        }

        return "";
    }
}
