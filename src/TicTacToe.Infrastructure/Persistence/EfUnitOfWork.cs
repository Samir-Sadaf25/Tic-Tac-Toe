using Microsoft.EntityFrameworkCore;
using TicTacToe.Application.Abstractions.Persistence;
using TicTacToe.Application.Common.Exceptions;
using TicTacToe.Domain.Entities;

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
        PromoteNewMatchPlayers();

        try
        {
            return await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new MatchConflictException("The match state was modified concurrently by another request.");
        }
    }

    private void PromoteNewMatchPlayers()
    {
        // MatchPlayer is immutable after construction, so a tracked player can only
        // be Unchanged (loaded from SQL) or newly created by GameMatch.Join. EF Core
        // classifies children with client-assigned keys added to a tracked aggregate
        // as Modified, which would emit an UPDATE against a row that does not exist.
        // Promoting those entries to Added keeps the insert semantics correct.
        foreach (var entry in _dbContext.ChangeTracker.Entries<MatchPlayer>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.State = EntityState.Added;
            }
        }
    }
}
