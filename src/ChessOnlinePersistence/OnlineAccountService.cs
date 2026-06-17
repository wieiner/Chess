using ChessOnlinePersistence.Entities;
using ChessOnlinePersistence.Repositories;
using Microsoft.AspNetCore.Identity;

namespace ChessOnlinePersistence;

public sealed class OnlineAccountService
{
    private readonly IOnlineIdentityStore _identityStore;
    private readonly IOnlineSessionStore _sessionStore;
    private readonly PasswordHasher<PlayerAccountEntity> _passwordHasher = new();
    private readonly OnlineSecurityOptions _security;

    public OnlineAccountService(IOnlineIdentityStore identityStore, IOnlineSessionStore sessionStore, OnlineSecurityOptions security)
    {
        _identityStore = identityStore;
        _sessionStore = sessionStore;
        _security = security;
        _security.Normalize();
    }

    public async Task<OnlineAccountResult> RegisterAsync(string userName, string displayName, string password, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUserName(userName);
        var validation = ValidateUserAndPassword(normalized, displayName, password);
        if (!validation.Success)
        {
            return validation;
        }
        if (await _identityStore.FindByUserNameAsync(normalized, cancellationToken) != null)
        {
            return OnlineAccountResult.Fail("duplicateUser", "Username is not available.");
        }

        var now = DateTime.UtcNow;
        var account = new PlayerAccountEntity
        {
            PlayerId = $"player-{Guid.NewGuid():N}",
            UserName = userName.Trim(),
            NormalizedUserName = normalized,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? userName.Trim() : displayName.Trim(),
            CreatedAtUtc = now
        };
        account.PasswordHash = _passwordHasher.HashPassword(account, password);
        await _identityStore.CreatePlayerAsync(account, cancellationToken);
        return OnlineAccountResult.Ok(account);
    }

    public async Task<OnlineAccountResult> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUserName(userName);
        var account = await _identityStore.FindByUserNameAsync(normalized, cancellationToken);
        if (account == null)
        {
            return OnlineAccountResult.Fail("invalidCredentials", "Invalid username or password.");
        }
        if (account.Disabled)
        {
            return OnlineAccountResult.Fail("disabled", "Invalid username or password.");
        }
        if (account.LockoutUntilUtc is { } lockout && lockout > DateTime.UtcNow)
        {
            return OnlineAccountResult.Fail("lockedOut", "Invalid username or password.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            account.FailedLoginCount++;
            if (account.FailedLoginCount >= _security.MaxLoginAttempts)
            {
                account.LockoutUntilUtc = DateTime.UtcNow.AddMinutes(_security.LockoutMinutes);
            }
            await _identityStore.UpdatePlayerAsync(account, cancellationToken);
            return OnlineAccountResult.Fail("invalidCredentials", "Invalid username or password.");
        }

        account.FailedLoginCount = 0;
        account.LockoutUntilUtc = null;
        account.LastLoginAtUtc = DateTime.UtcNow;
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = _passwordHasher.HashPassword(account, password);
        }
        await _identityStore.UpdatePlayerAsync(account, cancellationToken);
        return OnlineAccountResult.Ok(account);
    }

    public async Task<PlayerSessionEntity> CreateSessionAsync(PlayerAccountEntity account, string refreshTokenHash, DateTime expiresAtUtc, string clientName = "", CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var session = new PlayerSessionEntity
        {
            SessionId = $"session-{Guid.NewGuid():N}",
            PlayerId = account.PlayerId,
            RefreshTokenHash = refreshTokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc,
            LastSeenAtUtc = now,
            ClientName = clientName
        };
        await _sessionStore.CreateSessionAsync(session, cancellationToken);
        return session;
    }

    public Task<PlayerAccountEntity?> FindPlayerAsync(string playerId, CancellationToken cancellationToken = default) =>
        _identityStore.FindByPlayerIdAsync(playerId, cancellationToken);

    public Task<PlayerSessionEntity?> FindSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
        _sessionStore.FindSessionAsync(sessionId, cancellationToken);

    public Task UpdateSessionAsync(PlayerSessionEntity session, CancellationToken cancellationToken = default) =>
        _sessionStore.UpdateSessionAsync(session, cancellationToken);

    public Task RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
        _sessionStore.RevokeSessionAsync(sessionId, DateTime.UtcNow, cancellationToken);

    public static string NormalizeUserName(string userName) => (userName ?? "").Trim().ToUpperInvariant();

    private OnlineAccountResult ValidateUserAndPassword(string normalizedUserName, string displayName, string password)
    {
        if (normalizedUserName.Length is < 3 or > 64)
        {
            return OnlineAccountResult.Fail("invalidUserName", "Username must be between 3 and 64 characters.");
        }
        if (!string.IsNullOrWhiteSpace(displayName) && displayName.Trim().Length > 64)
        {
            return OnlineAccountResult.Fail("invalidDisplayName", "Display name is too long.");
        }
        if (string.IsNullOrEmpty(password) || password.Length < _security.PasswordMinLength || password.Length > _security.PasswordMaxLength)
        {
            return OnlineAccountResult.Fail("weakPassword", $"Password must be between {_security.PasswordMinLength} and {_security.PasswordMaxLength} characters.");
        }
        return OnlineAccountResult.Ok();
    }
}

public sealed class OnlineSecurityOptions
{
    public int PasswordMinLength { get; set; } = 8;
    public int PasswordMaxLength { get; set; } = 256;
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 5;

    public void Normalize()
    {
        PasswordMinLength = Math.Clamp(PasswordMinLength, 6, 128);
        PasswordMaxLength = Math.Clamp(PasswordMaxLength, PasswordMinLength, 4096);
        MaxLoginAttempts = Math.Clamp(MaxLoginAttempts, 1, 100);
        LockoutMinutes = Math.Clamp(LockoutMinutes, 1, 1440);
    }
}

public sealed class OnlineAccountResult
{
    public bool Success { get; init; }
    public string ErrorCode { get; init; } = "";
    public string ErrorText { get; init; } = "";
    public PlayerAccountEntity? Account { get; init; }

    public static OnlineAccountResult Ok(PlayerAccountEntity? account = null) => new() { Success = true, Account = account };
    public static OnlineAccountResult Fail(string code, string text) => new() { Success = false, ErrorCode = code, ErrorText = text };
}
