using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CPDLCServer.Persistence;

// Has the SQLite migrations (Migrations/Sqlite)
public class SqliteCpdlcDbContext(DbContextOptions<SqliteCpdlcDbContext> options) : CpdlcDbContext(options);

// Lets "dotnet ef" make the SQLite migrations without the configuration of the server
public class SqliteCpdlcDbContextFactory : IDesignTimeDbContextFactory<SqliteCpdlcDbContext>
{
    public SqliteCpdlcDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SqliteCpdlcDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new SqliteCpdlcDbContext(options);
    }
}
