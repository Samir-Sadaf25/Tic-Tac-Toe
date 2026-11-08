using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.Features.Matches.Dtos;

public sealed record WaitingMatchDto(
    Guid Id,
    string CreatorDisplayName,
    PlayerSymbol CreatorSymbol,
    DateTimeOffset CreatedAtUtc);
