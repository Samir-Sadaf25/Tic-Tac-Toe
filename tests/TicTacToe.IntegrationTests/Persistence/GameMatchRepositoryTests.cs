using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Enums;
using TicTacToe.Infrastructure.Persistence.Repositories;

namespace TicTacToe.IntegrationTests.Persistence;

[TestFixture]
public class GameMatchRepositoryTests : SqlServerTestBase
{
    private static readonly Guid CreatorId = Guid.NewGuid();

    [Test]
    public async Task AddAsync_ThenSave_PersistsMatchAndPlayers()
    {
        var match = GameMatch.Create(
            Guid.NewGuid(),
            CreatorId,
            "Alice",
            PlayerSymbol.X,
            "hash-alice");

        await PersistAsync(match);

        await using var context = Database.CreateContext();
        var repository = new GameMatchRepository(context);
        var loaded = await repository.GetByIdAsync(match.Id, CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(match.Id);
        loaded.Status.ShouldBe(MatchStatus.Waiting);
        loaded.BoardState.ShouldBe(".........");
        loaded.CurrentTurn.ShouldBe(PlayerSymbol.X);
        loaded.Outcome.ShouldBe(MatchOutcome.None);
        loaded.Revision.ShouldBe(0);
        loaded.CompletedAtUtc.ShouldBeNull();
        loaded.CreatedAtUtc.ShouldBe(match.CreatedAtUtc);

        var creator = loaded.Players.ShouldHaveSingleItem();
        creator.Id.ShouldBe(CreatorId);
        creator.MatchId.ShouldBe(match.Id);
        creator.DisplayName.ShouldBe("Alice");
        creator.Symbol.ShouldBe(PlayerSymbol.X);
        creator.TokenHash.ShouldBe("hash-alice");
    }

    [Test]
    public async Task AddAsync_WithoutUnitOfWorkSave_DoesNotPersist()
    {
        var match = GameMatch.Create(
            Guid.NewGuid(),
            CreatorId,
            "Alice",
            PlayerSymbol.X,
            "hash-alice");

        await using (var addingContext = Database.CreateContext())
        {
            await new GameMatchRepository(addingContext)
                .AddAsync(match, CancellationToken.None);
        }

        await using var verificationContext = Database.CreateContext();
        var loaded = await new GameMatchRepository(verificationContext)
            .GetByIdAsync(match.Id, CancellationToken.None);

        loaded.ShouldBeNull();
    }

    [Test]
    public async Task GetByIdAsync_ReturnsTrackedAggregate_SoJoinIsPersisted()
    {
        var match = GameMatch.Create(
            Guid.NewGuid(),
            CreatorId,
            "Alice",
            PlayerSymbol.X,
            "hash-alice");

        await PersistAsync(match);

        var joinerId = Guid.NewGuid();

        await using (var context = Database.CreateContext())
        {
            var repository = new GameMatchRepository(context);
            var loaded = await repository.GetByIdAsync(match.Id, CancellationToken.None);

            loaded.ShouldNotBeNull();
            context.Entry(loaded).State.ShouldBe(EntityState.Unchanged);

            var joiner = loaded.Join(joinerId, "Bob", "hash-bob");
            joiner.Symbol.ShouldBe(PlayerSymbol.O);

            await new TicTacToe.Infrastructure.Persistence.EfUnitOfWork(context)
                .SaveChangesAsync(CancellationToken.None);
        }

        await using var verificationContext = Database.CreateContext();
        var persisted = await new GameMatchRepository(verificationContext)
            .GetByIdAsync(match.Id, CancellationToken.None);

        persisted.ShouldNotBeNull();
        persisted.Status.ShouldBe(MatchStatus.InProgress);
        persisted.Revision.ShouldBe(1);
        persisted.Players.Count.ShouldBe(2);
        persisted.Players.Select(p => p.DisplayName).ShouldBe(
            new[] { "Alice", "Bob" },
            ignoreOrder: true);
        persisted.Players.Single(p => p.Id == joinerId).Symbol.ShouldBe(PlayerSymbol.O);
    }

    [Test]
    public async Task GetByIdAsync_UnknownMatchId_ReturnsNull()
    {
        await using var context = Database.CreateContext();
        var loaded = await new GameMatchRepository(context)
            .GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        loaded.ShouldBeNull();
    }
}
