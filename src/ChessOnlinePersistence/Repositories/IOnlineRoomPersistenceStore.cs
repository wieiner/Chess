using ChessOnlinePersistence.Entities;

namespace ChessOnlinePersistence.Repositories;

public interface IOnlineRoomPersistenceStore
{
    Task UpsertRoomAsync(PersistentRoomEntity room, CancellationToken cancellationToken = default);
    Task UpsertTableAsync(PersistentTableEntity table, CancellationToken cancellationToken = default);
    Task UpsertSeatAsync(PersistentSeatEntity seat, CancellationToken cancellationToken = default);
    Task ClearActionLogAsync(string tableId, CancellationToken cancellationToken = default);
    Task AppendActionAsync(PersistentActionLogEntity action, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersistentRoomEntity>> GetRoomsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersistentTableEntity>> GetTablesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersistentSeatEntity>> GetSeatsAsync(string tableId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersistentActionLogEntity>> GetActionLogAsync(string tableId, long fromServerSeq = 1, int maxCount = 256, CancellationToken cancellationToken = default);
}
