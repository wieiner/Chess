namespace ChessOnlineServer.Security;

public sealed class OnlineTokenOptions
{
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}
