namespace ChessOnlinePersistence.Entities;

public sealed class PlayerAccountEntity
{
    public string PlayerId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string NormalizedUserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public bool Disabled { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutUntilUtc { get; set; }
    public int TokenVersion { get; set; } = 1;
}

