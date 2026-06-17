namespace ChessOnlinePersistence.Entities;

public sealed class PlayerSessionEntity
{
    public string SessionId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string RefreshTokenHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    public string ClientName { get; set; } = "";
    public string LastKnownRoomId { get; set; } = "";
    public string LastKnownTableId { get; set; } = "";
    public int LastKnownSeatIndex { get; set; }
}

