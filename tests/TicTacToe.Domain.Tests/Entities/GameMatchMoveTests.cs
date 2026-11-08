using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Enums;
using TicTacToe.Domain.Exceptions;

namespace TicTacToe.Domain.Tests.Entities;

[TestFixture]
public class GameMatchMoveTests
{
    private GameMatch CreateActiveMatch(out MatchPlayer playerX, out MatchPlayer playerO)
    {
        var match = GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, "alice-hash");
        match.Join(Guid.NewGuid(), "Bob", "bob-hash");
        playerX = match.Players.First(p => p.Symbol == PlayerSymbol.X);
        playerO = match.Players.First(p => p.Symbol == PlayerSymbol.O);
        return match;
    }

    [Test]
    public void MakeMove_ValidMove_UpdatesBoardSwitchesTurnAndIncrementsRevision()
    {
        var match = CreateActiveMatch(out var playerX, out var playerO);
        var initialRevision = match.Revision;

        match.MakeMove(playerX.Id, 4); // Center square

        match.BoardState.ShouldBe("....X....");
        match.CurrentTurn.ShouldBe(PlayerSymbol.O);
        match.Revision.ShouldBe(initialRevision + 1);

        // Next move by O
        match.MakeMove(playerO.Id, 0);

        match.BoardState.ShouldBe("O...X....");
        match.CurrentTurn.ShouldBe(PlayerSymbol.X);
        match.Revision.ShouldBe(initialRevision + 2);
    }

    [Test]
    public void MakeMove_WhenNotPlayerTurn_ThrowsNotPlayerTurn()
    {
        var match = CreateActiveMatch(out _, out var playerO);
        var initialBoard = match.BoardState;
        var initialRevision = match.Revision;

        // O tries to move when it's X's turn
        var ex = Should.Throw<GameRuleException>(() => match.MakeMove(playerO.Id, 0));

        ex.ErrorCode.ShouldBe(GameErrorCode.NotPlayerTurn);
        match.BoardState.ShouldBe(initialBoard);
        match.Revision.ShouldBe(initialRevision);
        match.CurrentTurn.ShouldBe(PlayerSymbol.X);
    }

    [Test]
    public void MakeMove_OnAlreadyOccupiedCell_ThrowsCellAlreadyOccupied()
    {
        var match = CreateActiveMatch(out var playerX, out var playerO);
        match.MakeMove(playerX.Id, 4);

        var ex = Should.Throw<GameRuleException>(() => match.MakeMove(playerO.Id, 4));

        ex.ErrorCode.ShouldBe(GameErrorCode.CellAlreadyOccupied);
        match.BoardState.ShouldBe("....X....");
        match.CurrentTurn.ShouldBe(PlayerSymbol.O);
    }

    [TestCase(-1)]
    [TestCase(9)]
    [TestCase(100)]
    public void MakeMove_WithOutOfRangeCell_ThrowsInvalidCellIndex(int invalidCell)
    {
        var match = CreateActiveMatch(out var playerX, out _);

        var ex = Should.Throw<GameRuleException>(() => match.MakeMove(playerX.Id, invalidCell));

        ex.ErrorCode.ShouldBe(GameErrorCode.InvalidCellIndex);
        match.BoardState.ShouldBe(".........");
    }

    [Test]
    public void MakeMove_WhenMatchIsWaiting_ThrowsMatchNotInProgress()
    {
        var match = GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, "alice-hash");
        var creator = match.Players.First();

        var ex = Should.Throw<GameRuleException>(() => match.MakeMove(creator.Id, 0));

        ex.ErrorCode.ShouldBe(GameErrorCode.MatchNotInProgress);
        match.BoardState.ShouldBe(".........");
    }

    [Test]
    public void MakeMove_ByUnknownPlayer_ThrowsPlayerNotFound()
    {
        var match = CreateActiveMatch(out _, out _);

        var ex = Should.Throw<GameRuleException>(() => match.MakeMove(Guid.NewGuid(), 0));

        ex.ErrorCode.ShouldBe(GameErrorCode.PlayerNotFound);
        match.BoardState.ShouldBe(".........");
    }
}
