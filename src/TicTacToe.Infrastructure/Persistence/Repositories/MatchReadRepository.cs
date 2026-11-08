using Microsoft.EntityFrameworkCore;
using TicTacToe.Application.Abstractions.Persistence;
using TicTacToe.Application.Common.Mapping;
using TicTacToe.Application.Features.Matches.Dtos;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Infrastructure.Persistence.Repositories;

public class MatchReadRepository : IMatchReadRepository
{
    private readonly GameDbContext _dbContext;

    public MatchReadRepository(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WaitingMatchDto>> GetWaitingAsync(CancellationToken ct)
    {
        return await _dbContext.Matches
            .AsNoTracking()
            .Where(m => m.Status == MatchStatus.Waiting)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Select(m => new WaitingMatchDto(
                m.Id,
                m.Players.Select(p => p.DisplayName).FirstOrDefault() ?? string.Empty,
                m.Players.Select(p => p.Symbol).FirstOrDefault(),
                m.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<MatchStateDto?> GetAuthorizedStateAsync(Guid matchId, string playerTokenHash, CancellationToken ct)
    {
        var match = await _dbContext.Matches
            .AsNoTracking()
            .Include(m => m.Players)
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);

        if (match == null)
            return null;

        var isAuthorized = match.Players.Any(p => p.TokenHash == playerTokenHash);
        if (!isAuthorized)
            return null;

        return MatchStateMapper.ToDto(match);
    }
}
