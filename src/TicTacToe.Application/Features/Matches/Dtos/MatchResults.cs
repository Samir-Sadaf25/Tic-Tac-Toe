using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.Features.Matches.Dtos;

public sealed record CreateMatchResult(
    Guid MatchId,
    string AccessToken,
    PlayerSymbol AssignedSymbol,
    MatchStateDto MatchState);

public sealed record JoinMatchResult(
    Guid MatchId,
    string AccessToken,
    PlayerSymbol AssignedSymbol,
    MatchStateDto MatchState);
