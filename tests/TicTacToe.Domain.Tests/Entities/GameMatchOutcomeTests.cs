using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Enums;
using TicTacToe.Domain.Exceptions;

namespace TicTacToe.Domain.Tests.Entities;

[TestFixture]
public class GameMatchOutcomeTests
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
    public void MakeMove_HorizontalWinRow0_SetsCompletedAndXWins()
    {
        var match = CreateActiveMatch(out var x, out var o);

        // X: 0, O: 3, X: 1, O: 4, X: 2
        match.MakeMove(x.Id, 0);
        match.MakeMove(o.Id, 3);
        match.MakeMove(x.Id, 1);
        match.MakeMove(o.Id, 4);
        match.MakeMove(x.Id, 2); // X completes row 0: (0, 1, 2)

        match.Status.ShouldBe(MatchStatus.Completed);
        match.Outcome.ShouldBe(MatchOutcome.XWins);
        match.CompletedAtUtc.ShouldNotBeNull();
        match.BoardState.ShouldBe("XXXOO....");
    }

    [Test]
    public void MakeMove_VerticalWinCol1_SetsCompletedAndOWins()
    {
        var match = CreateActiveMatch(out var x, out var o);

        // X: 0, O: 1, X: 2, O: 4, X: 5, O: 7
        match.MakeMove(x.Id, 0);
        match.MakeMove(o.Id, 1);
        match.MakeMove(x.Id, 2);
        match.MakeMove(o.Id, 4);
        match.MakeMove(x.Id, 5);
        match.MakeMove(o.Id, 7); // O completes col 1: (1, 4, 7)

        match.Status.ShouldBe(MatchStatus.Completed);
        match.Outcome.ShouldBe(MatchOutcome.OWins);
        match.CompletedAtUtc.ShouldNotBeNull();
    }

    [Test]
    public void MakeMove_DiagonalWin_SetsCompletedAndXWins()
    {
        var match = CreateActiveMatch(out var x, out var o);

        // X: 0, O: 1, X: 4, O: 2, X: 8
        match.MakeMove(x.Id, 0);
        match.MakeMove(o.Id, 1);
        match.MakeMove(x.Id, 4);
        match.MakeMove(o.Id, 2);
        match.MakeMove(x.Id, 8); // X completes main diagonal: (0, 4, 8)

        match.Status.ShouldBe(MatchStatus.Completed);
        match.Outcome.ShouldBe(MatchOutcome.XWins);
        match.CompletedAtUtc.ShouldNotBeNull();
    }

    [Test]
    public void MakeMove_AntiDiagonalWin_SetsCompletedAndXWins()
    {
        var match = CreateActiveMatch(out var x, out var o);

        // X: 2, O: 0, X: 4, O: 1, X: 6
        match.MakeMove(x.Id, 2);
        match.MakeMove(o.Id, 0);
        match.MakeMove(x.Id, 4);
        match.MakeMove(o.Id, 1);
        match.MakeMove(x.Id, 6); // X completes anti-diagonal: (2, 4, 6)

        match.Status.ShouldBe(MatchStatus.Completed);
        match.Outcome.ShouldBe(MatchOutcome.XWins);
        match.CompletedAtUtc.ShouldNotBeNull();
    }

    [Test]
    public void MakeMove_FullBoardDraw_SetsCompletedAndDraw()
    {
        var match = CreateActiveMatch(out var x, out var o);

        // Playing a classic draw board:
        // X O X
        // X X O
        // O X O
        // Sequence:
        // X: 0, O: 1, X: 2, O: 5, X: 3, O: 6, X: 4, O: 8, X: 7
        match.MakeMove(x.Id, 0); // X (0)
        match.MakeMove(o.Id, 1); // O (1)
        match.MakeMove(x.Id, 2); // X (2)
        match.MakeMove(o.Id, 5); // O (5)
        match.MakeMove(x.Id, 3); // X (3)
        match.MakeMove(o.Id, 6); // O (6)
        match.MakeMove(x.Id, 4); // X (4)
        match.MakeMove(o.Id, 8); // O (8)
        match.MakeMove(x.Id, 7); // X (7)

        match.BoardState.ShouldBe("XOX..".Length == 5 ? "XOX" + "XXO" + "OXO" : "XOX" + "XXO" + "OXO");
        match.Status.ShouldBe(MatchStatus.Completed);
        match.Outcome.ShouldBe(MatchOutcome.Draw);
        match.CompletedAtUtc.ShouldNotBeNull();
    }

    [Test]
    public void MakeMove_WhenMatchAlreadyCompleted_ThrowsMatchCompleted()
    {
        var match = CreateActiveMatch(out var x, out var o);

        // X wins row 0
        match.MakeMove(x.Id, 0);
        match.MakeMove(o.Id, 3);
        match.MakeMove(x.Id, 1);
        match.MakeMove(o.Id, 4);
        match.MakeMove(x.Id, 2);

        match.Status.ShouldBe(MatchStatus.Completed);

        // O attempts move after completion
        var ex = Should.Throw<GameRuleException>(() => match.MakeMove(o.Id, 5));

        ex.ErrorCode.ShouldBe(GameErrorCode.MatchCompleted);
    }
}
