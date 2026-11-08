using Microsoft.EntityFrameworkCore;
using TicTacToe.Application.Abstractions.Persistence;
using TicTacToe.Application.Common.Exceptions;

namespace TicTacToe.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly GameDbContext _dbContext;

    public EfUnitOfWork(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new MatchConflictException("The match state was modified concurrently by another request.");
        }
    }
}
