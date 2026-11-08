using NUnit.Framework;
using Shouldly;
using TicTacToe.Application.Features.Matches.Dtos;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Enums;
using TicTacToe.Infrastructure.Persistence.Repositories;

namespace TicTacToe.IntegrationTests.Persistence;

[TestFixture]
public class MatchReadRepositoryTests : SqlServerTestBase
{
    private static readonly DateTimeOffset BaseTime =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task GetWaitingAsync_ReturnsOnlyWaitingMatches_NewestFirst()
    {
        var older = GameMatch.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, "hash-alice");
        SetCreatedAtUtc(older, BaseTime);

        var newer = GameMatch.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Bob", PlayerSymbol.O, "hash-bob");
        SetCreatedAtUtc(newer, BaseTime.AddHours(1));

        var inProgress = GameMatch.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Carol", PlayerSymbol.O, "hash-carol");
        inProgress.Join(Guid.NewGuid(), "Dave", "hash-dave");
        SetCreatedAtUtc(inProgress, BaseTime.AddHours(2));

        var completed = GameMatch.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Erin", PlayerSymbol.O, "hash-erin");
        var frank = completed.Join(Guid.NewGuid(), "Frank", "hash-frank");
        // X (Frank) plays first and wins the top row.
        completed.MakeMove(frank.Id, 0);
        completed.MakeMove(completed.Players.Single(p => p.Symbol == PlayerSymbol.O).Id, 3);
        completed.MakeMove(frank.Id, 1);
        completed.MakeMove(completed.Players.Single(p => p.Symbol == PlayerSymbol.O).Id, 4);
        completed.MakeMove(frank.Id, 2);
        SetCreatedAtUtc(completed, BaseTime.AddHours(3));

        await PersistAsync(older, newer, inProgress, completed);

        await using var context = Database.CreateContext();
        var result = await new MatchReadRepository(context)
            .GetWaitingAsync(CancellationToken.None);

        result.Count.ShouldBe(2);
        result.Select(m => m.Id).ShouldBe(new[] { newer.Id, older.Id });

        result[0].CreatorDisplayName.ShouldBe("Bob");
        result[0].CreatorSymbol.ShouldBe(PlayerSymbol.O);
        result[0].CreatedAtUtc.ShouldBe(BaseTime.AddHours(1));

        result[1].CreatorDisplayName.ShouldBe("Alice");
        result[1].CreatorSymbol.ShouldBe(PlayerSymbol.X);
        result[1].CreatedAtUtc.ShouldBe(BaseTime);
    }

    [Test]
    public async Task GetAuthorizedStateAsync_WithPlayerToken_ReturnsFullState()
    {
        var match = GameMatch.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, "hash-alice");
        var joiner = match.Join(Guid.NewGuid(), "Bob", "hash-bob");
        await PersistAsync(match);

        await using var context = Database.CreateContext();
        var state = await new MatchReadRepository(context)
            .GetAuthorizedStateAsync(match.Id, "hash-alice", CancellationToken.None);

        state.ShouldNotBeNull();
        state.Id.ShouldBe(match.Id);
        state.Status.ShouldBe(MatchStatus.InProgress);
        state.BoardState.ShouldBe(".........");
        state.CurrentTurn.ShouldBe(PlayerSymbol.X);
        state.Outcome.ShouldBe(MatchOutcome.None);
        state.Revision.ShouldBe(match.Revision);
        state.CreatedAtUtc.ShouldBe(match.CreatedAtUtc);
        state.CompletedAtUtc.ShouldBeNull();

        state.Players.Count.ShouldBe(2);
        state.Players.Single(p => p.DisplayName == "Alice").Symbol.ShouldBe(PlayerSymbol.X);
        state.Players.Single(p => p.Id == joiner.Id).Symbol.ShouldBe(PlayerSymbol.O);

        // The read projection must never carry the token hash off the persistence layer.
        typeof(MatchStateDto).GetProperties()
            .Any(p => p.Name.Contains("Token", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse();
    }

    [Test]
    public async Task GetAuthorizedStateAsync_WithJoinerToken_IsAuthorized()
    {
        var match = GameMatch.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, "hash-alice");
        match.Join(Guid.NewGuid(), "Bob", "hash-bob");
        await PersistAsync(match);

        await using var context = Database.CreateContext();
        var state = await new MatchReadRepository(context)
            .GetAuthorizedStateAsync(match.Id, "hash-bob", CancellationToken.None);

        state.ShouldNotBeNull();
        state.Id.ShouldBe(match.Id);
    }

    [Test]
    public async Task GetAuthorizedStateAsync_WithUnknownToken_ReturnsNull()
    {
        var match = GameMatch.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, "hash-alice");
        await PersistAsync(match);

        await using var context = Database.CreateContext();
        var state = await new MatchReadRepository(context)
            .GetAuthorizedStateAsync(match.Id, "hash-not-a-player", CancellationToken.None);

        state.ShouldBeNull();
    }

    [Test]
    public async Task GetAuthorizedStateAsync_WithTokenFromAnotherMatch_ReturnsNull()
    {
        var first = GameMatch.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice", PlayerSymbol.X, "hash-alice");
        var second = GameMatch.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Bob", PlayerSymbol.X, "hash-bob");
        await PersistAsync(first, second);

        await using var context = Database.CreateContext();
        var state = await new MatchReadRepository(context)
            .GetAuthorizedStateAsync(first.Id, "hash-bob", CancellationToken.None);

        state.ShouldBeNull();
    }

    [Test]
    public async Task GetAuthorizedStateAsync_WithUnknownMatch_ReturnsNull()
    {
        await using var context = Database.CreateContext();
        var state = await new MatchReadRepository(context)
            .GetAuthorizedStateAsync(Guid.NewGuid(), "hash-alice", CancellationToken.None);

        state.ShouldBeNull();
    }

    private static void SetCreatedAtUtc(GameMatch match, DateTimeOffset createdAtUtc)
    {
        // CreatedAtUtc is set from the clock by GameMatch.Create; tests pin it so
        // ordering is deterministic instead of depending on clock resolution.
        typeof(GameMatch)
            .GetProperty(nameof(GameMatch.CreatedAtUtc))!
            .SetValue(match, createdAtUtc);
    }
}
