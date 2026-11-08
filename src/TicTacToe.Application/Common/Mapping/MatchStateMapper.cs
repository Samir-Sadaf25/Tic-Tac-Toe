using TicTacToe.Application.Features.Matches.Dtos;
using TicTacToe.Domain.Entities;

namespace TicTacToe.Application.Common.Mapping;

public static class MatchStateMapper
{
    public static MatchStateDto ToDto(GameMatch match)
    {
        var players = match.Players
            .Select(p => new PlayerDto(p.Id, p.DisplayName, p.Symbol))
            .ToList();

        return new MatchStateDto(
            match.Id,
            match.Status,
            match.BoardState,
            match.CurrentTurn,
            match.Outcome,
            match.Revision,
            match.CreatedAtUtc,
            match.CompletedAtUtc,
            players);
    }
}
