using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.Features.Matches.Dtos;

public sealed record PlayerDto(
    Guid Id,
    string DisplayName,
    PlayerSymbol Symbol);

public sealed record MatchStateDto(
    Guid Id,
    MatchStatus Status,
    string BoardState,
    PlayerSymbol CurrentTurn,
    MatchOutcome Outcome,
    int Revision,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<PlayerDto> Players);
