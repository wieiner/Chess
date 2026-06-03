using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using ChessApp;
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
    }

    protected override void OnClosed(EventArgs e)
    {
        _icsClient?.Dispose();
        _relayClient?.Dispose();
        if (_p3fConnection != null)
        {
            _p3fConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
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

    private async void P3FConnect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_p3fConnection != null)
            {
                await _p3fConnection.DisposeAsync();
            }

            _p3fConnection = new HubConnectionBuilder()
                .WithUrl(P3FServerUrlBox.Text.Trim())
                .Build();
            RegisterP3FEvents(_p3fConnection);
            await _p3fConnection.StartAsync();
            P3FStatusText.Text = "SignalR connected.";
            Log("P3F SignalR connected.");
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
        if (message.Diagnostics != null)
        {
            Log(JsonSerializer.Serialize(message.Diagnostics, OnlineProtocolJson.Options));
        }
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
