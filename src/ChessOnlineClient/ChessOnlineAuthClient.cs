using System.Net.Http.Json;

namespace ChessOnlineClient;

public sealed class ChessOnlineAuthClient
{
    private readonly HttpClient _http;
    private readonly ChessOnlineServerEndpoint _endpoint;

    public ChessOnlineAuthClient(HttpClient http, ChessOnlineServerEndpoint endpoint)
    {
        _http = http;
        _endpoint = endpoint;
    }

    public async Task<ChessOnlineAuthTokenResponse> RegisterTemporaryUserAsync(
        string prefix = "p4f_test",
        string clientName = "chess-online-client",
        CancellationToken cancellationToken = default)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var userName = $"{prefix}_{suffix}";
        var password = CreateTemporaryPassword(suffix);
        return await RegisterAsync(userName, $"P4F Test {suffix}", password, clientName, cancellationToken);
    }

    public async Task<ChessOnlineAuthTokenResponse> RegisterAsync(
        string userName,
        string displayName,
        string password,
        string clientName,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            _endpoint.RegisterUri,
            new ChessOnlineAuthRegisterRequest(userName, displayName, password, clientName),
            cancellationToken);
        return await ReadTokenResponseAsync(response, cancellationToken);
    }

    public async Task<ChessOnlineAuthTokenResponse> LoginAsync(
        string userName,
        string password,
        string clientName,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            _endpoint.LoginUri,
            new ChessOnlineAuthLoginRequest(userName, password, clientName),
            cancellationToken);
        return await ReadTokenResponseAsync(response, cancellationToken);
    }

    public async Task<ChessOnlineAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(_endpoint.RefreshUri, new ChessOnlineAuthRefreshRequest(refreshToken), cancellationToken);
        return await ReadTokenResponseAsync(response, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        await _http.PostAsJsonAsync(_endpoint.LogoutUri, new ChessOnlineAuthRefreshRequest(refreshToken), cancellationToken);
    }

    private static async Task<ChessOnlineAuthTokenResponse> ReadTokenResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var token = await response.Content.ReadFromJsonAsync<ChessOnlineAuthTokenResponse>(cancellationToken: cancellationToken);
        if (token != null)
        {
            return token;
        }

        return new ChessOnlineAuthTokenResponse
        {
            Success = false,
            ErrorCode = $"http{(int)response.StatusCode}",
            ErrorText = "Auth endpoint returned an empty response."
        };
    }

    private static string CreateTemporaryPassword(string suffix)
    {
        return $"P4F-{suffix}-Temp!2026";
    }
}
