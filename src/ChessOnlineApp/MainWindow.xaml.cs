using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using ChessApp;

namespace ChessOnlineApp;

public partial class MainWindow : Window
{
    private readonly IIntegrationAccountStore _accountStore = new JsonIntegrationAccountStore();
    private readonly ObservableCollection<IntegrationAccountProfile> _accountProfiles = new();
    private IcsTextChessClient? _icsClient;
    private Chess3DInternetRelayClient? _relayClient;

    public MainWindow()
    {
        InitializeComponent();
        PortalGrid.ItemsSource = ChessPortalRegistry.All;
        AccountListBox.ItemsSource = _accountProfiles;
        AccountStorePathText.Text = $"Store: {_accountStore.StorePath}";
        PortalGrid.SelectedIndex = 0;
        _ = ReloadAccountsAsync();
        Log("Online hub started. Main chess boards are intentionally separate.");
    }

    protected override void OnClosed(EventArgs e)
    {
        _icsClient?.Dispose();
        _relayClient?.Dispose();
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
