using NUnit.Framework;
using TicTacToe.Domain.Entities;
using TicTacToe.Infrastructure.Persistence;
using TicTacToe.Infrastructure.Persistence.Repositories;

namespace TicTacToe.IntegrationTests.Persistence;

public abstract class SqlServerTestBase
{
    private SqlServerTestDatabase? _database;

    protected SqlServerTestDatabase Database =>
        _database ?? throw new InvalidOperationException("The SQL Server test database is not initialized.");

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _database = await SqlServerTestDatabase.CreateAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    [SetUp]
    public async Task ResetDatabase()
    {
        if (_database is not null)
        {
            await _database.ResetAsync();
        }
    }

    protected async Task PersistAsync(params GameMatch[] matches)
    {
        await using var context = Database.CreateContext();
        var repository = new GameMatchRepository(context);

        foreach (var match in matches)
        {
            await repository.AddAsync(match, CancellationToken.None);
        }

        await new EfUnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }
}
