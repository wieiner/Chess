using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace ChessApp;

internal enum LichessAccessMode
{
    Board,
    Bot
}

internal sealed class LichessClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private readonly bool _ownsHttpClient;

    public LichessClient(string token, HttpClient? httpClient = null, Uri? baseUri = null)
    {
        _http = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient == null;
        _http.BaseAddress = baseUri ?? new Uri("https://lichess.org");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ChessAdvisor/1.0");
    }

    public Task<string> GetAccountJsonAsync(CancellationToken cancellationToken = default)
    {
        return GetStringAsync("/api/account", cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamIncomingEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var line in StreamLinesAsync("/api/stream/event", cancellationToken))
        {
            yield return line;
        }
    }

    public async IAsyncEnumerable<string> StreamGameAsync(
        string gameId,
        LichessAccessMode mode,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = mode == LichessAccessMode.Bot
            ? $"/api/bot/game/stream/{Uri.EscapeDataString(gameId)}"
            : $"/api/board/game/stream/{Uri.EscapeDataString(gameId)}";
        await foreach (var line in StreamLinesAsync(path, cancellationToken))
        {
            yield return line;
        }
    }

    public Task<bool> MakeMoveAsync(string gameId, string uciMove, LichessAccessMode mode, CancellationToken cancellationToken = default)
    {
        var path = mode == LichessAccessMode.Bot
            ? $"/api/bot/game/{Uri.EscapeDataString(gameId)}/move/{Uri.EscapeDataString(uciMove)}"
            : $"/api/board/game/{Uri.EscapeDataString(gameId)}/move/{Uri.EscapeDataString(uciMove)}";
        return PostOkAsync(path, cancellationToken);
    }

    public async Task<bool> WriteChatAsync(string gameId, string room, string text, LichessAccessMode mode, CancellationToken cancellationToken = default)
    {
        var path = mode == LichessAccessMode.Bot
            ? $"/api/bot/game/{Uri.EscapeDataString(gameId)}/chat"
            : $"/api/board/game/{Uri.EscapeDataString(gameId)}/chat";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["room"] = room,
            ["text"] = text
        });
        return await PostOkAsync(path, cancellationToken, content);
    }

    public async Task<bool> ChallengeAsync(string username, string clockLimitSeconds, string clockIncrementSeconds, CancellationToken cancellationToken = default)
    {
        var path = $"/api/challenge/{Uri.EscapeDataString(username)}";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["rated"] = "false",
            ["clock.limit"] = clockLimitSeconds,
            ["clock.increment"] = clockIncrementSeconds
        });
        return await PostOkAsync(path, cancellationToken, content);
    }

    public void Dispose()
    {
        _requestGate.Dispose();
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }

    private async Task<string> GetStringAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await SendWithLichessBackoffAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async IAsyncEnumerable<string> StreamLinesAsync(string path, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await SendWithLichessBackoffAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line != null && !string.IsNullOrWhiteSpace(line))
            {
                yield return line;
            }
        }
    }

    private async Task<bool> PostOkAsync(string path, CancellationToken cancellationToken, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content
        };
        using var response = await SendWithLichessBackoffAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<HttpResponseMessage> SendWithLichessBackoffAsync(
        HttpRequestMessage request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
        await _requestGate.WaitAsync(cancellationToken);
        try
        {
            var response = await _http.SendAsync(request, completionOption, cancellationToken);
            if (response.StatusCode != (HttpStatusCode)429)
            {
                return response;
            }
            if (request.Content != null)
            {
                return response;
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            var retry = CloneRequest(request);
            return await _http.SendAsync(retry, completionOption, cancellationToken);
        }
        finally
        {
            _requestGate.Release();
        }
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        if (request.Content != null)
        {
            throw new InvalidOperationException("Automatic retry with request content is not supported after a 429 response.");
        }
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }
}
