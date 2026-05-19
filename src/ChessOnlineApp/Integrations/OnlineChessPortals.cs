using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ChessApp;

[Flags]
internal enum ChessPortalCapability
{
    None = 0,
    PublicProfile = 1 << 0,
    PublicGameArchive = 1 << 1,
    CurrentDailyGames = 1 << 2,
    LiveGameStream = 1 << 3,
    OfficialMoveSubmit = 1 << 4,
    BotPlay = 1 << 5,
    Correspondence = 1 << 6,
    TextServer = 1 << 7,
    RequiresPartnerProgram = 1 << 8,
    ManualImportOnly = 1 << 9,
    Custom3DRelay = 1 << 10,
    SixPlayer3D = 1 << 11
}

internal enum ChessPortalAuthKind
{
    None,
    BearerToken,
    UsernamePassword,
    ExternalClient,
    PartnerContract
}

internal sealed record ChessPortalDescriptor(
    string Id,
    string DisplayName,
    Uri HomeUri,
    ChessPortalCapability Capabilities,
    ChessPortalAuthKind AuthKind,
    string Transport,
    string Notes);

internal sealed record OnlineGameSnapshot(
    string PortalId,
    string GameId,
    string Fen,
    string Pgn,
    string SideToMove,
    string RawJson);

internal interface IOnlineChessPortalClient : IDisposable
{
    ChessPortalDescriptor Descriptor { get; }
    Task<string> GetAccountOrProfileJsonAsync(string username, CancellationToken cancellationToken = default);
    IAsyncEnumerable<OnlineGameSnapshot> GetCurrentGamesAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> SubmitMoveAsync(string gameId, string uciMove, CancellationToken cancellationToken = default);
}

internal static class ChessPortalRegistry
{
    public static IReadOnlyList<ChessPortalDescriptor> All { get; } = new ReadOnlyCollection<ChessPortalDescriptor>(new[]
    {
        new ChessPortalDescriptor(
            "lichess",
            "Lichess",
            new Uri("https://lichess.org"),
            ChessPortalCapability.PublicProfile | ChessPortalCapability.PublicGameArchive | ChessPortalCapability.LiveGameStream | ChessPortalCapability.OfficialMoveSubmit | ChessPortalCapability.BotPlay,
            ChessPortalAuthKind.BearerToken,
            "HTTPS NDJSON Board API / Bot API",
            "Best-supported live integration. Keep Board API human/board play separate from Bot API engine play."),

        new ChessPortalDescriptor(
            "chessadvisor3d",
            "ChessAdvisor 3D Web Platform",
            new Uri("https://localhost/chess3d"),
            ChessPortalCapability.Custom3DRelay | ChessPortalCapability.SixPlayer3D | ChessPortalCapability.OfficialMoveSubmit,
            ChessPortalAuthKind.BearerToken,
            "WebSocket relay + HTTPS room API",
            "Future hosted platform for 8x8x8 six-sided chess rooms, group bridges, board sync, 3D moves, layer rotations and chat."),

        new ChessPortalDescriptor(
            "chesscom",
            "Chess.com",
            new Uri("https://www.chess.com"),
            ChessPortalCapability.PublicProfile | ChessPortalCapability.PublicGameArchive | ChessPortalCapability.CurrentDailyGames,
            ChessPortalAuthKind.None,
            "HTTPS read-only PubAPI",
            "Official Published Data API is read-only; it cannot send game moves or commands."),

        new ChessPortalDescriptor(
            "chesskid",
            "ChessKid",
            new Uri("https://www.chesskid.com"),
            ChessPortalCapability.ManualImportOnly | ChessPortalCapability.RequiresPartnerProgram,
            ChessPortalAuthKind.PartnerContract,
            "No public move API documented",
            "Treat as Chess.com-family but not Chess.com PubAPI-compatible unless a partner API is provided."),

        new ChessPortalDescriptor(
            "worldchess",
            "World Chess / FIDE Online Arena",
            new Uri("https://worldchess.com"),
            ChessPortalCapability.ManualImportOnly | ChessPortalCapability.RequiresPartnerProgram,
            ChessPortalAuthKind.PartnerContract,
            "Official web platform, no public gameplay API documented",
            "Use PGN/FEN import or a future approved partner adapter."),

        new ChessPortalDescriptor(
            "playchess",
            "ChessBase / Playchess.com",
            new Uri("https://play.chessbase.com"),
            ChessPortalCapability.ManualImportOnly | ChessPortalCapability.RequiresPartnerProgram,
            ChessPortalAuthKind.ExternalClient,
            "ChessBase ecosystem",
            "Best handled via ChessBase exports or an approved local bridge."),

        new ChessPortalDescriptor(
            "icc",
            "ICC - Internet Chess Club",
            new Uri("https://www.chessclub.com"),
            ChessPortalCapability.TextServer | ChessPortalCapability.RequiresPartnerProgram,
            ChessPortalAuthKind.UsernamePassword,
            "ICS-style text protocol / proprietary clients",
            "Can share the generic ICS text adapter if account/server terms allow it."),

        new ChessPortalDescriptor(
            "fics",
            "FICS - Free Internet Chess Server",
            new Uri("https://www.freechess.org"),
            ChessPortalCapability.TextServer | ChessPortalCapability.OfficialMoveSubmit,
            ChessPortalAuthKind.UsernamePassword,
            "Telnet ICS text protocol",
            "Historical free server; adapter uses line-oriented ICS commands."),

        new ChessPortalDescriptor(
            "gameknot",
            "GameKnot",
            new Uri("https://gameknot.com"),
            ChessPortalCapability.Correspondence | ChessPortalCapability.ManualImportOnly,
            ChessPortalAuthKind.ExternalClient,
            "No public move API documented",
            "Start with PGN/FEN import/export for correspondence games."),

        new ChessPortalDescriptor(
            "chess24",
            "Chess24",
            new Uri("https://www.chess.com/events"),
            ChessPortalCapability.ManualImportOnly,
            ChessPortalAuthKind.None,
            "Closed site, Chess.com-family content",
            "The former chess24 play site is closed; use Chess.com/Chessable content paths instead."),

        new ChessPortalDescriptor(
            "chessable",
            "Chessable",
            new Uri("https://www.chessable.com"),
            ChessPortalCapability.ManualImportOnly | ChessPortalCapability.RequiresPartnerProgram,
            ChessPortalAuthKind.ExternalClient,
            "Training/course platform",
            "Not a live-play chess server; useful future target for course/FEN/PGN import, not game moves.")
    });

