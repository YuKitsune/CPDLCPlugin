using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.MessageLog;
using CPDLCServer.AdscContracts.Reports;
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

        var handler = new PurgeExpiredAdscDataRequestHandler(adscMessageLogRepository, new TestAdscReportRepository(), clock);

        // Act - advance time beyond the purge timeout (24 hours) for the old entry only
        clock.SetUtcNow(startTime.AddHours(25));
        await handler.Handle(new PurgeExpiredAdscDataRequest(TimeSpan.FromHours(24)), CancellationToken.None);

        // Assert
        Assert.Equal([newEntry], await adscMessageLogRepository.All(CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RemovesAdscReportsOlderThanRetention()
    {
        // Arrange
        var clock = new TestClock();
        var adscReportRepository = new TestAdscReportRepository();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        var oldReport = new AircraftAdscReport(
            "UAL123",
            "YBBB",
            new AdscReport(startTime, -33.9461, 151.1772, 37000, AdscReportSource.Hoppie),
            startTime);
        var newReport = new AircraftAdscReport(
            "QFA1",
            "YBBB",
            new AdscReport(startTime.AddHours(2), -33.9461, 151.1772, 37000, AdscReportSource.Hoppie),
            startTime.AddHours(2));
        await adscReportRepository.Add(oldReport, CancellationToken.None);
        await adscReportRepository.Add(newReport, CancellationToken.None);

        var handler = new PurgeExpiredAdscDataRequestHandler(new TestAdscMessageLogRepository(), adscReportRepository, clock);

        // Act - advance time beyond the retention (24 hours) for the old report only
        clock.SetUtcNow(startTime.AddHours(25));
        await handler.Handle(new PurgeExpiredAdscDataRequest(TimeSpan.FromHours(24)), CancellationToken.None);

        // Assert
        Assert.Equal([newReport], await adscReportRepository.AllLatest(CancellationToken.None));
    }
}
