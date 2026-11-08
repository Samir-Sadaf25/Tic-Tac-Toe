using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Enums;
using TicTacToe.Domain.Exceptions;

namespace TicTacToe.Domain.Tests.Entities;

[TestFixture]
public class GameMatchCreateTests
{
    [Test]
    public void Create_WithValidParameters_SetsInitialStateCorrectly()
    {
        var matchId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        var match = GameMatch.Create(matchId, creatorId, "Alice", PlayerSymbol.X, "token-hash-123");

        match.Id.ShouldBe(matchId);
        match.Status.ShouldBe(MatchStatus.Waiting);
        match.BoardState.ShouldBe(".........");
        match.CurrentTurn.ShouldBe(PlayerSymbol.X);
        match.Outcome.ShouldBe(MatchOutcome.None);
        match.Revision.ShouldBe(0);
        match.CreatedAtUtc.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
        match.CompletedAtUtc.ShouldBeNull();

        match.Players.Count.ShouldBe(1);
        var creator = match.Players.First();
        creator.Id.ShouldBe(creatorId);
        creator.MatchId.ShouldBe(matchId);
        creator.DisplayName.ShouldBe("Alice");
        creator.Symbol.ShouldBe(PlayerSymbol.X);
        creator.TokenHash.ShouldBe("token-hash-123");
    }

    [Test]
    public void Create_WhenCreatorChoosesO_TurnStillStartsAtX()
    {
        var match = GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), "Bob", PlayerSymbol.O, "hash-bob");

        match.CurrentTurn.ShouldBe(PlayerSymbol.X);
        match.Players.First().Symbol.ShouldBe(PlayerSymbol.O);
    }

    [TestCase("A")]
    [TestCase("12345678901234567890")] // 20 chars
    [TestCase("  TrimmedName  ")]
    public void Create_ValidDisplayNames_AreAcceptedAndTrimmed(string name)
    {
        var match = GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), name, PlayerSymbol.X, "hash");
        match.Players.First().DisplayName.ShouldBe(name.Trim());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("123456789012345678901")] // 21 chars
    public void Create_InvalidDisplayNames_ThrowsInvalidDisplayNameException(string? invalidName)
    {
        var ex = Should.Throw<GameRuleException>(() =>
            GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), invalidName!, PlayerSymbol.X, "hash"));

        ex.ErrorCode.ShouldBe(GameErrorCode.InvalidDisplayName);
    }

    [Test]
    public void Create_InvalidSymbol_ThrowsInvalidSymbolException()
    {
        var ex = Should.Throw<GameRuleException>(() =>
            GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.None, "hash"));

        ex.ErrorCode.ShouldBe(GameErrorCode.InvalidSymbol);
    }

    [Test]
    public void Create_EmptyMatchId_ThrowsInvalidMatchIdException()
    {
        var ex = Should.Throw<GameRuleException>(() =>
            GameMatch.Create(Guid.Empty, Guid.NewGuid(), "Alice", PlayerSymbol.X, "hash"));

        ex.ErrorCode.ShouldBe(GameErrorCode.InvalidMatchId);
    }

    [Test]
    public void Create_EmptyPlayerId_ThrowsInvalidPlayerIdException()
    {
        var ex = Should.Throw<GameRuleException>(() =>
            GameMatch.Create(Guid.NewGuid(), Guid.Empty, "Alice", PlayerSymbol.X, "hash"));

        ex.ErrorCode.ShouldBe(GameErrorCode.InvalidPlayerId);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_EmptyTokenHash_ThrowsInvalidTokenHashException(string? invalidHash)
    {
        var ex = Should.Throw<GameRuleException>(() =>
            GameMatch.Create(Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, invalidHash!));

        ex.ErrorCode.ShouldBe(GameErrorCode.InvalidTokenHash);
    }
}