    public static ChessPortalDescriptor Get(string id)
    {
        return All.First(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed class ChessComPublishedDataClient : IOnlineChessPortalClient
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    public ChessComPublishedDataClient(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient { BaseAddress = new Uri("https://api.chess.com") };
        _ownsHttpClient = httpClient == null;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ChessAdvisor/1.0");
    }

    public ChessPortalDescriptor Descriptor => ChessPortalRegistry.Get("chesscom");

    public Task<string> GetAccountOrProfileJsonAsync(string username, CancellationToken cancellationToken = default)
    {
        return _http.GetStringAsync($"/pub/player/{Uri.EscapeDataString(username)}", cancellationToken);
    }

    public async IAsyncEnumerable<OnlineGameSnapshot> GetCurrentGamesAsync(string username, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var json = await _http.GetStringAsync($"/pub/player/{Uri.EscapeDataString(username)}/games", cancellationToken);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("games", out var games))
        {
            yield break;
        }

        foreach (var game in games.EnumerateArray())
        {
            var url = game.TryGetProperty("url", out var urlElement) ? urlElement.GetString() ?? "" : "";
            yield return new OnlineGameSnapshot(
                Descriptor.Id,
                url,
                game.TryGetProperty("fen", out var fen) ? fen.GetString() ?? "" : "",
                game.TryGetProperty("pgn", out var pgn) ? pgn.GetString() ?? "" : "",
                game.TryGetProperty("turn", out var turn) ? turn.GetString() ?? "" : "",
                game.GetRawText());
        }
    }

    public Task<bool> SubmitMoveAsync(string gameId, string uciMove, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }
}

internal sealed class IcsTextChessClient : IDisposable
{
    private readonly TcpClient _client = new();
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public event Action<string>? LineReceived;
    public event Action<string>? StatusChanged;

    public bool IsConnected => _client.Connected;

    public async Task ConnectAsync(string host, int port, string username, string password, CancellationToken cancellationToken = default)
    {
        await _client.ConnectAsync(host, port, cancellationToken);
        var stream = _client.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        _writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        StatusChanged?.Invoke($"ICS: connected to {host}:{port}");
        _ = ReadLoopAsync(cancellationToken);
        await SendLineAsync(username, cancellationToken);
        if (!string.IsNullOrEmpty(password))
        {
            await SendLineAsync(password, cancellationToken);
        }
    }

    public Task SendMoveAsync(string moveText, CancellationToken cancellationToken = default)
    {
        return SendLineAsync(moveText, cancellationToken);
    }

    public Task ObserveAsync(string gameId, CancellationToken cancellationToken = default)
    {
        return SendLineAsync($"observe {gameId}", cancellationToken);
    }

    public Task SeekAsync(string timeControl, CancellationToken cancellationToken = default)
    {
        return SendLineAsync($"seek {timeControl}", cancellationToken);
    }

    public void Dispose()
    {
        _reader?.Dispose();
        _writer?.Dispose();
        _client.Dispose();
    }

    private async Task SendLineAsync(string line, CancellationToken cancellationToken)
    {
        if (_writer == null)
        {
            return;
        }
        await _writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        await _writer.FlushAsync(cancellationToken);
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_reader != null && !cancellationToken.IsCancellationRequested)
            {
                var line = await _reader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    break;
                }
                LineReceived?.Invoke(line);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException ex)
        {
            StatusChanged?.Invoke($"ICS: disconnected, {ex.Message}");
        }
    }
}

internal sealed class UnsupportedPortalClient : IOnlineChessPortalClient
{
    public UnsupportedPortalClient(ChessPortalDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

    public ChessPortalDescriptor Descriptor { get; }

    public Task<string> GetAccountOrProfileJsonAsync(string username, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("{}");
    }

    public async IAsyncEnumerable<OnlineGameSnapshot> GetCurrentGamesAsync(string username, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    public Task<bool> SubmitMoveAsync(string gameId, string uciMove, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public void Dispose()
    {
    }
}
