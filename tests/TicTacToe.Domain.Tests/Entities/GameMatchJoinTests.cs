using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Enums;
using TicTacToe.Domain.Exceptions;

namespace TicTacToe.Domain.Tests.Entities;

[TestFixture]
public class GameMatchJoinTests
{
    [Test]
    public void Join_WhenCreatorIsX_JoinerReceivesOAndMatchStarts()
    {
        var match = GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, "creator-hash");
        var joinerId = Guid.NewGuid();

        var joiner = match.Join(joinerId, "Bob", "joiner-hash");

        match.Status.ShouldBe(MatchStatus.InProgress);
        match.CurrentTurn.ShouldBe(PlayerSymbol.X);
        match.Revision.ShouldBe(1);
        match.Players.Count.ShouldBe(2);

        joiner.Id.ShouldBe(joinerId);
        joiner.DisplayName.ShouldBe("Bob");
        joiner.Symbol.ShouldBe(PlayerSymbol.O);
        joiner.TokenHash.ShouldBe("joiner-hash");
    }

    [Test]
    public void Join_WhenCreatorIsO_JoinerReceivesXAndXMovesFirst()
    {
        var match = GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.O, "creator-hash");
        var joinerId = Guid.NewGuid();

        var joiner = match.Join(joinerId, "Bob", "joiner-hash");

        match.Status.ShouldBe(MatchStatus.InProgress);
        match.CurrentTurn.ShouldBe(PlayerSymbol.X); // X moves first!
        match.Revision.ShouldBe(1);

        joiner.Symbol.ShouldBe(PlayerSymbol.X);
    }

    [Test]
    public void Join_WithSameDisplayName_IsAllowed()
    {
        var match = GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), "Alex", PlayerSymbol.X, "creator-hash");
        
        var joiner = match.Join(Guid.NewGuid(), "Alex", "joiner-hash");

        joiner.DisplayName.ShouldBe("Alex");
        match.Players.Count.ShouldBe(2);
    }

    [Test]
    public void Join_WhenUsingCreatorToken_ThrowsCannotJoinOwnMatch()
    {
        var match = GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, "creator-hash");

        var ex = Should.Throw<GameRuleException>(() =>
            match.Join(Guid.NewGuid(), "AliceAgain", "creator-hash"));

        ex.ErrorCode.ShouldBe(GameErrorCode.CannotJoinOwnMatch);
        match.Status.ShouldBe(MatchStatus.Waiting);
        match.Revision.ShouldBe(0);
        match.Players.Count.ShouldBe(1);
    }

    [Test]
    public void Join_WhenMatchAlreadyHasTwoPlayers_ThrowsMatchAlreadyFull()
    {
        var match = GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, "creator-hash");
        match.Join(Guid.NewGuid(), "Bob", "joiner-hash");

        var ex = Should.Throw<GameRuleException>(() =>
            match.Join(Guid.NewGuid(), "Charlie", "charlie-hash"));

        ex.ErrorCode.ShouldBe(GameErrorCode.MatchAlreadyFull);
        match.Revision.ShouldBe(1);
        match.Players.Count.ShouldBe(2);
    }

    [Test]
    public void GetPlayerByTokenHash_ReturnsMatchingPlayerOrNull()
    {
        var match = GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, "alice-hash");
        match.Join(Guid.NewGuid(), "Bob", "bob-hash");

        match.GetPlayerByTokenHash("alice-hash")?.DisplayName.ShouldBe("Alice");
        match.GetPlayerByTokenHash("bob-hash")?.DisplayName.ShouldBe("Bob");
        match.GetPlayerByTokenHash("unknown-hash").ShouldBeNull();
    }
}
