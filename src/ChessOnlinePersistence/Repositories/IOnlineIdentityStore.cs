using ChessOnlinePersistence.Entities;

namespace ChessOnlinePersistence.Repositories;

public interface IOnlineIdentityStore
{
    Task<PlayerAccountEntity?> FindByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default);
    Task<PlayerAccountEntity?> FindByPlayerIdAsync(string playerId, CancellationToken cancellationToken = default);
    Task CreatePlayerAsync(PlayerAccountEntity account, CancellationToken cancellationToken = default);
    Task UpdatePlayerAsync(PlayerAccountEntity account, CancellationToken cancellationToken = default);
}

