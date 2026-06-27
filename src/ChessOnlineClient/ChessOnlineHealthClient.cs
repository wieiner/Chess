using System.Net.Http.Json;
using System.Text.Json;
using ChessOnlineProtocol;

namespace ChessOnlineClient;

public sealed class ChessOnlineHealthClient
{
    private readonly HttpClient _http;
    private readonly ChessOnlineServerEndpoint _endpoint;

    public ChessOnlineHealthClient(HttpClient http, ChessOnlineServerEndpoint endpoint)
    {
        _http = http;
        _endpoint = endpoint;
    }

    public async Task<string> GetLiveAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetStringAsync(_endpoint.LiveHealthUri, cancellationToken);
    }

    public async Task<ChessOnlineReadyStatus> GetReadyAsync(CancellationToken cancellationToken = default)
    {
        using var document = await JsonDocument.ParseAsync(
            await _http.GetStreamAsync(_endpoint.ReadyHealthUri, cancellationToken),
            cancellationToken: cancellationToken);
        var root = document.RootElement;
        return new ChessOnlineReadyStatus(
            GetString(root, "status"),
            GetInt(root, "profileCount"),
            GetBool(root, "authEnabled"));
    }

    public async Task<ChessOnlineDiagnosticsStatus> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        using var document = await JsonDocument.ParseAsync(
            await _http.GetStreamAsync(_endpoint.DiagnosticsUri, cancellationToken),
            cancellationToken: cancellationToken);
        var root = document.RootElement;
        var diagnostics = root.Deserialize<OnlineDiagnostics>() ?? new OnlineDiagnostics();
        return new ChessOnlineDiagnosticsStatus(
            GetBool(root, "authEnabled"),
            GetBool(root, "authorityIsSupported"),
            GetString(root, "authorityPlatform"),
            GetString(root, "authorityNativeLibraryName"),
            diagnostics ?? new OnlineDiagnostics());
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetString() ?? "" : "";
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result) ? result : 0;
    }

    private static bool GetBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.True;
    }
}

public sealed record ChessOnlineReadyStatus(string Status, int ProfileCount, bool AuthEnabled);

public sealed record ChessOnlineDiagnosticsStatus(
    bool AuthEnabled,
    bool AuthorityIsSupported,
    string AuthorityPlatform,
    string AuthorityNativeLibraryName,
    OnlineDiagnostics Diagnostics);
