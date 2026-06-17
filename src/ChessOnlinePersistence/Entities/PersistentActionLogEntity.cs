namespace ChessOnlinePersistence.Entities;

public sealed class PersistentActionLogEntity
{
    public string TableId { get; set; } = "";
    public long ServerSeq { get; set; }
    public int ActionIndex { get; set; }
    public string ActorPlayerId { get; set; } = "";
    public string ActionKind { get; set; } = "";
    public string ActionJson { get; set; } = "";
    public string Notation { get; set; } = "";
    public string StateHashBefore { get; set; } = "";
    public string StateHashAfter { get; set; } = "";
    public string PreviousEventHash { get; set; } = "";
    public string EventHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

