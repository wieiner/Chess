namespace ChessOnlinePersistence.Entities;

public sealed class PersistentTableEntity
{
    public string TableId { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string RulesetId { get; set; } = "";
    public string ProfileKind { get; set; } = "";
    public string State { get; set; } = "";
    public long ServerSeq { get; set; }
    public string StateHash { get; set; } = "";
    public string SaveGameJson { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
}

