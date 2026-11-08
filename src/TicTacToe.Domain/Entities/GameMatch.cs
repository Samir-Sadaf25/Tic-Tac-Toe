using TicTacToe.Domain.Enums;
using TicTacToe.Domain.Exceptions;
using TicTacToe.Domain.Rules;

namespace TicTacToe.Domain.Entities;

public class GameMatch
{
    private readonly List<MatchPlayer> _players = new();

    public Guid Id { get; private set; }
    public MatchStatus Status { get; private set; }
    public string BoardState { get; private set; } = ".........";
    public PlayerSymbol CurrentTurn { get; private set; }
    public MatchOutcome Outcome { get; private set; }
    public int Revision { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public IReadOnlyCollection<MatchPlayer> Players => _players.AsReadOnly();

    // Private constructor for EF Core
    private GameMatch() { }

    public static GameMatch Create(
        Guid matchId,
        Guid creatorPlayerId,
        string displayName,
        PlayerSymbol creatorSymbol,
        string tokenHash)
    {
        if (matchId == Guid.Empty)
            throw new GameRuleException(GameErrorCode.InvalidMatchId, "Match ID cannot be empty.");

        if (creatorSymbol is not (PlayerSymbol.X or PlayerSymbol.O))
            throw new GameRuleException(GameErrorCode.InvalidSymbol, "Creator symbol must be X or O.");

        var match = new GameMatch
        {
            Id = matchId,
            Status = MatchStatus.Waiting,
            BoardState = ".........",
            CurrentTurn = PlayerSymbol.X, // X always starts
            Outcome = MatchOutcome.None,
            Revision = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = null
        };

        var creator = new MatchPlayer(creatorPlayerId, matchId, displayName, creatorSymbol, tokenHash);
        match._players.Add(creator);

        return match;
    }

    public MatchPlayer Join(Guid joinerPlayerId, string displayName, string tokenHash)
    {
        if (Status != MatchStatus.Waiting || _players.Count != 1)
        {
            if (_players.Count >= 2 || Status != MatchStatus.Waiting)
                throw new GameRuleException(GameErrorCode.MatchAlreadyFull, "Match is not waiting for players or is already full.");
            throw new GameRuleException(GameErrorCode.MatchNotWaiting, "Match is not in waiting state.");
        }

        var creator = _players[0];
        if (creator.TokenHash == tokenHash)
            throw new GameRuleException(GameErrorCode.CannotJoinOwnMatch, "Creator cannot join their own match.");

        var joinerSymbol = creator.Symbol == PlayerSymbol.X ? PlayerSymbol.O : PlayerSymbol.X;
        var joiner = new MatchPlayer(joinerPlayerId, Id, displayName, joinerSymbol, tokenHash);

        _players.Add(joiner);
        Status = MatchStatus.InProgress;
        CurrentTurn = PlayerSymbol.X; // X always plays first
        Revision++;

        return joiner;
    }

    public void MakeMove(Guid playerId, int cellIndex)
    {
        if (Status != MatchStatus.InProgress)
        {
            if (Status == MatchStatus.Completed)
                throw new GameRuleException(GameErrorCode.MatchCompleted, "The match has already ended.");
            throw new GameRuleException(GameErrorCode.MatchNotInProgress, "Match is not in progress.");
        }

        var player = _players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            throw new GameRuleException(GameErrorCode.PlayerNotFound, "Player is not part of this match.");

        if (player.Symbol != CurrentTurn)
            throw new GameRuleException(GameErrorCode.NotPlayerTurn, "It is not your turn.");

        if (cellIndex < 0 || cellIndex > 8)
            throw new GameRuleException(GameErrorCode.InvalidCellIndex, "Cell index must be between 0 and 8.");

        if (BoardState[cellIndex] != '.')
            throw new GameRuleException(GameErrorCode.CellAlreadyOccupied, "Cell is already occupied.");

        char symbolChar = player.Symbol == PlayerSymbol.X ? 'X' : 'O';
        var chars = BoardState.ToCharArray();
        chars[cellIndex] = symbolChar;
        BoardState = new string(chars);

        Revision++;

        // 1. Check win condition
        if (WinningLines.IsWinningBoard(BoardState, symbolChar, out _))
        {
            Status = MatchStatus.Completed;
            Outcome = player.Symbol == PlayerSymbol.X ? MatchOutcome.XWins : MatchOutcome.OWins;
            CompletedAtUtc = DateTimeOffset.UtcNow;
            return;
        }

        // 2. Check draw condition
        if (!BoardState.Contains('.'))
        {
            Status = MatchStatus.Completed;
            Outcome = MatchOutcome.Draw;
            CompletedAtUtc = DateTimeOffset.UtcNow;
            return;
        }

        // 3. Switch turn
        CurrentTurn = CurrentTurn == PlayerSymbol.X ? PlayerSymbol.O : PlayerSymbol.X;
    }

    public MatchPlayer? GetPlayerByTokenHash(string tokenHash)
    {
        return _players.FirstOrDefault(p => p.TokenHash == tokenHash);
    }
}
