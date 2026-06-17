using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ChessOnlineServer.Security;

public sealed class ChessOnlineAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthScheme = "Chess3DToken";

    private readonly OnlineTokenService _tokens;

    public ChessOnlineAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        OnlineTokenService tokens)
        : base(options, logger, encoder)
    {
        _tokens = tokens;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ReadBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var validation = await _tokens.ValidateAccessTokenAsync(token, Context.RequestAborted);
        if (!validation.Success || validation.Account == null || validation.Session == null)
        {
            return AuthenticateResult.Fail(validation.ErrorCode);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, validation.Account.PlayerId),
            new Claim(ClaimTypes.Name, validation.Account.DisplayName),
            new Claim("playerId", validation.Account.PlayerId),
            new Claim("sessionId", validation.Session.SessionId)
        };
        var identity = new ClaimsIdentity(claims, AuthScheme);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), AuthScheme));
    }

    private string ReadBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return header["Bearer ".Length..].Trim();
        }

        // SignalR WebSocket clients commonly pass the bearer token via access_token.
        if (Request.Query.TryGetValue("access_token", out var values))
        {
            return values.FirstOrDefault() ?? "";
        }
        return "";
    }
}
