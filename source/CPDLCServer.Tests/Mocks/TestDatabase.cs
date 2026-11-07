using CPDLCServer.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Tests.Mocks;

// An in-memory SQLite database. The database exists while the connection is open.
// Use a new context for each unit of work, so that the tests read the saved data and not the tracked entities.
public sealed class TestDatabase : IDisposable
{
    readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();
    }

    public CpdlcDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CpdlcDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new CpdlcDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
