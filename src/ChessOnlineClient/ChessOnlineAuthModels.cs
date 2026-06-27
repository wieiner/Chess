namespace ChessOnlineClient;

public sealed record ChessOnlineAuthRegisterRequest(string UserName, string DisplayName, string Password, string ClientName);

public sealed record ChessOnlineAuthLoginRequest(string UserName, string Password, string ClientName);

public sealed record ChessOnlineAuthRefreshRequest(string RefreshToken);

public sealed class ChessOnlineAuthTokenResponse
{
    public bool Success { get; set; }
    public string PlayerId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string ExpiresAtUtc { get; set; } = "";
    public string ErrorCode { get; set; } = "";
    public string ErrorText { get; set; } = "";
}
