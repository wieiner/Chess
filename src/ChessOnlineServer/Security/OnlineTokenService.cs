using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChessOnlinePersistence;
using ChessOnlinePersistence.Entities;
using Microsoft.AspNetCore.DataProtection;

namespace ChessOnlineServer.Security;

public sealed class OnlineTokenService
{
    public const string AccessTokenType = "access";
    public const string RefreshTokenType = "refresh";
    private const string ProtectorPurpose = "Chess3D.Online.P4A.Token.v0.1";

    private readonly IDataProtector _protector;
    private readonly OnlineAccountService _accounts;
    private readonly OnlineTokenOptions _options;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public OnlineTokenService(IDataProtectionProvider dataProtection, OnlineAccountService accounts, OnlineTokenOptions options)
    {
        _protector = dataProtection.CreateProtector(ProtectorPurpose);
        _accounts = accounts;
        _options = options;
    }

    public string CreateAccessToken(PlayerAccountEntity account, PlayerSessionEntity session)
    {
        return Protect(new ProtectedTokenPayload
        {
            TokenType = AccessTokenType,
            PlayerId = account.PlayerId,
            SessionId = session.SessionId,
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes)
        });
    }

    public string CreateRefreshToken(PlayerAccountEntity account, PlayerSessionEntity session)
    {
        return Protect(new ProtectedTokenPayload
        {
            TokenType = RefreshTokenType,
            PlayerId = account.PlayerId,
            SessionId = session.SessionId,
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = session.ExpiresAtUtc
        });
    }

    public async Task<OnlineTokenValidationResult> ValidateAccessTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var payload = Unprotect(token);
        if (payload == null || !string.Equals(payload.TokenType, AccessTokenType, StringComparison.Ordinal))
        {
            return OnlineTokenValidationResult.Fail("invalidToken");
        }
        return await ValidatePayloadSessionAsync(payload, cancellationToken);
    }

    public async Task<OnlineTokenValidationResult> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var payload = Unprotect(token);
        if (payload == null || !string.Equals(payload.TokenType, RefreshTokenType, StringComparison.Ordinal))
        {
            return OnlineTokenValidationResult.Fail("invalidToken");
        }
        var result = await ValidatePayloadSessionAsync(payload, cancellationToken);
        if (!result.Success || result.Session == null)
        {
            return result;
        }
        return string.Equals(result.Session.RefreshTokenHash, HashToken(token), StringComparison.Ordinal)
            ? result
            : OnlineTokenValidationResult.Fail("revokedToken");
    }

    public static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token ?? ""))).ToLowerInvariant();
    }

    private async Task<OnlineTokenValidationResult> ValidatePayloadSessionAsync(ProtectedTokenPayload payload, CancellationToken cancellationToken)
    {
        if (payload.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return OnlineTokenValidationResult.Fail("expiredToken");
        }

        var account = await _accounts.FindPlayerAsync(payload.PlayerId, cancellationToken);
        var session = await _accounts.FindSessionAsync(payload.SessionId, cancellationToken);
        if (account == null || account.Disabled || session == null ||
            !string.Equals(session.PlayerId, account.PlayerId, StringComparison.OrdinalIgnoreCase))
        {
            return OnlineTokenValidationResult.Fail("invalidSession");
        }
        if (session.RevokedAtUtc != null || session.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return OnlineTokenValidationResult.Fail("revokedSession");
        }
        return OnlineTokenValidationResult.Ok(account, session);
    }

    private string Protect(ProtectedTokenPayload payload)
    {
        return _protector.Protect(JsonSerializer.Serialize(payload, _jsonOptions));
    }

    private ProtectedTokenPayload? Unprotect(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<ProtectedTokenPayload>(_protector.Unprotect(token), _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}

public sealed class OnlineTokenValidationResult
{
    public bool Success { get; init; }
    public string ErrorCode { get; init; } = "";
    public PlayerAccountEntity? Account { get; init; }
    public PlayerSessionEntity? Session { get; init; }

    public static OnlineTokenValidationResult Ok(PlayerAccountEntity account, PlayerSessionEntity session) => new()
    {
        Success = true,
        Account = account,
        Session = session
    };

    public static OnlineTokenValidationResult Fail(string errorCode) => new()
    {
        Success = false,
        ErrorCode = errorCode
    };
}
