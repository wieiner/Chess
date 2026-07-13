using System.Security.Claims;
using System.Threading.RateLimiting;

namespace ChessOnlineServer;

public static class OnlineRateLimitPolicies
{
    public const string Register = "online-auth-register";
    public const string Login = "online-auth-login";
    public const string Session = "online-auth-session";
    public const string Diagnostics = "online-public-diagnostics";

    public static RateLimitPartition<string> FixedWindow(
        HttpContext context,
        int permitLimit,
        int windowSeconds)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            BuildPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }

    public static string BuildPartitionKey(HttpContext context)
    {
        var playerId = context.User.FindFirst("playerId")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(playerId))
        {
            return $"player:{playerId.Trim().ToUpperInvariant()}";
        }

        var address = context.Connection.RemoteIpAddress;
        return address == null ? "ip:unknown" : $"ip:{address}";
    }
}
