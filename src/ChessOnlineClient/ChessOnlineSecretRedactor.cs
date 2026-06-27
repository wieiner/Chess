using System.Text.RegularExpressions;

namespace ChessOnlineClient;

public static partial class ChessOnlineSecretRedactor
{
    public static string Redact(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var redacted = AuthorizationBearerPattern().Replace(value, "$1: Bearer <redacted>");
        redacted = BearerPattern().Replace(redacted, "Bearer <redacted>");
        return TokenLikePattern().Replace(redacted, "$1=<redacted>");
    }

    [GeneratedRegex(@"(?i)\b(Authorization)\s*:\s*Bearer\s+[A-Za-z0-9._~+/=-]+")]
    private static partial Regex AuthorizationBearerPattern();

    [GeneratedRegex(@"(?i)\b(accessToken|refreshToken|password|authorization|bearer)\s*[:=]\s*[^,\s;]+")]
    private static partial Regex TokenLikePattern();

    [GeneratedRegex(@"(?i)Bearer\s+[A-Za-z0-9._~+/=-]+")]
    private static partial Regex BearerPattern();
}
