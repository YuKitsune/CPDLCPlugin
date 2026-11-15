using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.MessageLog;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.AdscContracts;

public class PurgeExpiredAdscDataRequestHandlerTests
{
    [Fact]
    public async Task Handle_RemovesAdscMessagesOlderThan24Hours()
    {
        // Arrange
        var clock = new TestClock();
        var adscMessageLogRepository = new TestAdscMessageLogRepository();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        var oldEntry = new AdscMessageLogEntry(
            startTime,
            "Hoppies/YBBB",
            "YBBB",
            "UAL123",
            AdscMessageDirection.Uplink,
            "REQUEST PERIODIC 1");
        var newEntry = new AdscMessageLogEntry(
            startTime.AddHours(2),
            "Hoppies/YBBB",
            "YBBB",
            "UAL123",
            AdscMessageDirection.Uplink,
            "REQUEST PERIODIC 2");
        await adscMessageLogRepository.Add(oldEntry, CancellationToken.None);
        await adscMessageLogRepository.Add(newEntry, CancellationToken.None);

        var handler = new PurgeExpiredAdscDataRequestHandler(adscMessageLogRepository, clock);

        // Act - advance time beyond the purge timeout (24 hours) for the old entry only
        clock.SetUtcNow(startTime.AddHours(25));
        await handler.Handle(new PurgeExpiredAdscDataRequest(), CancellationToken.None);

        // Assert
        Assert.Equal([newEntry], await adscMessageLogRepository.All(CancellationToken.None));
    }
}
