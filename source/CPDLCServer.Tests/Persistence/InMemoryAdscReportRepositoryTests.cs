using CPDLCServer.Model;
using CPDLCServer.Persistence;

namespace CPDLCServer.Tests.Persistence;

public class InMemoryAdscReportRepositoryTests
{
    static readonly DateTimeOffset Start = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Add_KeepsLatestReportForEachAircraft()
    {
        // Arrange
        var repository = new InMemoryAdscReportRepository();
        var first = CreateReport("UAL123", "YBBB", 0);
        var second = CreateReport("UAL123", "YBBB", 5);
        var other = CreateReport("QFA1", "YBBB", 0);

        // Act
        await repository.Add(first, CancellationToken.None);
        await repository.Add(second, CancellationToken.None);
        await repository.Add(other, CancellationToken.None);

        // Assert
        var reports = await repository.All(CancellationToken.None);
        Assert.Equal(2, reports.Length);
        Assert.Contains(second, reports);
        Assert.Contains(other, reports);
    }

    [Fact]
    public async Task Add_WhenReportIsOlder_KeepsNewerReport()
    {
        // Arrange
        var repository = new InMemoryAdscReportRepository();
        var newer = CreateReport("UAL123", "YBBB", 5);
        var older = CreateReport("UAL123", "YMMM", 0);

        // Act
        await repository.Add(newer, CancellationToken.None);
        await repository.Add(older, CancellationToken.None);

        // Assert
        Assert.Equal([newer], await repository.All(CancellationToken.None));
    }

    static AircraftAdscReport CreateReport(string callsign, string stationId, int minutes)
    {
        var timestamp = Start.AddMinutes(minutes);
        return new AircraftAdscReport(
            callsign,
            stationId,
            new AdscReport(timestamp, -33.9461, 151.1772, 37000),
            timestamp.AddSeconds(30));
    }
}
