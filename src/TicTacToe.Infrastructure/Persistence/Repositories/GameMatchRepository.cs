using Microsoft.EntityFrameworkCore;
using TicTacToe.Application.Abstractions.Persistence;
using TicTacToe.Domain.Entities;

namespace TicTacToe.Infrastructure.Persistence.Repositories;

public class GameMatchRepository : IGameMatchRepository
{
    private readonly GameDbContext _dbContext;

    public GameMatchRepository(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GameMatch?> GetByIdAsync(Guid matchId, CancellationToken ct)
    {
        return await _dbContext.Matches
            .Include(m => m.Players)
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);
    }

    public async Task AddAsync(GameMatch match, CancellationToken ct)
    {
        await _dbContext.Matches.AddAsync(match, ct);
    }
}
