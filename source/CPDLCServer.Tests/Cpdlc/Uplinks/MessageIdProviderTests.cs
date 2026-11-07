using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.Cpdlc.Uplinks;

public class MessageIdProviderTests
{
    [Fact]
    public async Task GetNextMessageId_FirstCall_ReturnsOne()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var dbContext = database.CreateDbContext();
        var provider = new MessageIdProvider(dbContext);

        // Act
        var messageId = await provider.GetNextMessageId("YBBB", "UAL123", CancellationToken.None);

        // Assert
        Assert.Equal(1, messageId);
    }

    [Fact]
    public async Task GetNextMessageId_SequentialCalls_IncrementsForSamePair()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var dbContext = database.CreateDbContext();
        var provider = new MessageIdProvider(dbContext);

        // Act
        var id1 = await provider.GetNextMessageId("YBBB", "UAL123", CancellationToken.None);
        var id2 = await provider.GetNextMessageId("YBBB", "UAL123", CancellationToken.None);
        var id3 = await provider.GetNextMessageId("YBBB", "UAL123", CancellationToken.None);

        // Assert
        Assert.Equal(1, id1);
        Assert.Equal(2, id2);
        Assert.Equal(3, id3);
    }

    [Fact]
    public async Task GetNextMessageId_DifferentCallsigns_MaintainsSeparateSequences()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var dbContext = database.CreateDbContext();
        var provider = new MessageIdProvider(dbContext);

        // Act
        var ual1 = await provider.GetNextMessageId("YBBB", "UAL123", CancellationToken.None);
        var dal1 = await provider.GetNextMessageId("YBBB", "DAL456", CancellationToken.None);
        var ual2 = await provider.GetNextMessageId("YBBB", "UAL123", CancellationToken.None);
        var dal2 = await provider.GetNextMessageId("YBBB", "DAL456", CancellationToken.None);

        // Assert
        Assert.Equal(1, ual1);
        Assert.Equal(1, dal1);
        Assert.Equal(2, ual2);
        Assert.Equal(2, dal2);
    }

    [Fact]
    public async Task GetNextMessageId_DifferentStations_MaintainsSeparateSequences()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var dbContext = database.CreateDbContext();
        var provider = new MessageIdProvider(dbContext);

        // Act
        var ybbb1 = await provider.GetNextMessageId("YBBB", "UAL123", CancellationToken.None);
        var ymmm1 = await provider.GetNextMessageId("YMMM", "UAL123", CancellationToken.None);
        var ybbb2 = await provider.GetNextMessageId("YBBB", "UAL123", CancellationToken.None);
        var ymmm2 = await provider.GetNextMessageId("YMMM", "UAL123", CancellationToken.None);

        // Assert
        Assert.Equal(1, ybbb1);
        Assert.Equal(1, ymmm1);
        Assert.Equal(2, ybbb2);
        Assert.Equal(2, ymmm2);
    }

    [Fact]
    public async Task GetNextMessageId_AfterRestart_ContinuesSequence()
    {
        // Arrange
        using var database = new TestDatabase();

        await using (var dbContext = database.CreateDbContext())
        {
            var provider = new MessageIdProvider(dbContext);
            await provider.GetNextMessageId("YBBB", "UAL123", CancellationToken.None);
            await provider.GetNextMessageId("YBBB", "UAL123", CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using var newDbContext = database.CreateDbContext();
        var newProvider = new MessageIdProvider(newDbContext);
        var messageId = await newProvider.GetNextMessageId("YBBB", "UAL123", CancellationToken.None);

        // Assert
        Assert.Equal(3, messageId);
    }
}
