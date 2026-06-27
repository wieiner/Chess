namespace ChessOnlineClient;

public sealed class ChessOnlineClientSession
{
    public ChessOnlineServerEndpoint Endpoint { get; }
    public string ClientName { get; }
    public ChessOnlineAuthTokenResponse? Token { get; private set; }

    public ChessOnlineClientSession(ChessOnlineServerEndpoint endpoint, string clientName)
    {
        Endpoint = endpoint;
        ClientName = string.IsNullOrWhiteSpace(clientName) ? "chess-online-client" : clientName.Trim();
    }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token?.AccessToken);

    public string PlayerId => Token?.PlayerId ?? "";

    public string UserName => Token?.UserName ?? "";

    public string RedactedStatus => IsAuthenticated
        ? $"authenticated player={Short(PlayerId)} user={UserName}"
        : "anonymous";

    public void SetToken(ChessOnlineAuthTokenResponse token) => Token = token;

    public void ClearToken() => Token = null;

    private static string Short(string value) => value.Length <= 8 ? value : value[..8];
}
