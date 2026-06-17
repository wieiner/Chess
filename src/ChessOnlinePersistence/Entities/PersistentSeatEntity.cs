namespace ChessOnlinePersistence.Entities;

public sealed class PersistentSeatEntity
{
    public string TableId { get; set; } = "";
    public int SeatIndex { get; set; }
    public int SideId { get; set; }
    public int MacroPlayer { get; set; }
    public string PlayerId { get; set; } = "";
    public bool IsReady { get; set; }
    public bool IsConnected { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
}

