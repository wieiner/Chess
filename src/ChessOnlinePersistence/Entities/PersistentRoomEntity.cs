namespace ChessOnlinePersistence.Entities;

public sealed class PersistentRoomEntity
{
    public string RoomId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public string OwnerPlayerId { get; set; } = "";
    public string State { get; set; } = "";
    public long LastServerSeq { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
}

