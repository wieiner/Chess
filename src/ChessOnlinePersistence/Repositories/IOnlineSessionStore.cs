using ChessOnlinePersistence.Entities;

namespace ChessOnlinePersistence.Repositories;

public interface IOnlineSessionStore
{
    Task<PlayerSessionEntity?> FindSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task CreateSessionAsync(PlayerSessionEntity session, CancellationToken cancellationToken = default);
    Task UpdateSessionAsync(PlayerSessionEntity session, CancellationToken cancellationToken = default);
    Task RevokeSessionAsync(string sessionId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
    Task UpdateLastSeenAsync(string sessionId, string roomId, string tableId, int seatIndex, CancellationToken cancellationToken = default);
}

