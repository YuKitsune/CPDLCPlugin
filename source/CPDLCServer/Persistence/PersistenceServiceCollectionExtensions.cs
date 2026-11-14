using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    // Registers the context of the provider. The repositories use it as CpdlcDbContext.
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        DatabaseProvider provider,
        string databasePath)
    {
        switch (provider)
        {
            case DatabaseProvider.Sqlite:
                services.AddDbContext<SqliteCpdlcDbContext>(options =>
                    options.UseSqlite($"Data Source={databasePath}"));
                services.AddScoped<CpdlcDbContext>(sp => sp.GetRequiredService<SqliteCpdlcDbContext>());
                break;

            case DatabaseProvider.PostgreSql:
                var connectionString = configuration["DatabaseConnectionString"];
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new Exception("The PostgreSQL database provider needs DatabaseConnectionString.");

                services.AddDbContext<PostgreSqlCpdlcDbContext>(options =>
                    options.UseNpgsql(PostgreSqlConnectionString.Parse(connectionString)));
                services.AddScoped<CpdlcDbContext>(sp => sp.GetRequiredService<PostgreSqlCpdlcDbContext>());
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }

        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>());

        return services;
    }
}
