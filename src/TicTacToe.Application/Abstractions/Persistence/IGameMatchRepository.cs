using TicTacToe.Domain.Entities;

namespace TicTacToe.Application.Abstractions.Persistence;

public interface IGameMatchRepository
{
    Task<GameMatch?> GetByIdAsync(Guid matchId, CancellationToken ct);
    Task AddAsync(GameMatch match, CancellationToken ct);
}
