using TicTacToe.Application.Features.Matches.Dtos;

namespace TicTacToe.Application.Abstractions.Persistence;

public interface IMatchReadRepository
{
    Task<IReadOnlyList<WaitingMatchDto>> GetWaitingAsync(CancellationToken ct);
    Task<MatchStateDto?> GetAuthorizedStateAsync(Guid matchId, string playerTokenHash, CancellationToken ct);
}
