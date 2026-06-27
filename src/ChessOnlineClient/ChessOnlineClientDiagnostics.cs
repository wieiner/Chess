namespace ChessOnlineClient;

public sealed record ChessOnlineClientDiagnostics(
    string BaseUrl,
    string HubUrl,
    bool DiagnosticHttp,
    bool Authenticated,
    string PlayerId,
    string UserName,
    int EventCount);
