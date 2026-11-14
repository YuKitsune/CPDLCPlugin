using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.Cpdlc.Connections;

public class AircraftRepositoryTests
{
    static readonly DateTimeOffset Start = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Find_ReturnsSavedConnection()
    {
        // Arrange
        using var database = new TestDatabase();
        var connection = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.NextDataAuthority);
        connection.RequestLogon(Start);
        connection.AcceptLogon(Start.AddSeconds(5));
        connection.PromoteToCurrentDataAuthority();
        connection.SetNextDataAuthority("YMMM", Start.AddHours(1));
        connection.SentNextDataAuthorityMessage();

        await using (var dbContext = database.CreateDbContext())
        {
            await new AircraftRepository(dbContext).Add(new("UAL123", "hoppies-ybbb"), connection, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using var readDbContext = database.CreateDbContext();
        var saved = await new AircraftRepository(readDbContext).Find(new("UAL123", "hoppies-ybbb"), CancellationToken.None);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal("YBBB", saved.StationId);
        Assert.Equal(DataAuthorityState.CurrentDataAuthority, saved.DataAuthorityState);
        Assert.Equal(ConnectionState.Connected, saved.ConnectionState);
        Assert.Equal(Start, saved.LogonRequested);
        Assert.Equal(Start.AddSeconds(5), saved.LogonAccepted);
        Assert.Equal(Start.AddSeconds(5), saved.LastSeen);
        Assert.Equal("YMMM", saved.NextDataAuthority);
        Assert.Equal(Start.AddHours(1), saved.ExpectedTransferTime);
        Assert.True(saved.DidSentNextDataAuthorityMessage);
    }

    [Fact]
    public async Task Add_WhenConnectionExists_ReplacesConnection()
    {
        // Arrange
        using var database = new TestDatabase();
        var first = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.NextDataAuthority);
        var second = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);

        await using (var dbContext = database.CreateDbContext())
        {
            await new AircraftRepository(dbContext).Add(new("UAL123", "hoppies-ybbb"), first, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using (var dbContext = database.CreateDbContext())
        {
            await new AircraftRepository(dbContext).Add(new("UAL123", "hoppies-ybbb"), second, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        await using var readDbContext = database.CreateDbContext();
        var connections = await new AircraftRepository(readDbContext).All(CancellationToken.None);
        var connection = Assert.Single(connections);
        Assert.Equal(DataAuthorityState.CurrentDataAuthority, connection.DataAuthorityState);
    }

    [Fact]
    public async Task Remove_InSameUnitOfWorkAsAdd_DoesNotSaveConnection()
    {
        // Arrange
        using var database = new TestDatabase();
        var connection = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.NextDataAuthority);

        // Act
        bool removed;
        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AircraftRepository(dbContext);
            await repository.Add(new("UAL123", "hoppies-ybbb"), connection, CancellationToken.None);
            removed = await repository.Remove(new("UAL123", "hoppies-ybbb"), CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        Assert.True(removed);
        await using var readDbContext = database.CreateDbContext();
        Assert.Empty(await new AircraftRepository(readDbContext).All(CancellationToken.None));
    }
}
