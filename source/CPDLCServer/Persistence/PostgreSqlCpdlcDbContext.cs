using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CPDLCServer.Persistence;

// Has the PostgreSQL migrations (Migrations/PostgreSql)
public class PostgreSqlCpdlcDbContext(DbContextOptions<PostgreSqlCpdlcDbContext> options) : CpdlcDbContext(options);

// Lets "dotnet ef" make the PostgreSQL migrations without the configuration of the server.
// "dotnet ef migrations add" does not connect to the database.
public class PostgreSqlCpdlcDbContextFactory : IDesignTimeDbContextFactory<PostgreSqlCpdlcDbContext>
{
    public PostgreSqlCpdlcDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PostgreSqlCpdlcDbContext>()
            .UseNpgsql("Host=localhost;Database=cpdlc")
            .Options;

        return new PostgreSqlCpdlcDbContext(options);
    }
}
