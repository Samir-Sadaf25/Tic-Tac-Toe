using TicTacToe.Domain.Enums;
using TicTacToe.Domain.Exceptions;

namespace TicTacToe.Domain.Entities;

public class MatchPlayer
{
    public Guid Id { get; private set; }
    public Guid MatchId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public PlayerSymbol Symbol { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;

    // Private constructor for EF Core
    private MatchPlayer() { }

    internal MatchPlayer(Guid id, Guid matchId, string displayName, PlayerSymbol symbol, string tokenHash)
    {
        if (id == Guid.Empty)
            throw new GameRuleException(GameErrorCode.InvalidPlayerId, "Player ID cannot be empty.");

        if (matchId == Guid.Empty)
            throw new GameRuleException(GameErrorCode.InvalidMatchId, "Match ID cannot be empty.");

        var trimmedName = displayName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName) || trimmedName.Length > 20)
            throw new GameRuleException(GameErrorCode.InvalidDisplayName, "Display name must be between 1 and 20 characters.");

        if (symbol is not (PlayerSymbol.X or PlayerSymbol.O))
            throw new GameRuleException(GameErrorCode.InvalidSymbol, "Player symbol must be X or O.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new GameRuleException(GameErrorCode.InvalidTokenHash, "Token hash cannot be empty.");

        Id = id;
        MatchId = matchId;
        DisplayName = trimmedName;
        Symbol = symbol;
        TokenHash = tokenHash;
    }
}
