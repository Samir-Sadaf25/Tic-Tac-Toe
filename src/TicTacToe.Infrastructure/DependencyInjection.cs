using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Application.Abstractions.Persistence;
using TicTacToe.Application.Abstractions.Security;
using TicTacToe.Infrastructure.Persistence;
using TicTacToe.Infrastructure.Persistence.Repositories;
using TicTacToe.Infrastructure.Security;

namespace TicTacToe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Set it with 'dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<your-connection-string>\"' " +
                "from src/TicTacToe.Web, or use the ConnectionStrings__DefaultConnection environment variable.");
        }

        services.AddDbContext<GameDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IGameMatchRepository, GameMatchRepository>();
        services.AddScoped<IMatchReadRepository, MatchReadRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IGuestTokenService, GuestTokenService>();

        return services;
    }
}
