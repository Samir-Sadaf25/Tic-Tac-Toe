using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TicTacToe.Infrastructure.Persistence;

namespace TicTacToe.IntegrationTests.Persistence;

/// <summary>
/// Creates a unique SQL Server database for one test fixture, applies the real
/// EF Core migrations, and drops the database afterwards.
/// Set TIC_TAC_TOE_TEST_CONNECTION to use a SQL Server other than LocalDB.
/// </summary>
public sealed class SqlServerTestDatabase : IAsyncDisposable
{
    private const string DefaultBaseConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True";

    private SqlServerTestDatabase(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public static async Task<SqlServerTestDatabase> CreateAsync()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("TIC_TAC_TOE_TEST_CONNECTION")
            ?? DefaultBaseConnectionString;

        var serverBuilder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = "master"
        };

        try
        {
            await using var connection = new SqlConnection(serverBuilder.ConnectionString);
            await connection.OpenAsync();
        }
        catch (Exception ex)
        {
            Assert.Ignore(
                $"SQL Server is not available for integration tests ({ex.GetType().Name}). " +
                "Start LocalDB or set TIC_TAC_TOE_TEST_CONNECTION.");
        }

        var databaseBuilder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = $"TicTacToeTests_{Guid.NewGuid():N}"
        };

        var database = new SqlServerTestDatabase(databaseBuilder.ConnectionString);

        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();

        return database;
    }

    public GameDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new GameDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [GameMatches];");
    }

    public async ValueTask DisposeAsync()
    {
        SqlConnection.ClearAllPools();

        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
