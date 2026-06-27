namespace ChessOnlineClient;

public sealed record ChessOnlineServerEndpoint(Uri BaseUri)
{
    public static ChessOnlineServerEndpoint FromBaseUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Base URL is required.", nameof(value));
        }

        var trimmed = value.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/chess3d/relay", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^"/chess3d/relay".Length];
        }

        trimmed = trimmed.TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Base URL must be an absolute HTTP or HTTPS URL.", nameof(value));
        }

        return new ChessOnlineServerEndpoint(uri);
    }

    public Uri LiveHealthUri => Combine("/healthz/live");

    public Uri ReadyHealthUri => Combine("/healthz/ready");

    public Uri DiagnosticsUri => Combine("/chess3d/diagnostics");

    public Uri RegisterUri => Combine("/api/auth/register");

    public Uri LoginUri => Combine("/api/auth/login");

    public Uri RefreshUri => Combine("/api/auth/refresh");

    public Uri LogoutUri => Combine("/api/auth/logout");

    public Uri HubUri => Combine("/chess3d/relay");

    public bool IsDiagnosticHttp => BaseUri.Scheme == Uri.UriSchemeHttp && !IsLoopbackHost(BaseUri.Host);

    public override string ToString() => BaseUri.ToString().TrimEnd('/');

    private Uri Combine(string path) => new(BaseUri, path);

    private static bool IsLoopbackHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }
}
