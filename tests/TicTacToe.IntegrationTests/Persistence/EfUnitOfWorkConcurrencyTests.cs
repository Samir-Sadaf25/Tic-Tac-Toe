using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Application.Common.Exceptions;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Enums;
using TicTacToe.Infrastructure.Persistence;
using TicTacToe.Infrastructure.Persistence.Repositories;

namespace TicTacToe.IntegrationTests.Persistence;

[TestFixture]
public class EfUnitOfWorkConcurrencyTests : SqlServerTestBase
{
    [Test]
    public async Task SaveChangesAsync_WhenRowVersionIsStale_ThrowsMatchConflictAndDoesNotPersistLoser()
    {
        var match = GameMatch.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alice",
            PlayerSymbol.X,
            "hash-alice");
        await PersistAsync(match);

        var bobId = Guid.NewGuid();
        var charlieId = Guid.NewGuid();

        await using var firstContext = Database.CreateContext();
        await using var secondContext = Database.CreateContext();

        // Both requests read the same revision before either writes.
        var firstMatch = await new GameMatchRepository(firstContext)
            .GetByIdAsync(match.Id, CancellationToken.None);
        var secondMatch = await new GameMatchRepository(secondContext)
            .GetByIdAsync(match.Id, CancellationToken.None);

        firstMatch.ShouldNotBeNull();
        secondMatch.ShouldNotBeNull();
        firstMatch.Revision.ShouldBe(0);
        secondMatch.Revision.ShouldBe(0);

        firstMatch.Join(bobId, "Bob", "hash-bob");
        await new EfUnitOfWork(firstContext).SaveChangesAsync(CancellationToken.None);

        secondMatch.Join(charlieId, "Charlie", "hash-charlie");

        await Should.ThrowAsync<MatchConflictException>(async () =>
            await new EfUnitOfWork(secondContext).SaveChangesAsync(CancellationToken.None));

        await using var verificationContext = Database.CreateContext();
        var persisted = await new GameMatchRepository(verificationContext)
            .GetByIdAsync(match.Id, CancellationToken.None);

        persisted.ShouldNotBeNull();
        persisted.Revision.ShouldBe(1);
        persisted.Players.Count.ShouldBe(2);
        persisted.Players.Select(p => p.DisplayName)
            .ShouldBe(new[] { "Alice", "Bob" }, ignoreOrder: true);
        persisted.Players.Any(p => p.Id == charlieId).ShouldBeFalse();
        persisted.Players.Any(p => p.DisplayName == "Charlie").ShouldBeFalse();
    }

    [Test]
    public async Task SaveChangesAsync_OnAcceptedUpdate_AdvancesRowVersionInDatabase()
    {
        var match = GameMatch.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alice",
            PlayerSymbol.X,
            "hash-alice");
        await PersistAsync(match);

        byte[]? before;

        await using (var context = Database.CreateContext())
        {
            var loaded = await new GameMatchRepository(context)
                .GetByIdAsync(match.Id, CancellationToken.None);
            loaded.ShouldNotBeNull();

            before = context.Entry(loaded).Property<byte[]>("RowVersion").CurrentValue;
            before.ShouldNotBeNull();
            before.Length.ShouldBe(8);

            loaded.Join(Guid.NewGuid(), "Bob", "hash-bob");
            await new EfUnitOfWork(context).SaveChangesAsync(CancellationToken.None);
        }

        await using var verificationContext = Database.CreateContext();
        var persisted = await new GameMatchRepository(verificationContext)
            .GetByIdAsync(match.Id, CancellationToken.None);
        persisted.ShouldNotBeNull();

        var after = verificationContext.Entry(persisted)
            .Property<byte[]>("RowVersion").CurrentValue;

        after.ShouldNotBeNull();
        after.ShouldNotBe(before);
    }

    [Test]
    public async Task SaveChangesAsync_WithNoTrackedChanges_ReturnsZero()
    {
        await using var context = Database.CreateContext();

        var affected = await new EfUnitOfWork(context)
            .SaveChangesAsync(CancellationToken.None);

        affected.ShouldBe(0);
    }
}
