namespace ChessOnlineServer.Security;

public sealed class ProtectedTokenPayload
{
    public string TokenType { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string SessionId { get; set; } = "";
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
