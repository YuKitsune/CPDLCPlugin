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

    [Fact]
    public async Task Add_WhenHoppieReportIsOlderThanVatsimReport_ReplacesVatsimReport()
    {
        // Arrange
        var repository = new InMemoryAdscReportRepository();
        var vatsimReport = CreateReport("UAL123", "YBBB", 5, AdscReportSource.VATSIM);
        var hoppieReport = CreateReport("UAL123", "YBBB", 0);

        // Act
        await repository.Add(vatsimReport, CancellationToken.None);
        await repository.Add(hoppieReport, CancellationToken.None);

        // Assert
        Assert.Equal([hoppieReport], await repository.All(CancellationToken.None));
    }

    [Fact]
    public async Task Add_WhenVatsimReportIsOlderThanHoppieReport_KeepsHoppieReport()
    {
        // Arrange
        var repository = new InMemoryAdscReportRepository();
        var hoppieReport = CreateReport("UAL123", "YBBB", 5);
        var vatsimReport = CreateReport("UAL123", "YBBB", 0, AdscReportSource.VATSIM);

        // Act
        await repository.Add(hoppieReport, CancellationToken.None);
        await repository.Add(vatsimReport, CancellationToken.None);

        // Assert
        Assert.Equal([hoppieReport], await repository.All(CancellationToken.None));
    }

    [Fact]
    public async Task Find_ReturnsLatestReportForAircraft()
    {
        // Arrange
        var repository = new InMemoryAdscReportRepository();
        var report = CreateReport("UAL123", "YBBB", 0);
        await repository.Add(report, CancellationToken.None);

        // Act & Assert
        Assert.Equal(report, await repository.Find("UAL123", CancellationToken.None));
        Assert.Null(await repository.Find("QFA1", CancellationToken.None));
    }

    [Fact]
    public async Task RemoveOlderThan_RemovesOnlyOlderReports()
    {
        // Arrange
        var repository = new InMemoryAdscReportRepository();
        var older = CreateReport("UAL123", "YBBB", 0);
        var atCutoff = CreateReport("QFA1", "YBBB", 5);
        var newer = CreateReport("VOZ1", "YBBB", 10);
        await repository.Add(older, CancellationToken.None);
        await repository.Add(atCutoff, CancellationToken.None);
        await repository.Add(newer, CancellationToken.None);

        // Act
        await repository.RemoveOlderThan(atCutoff.Received, CancellationToken.None);

        // Assert
        var reports = await repository.All(CancellationToken.None);
        Assert.Equal(2, reports.Length);
        Assert.Contains(atCutoff, reports);
        Assert.Contains(newer, reports);
    }

    static AircraftAdscReport CreateReport(
        string callsign,
        string stationId,
        int minutes,
        AdscReportSource source = AdscReportSource.Hoppie)
    {
        var timestamp = Start.AddMinutes(minutes);
        return new AircraftAdscReport(
            callsign,
            stationId,
            new AdscReport(timestamp, -33.9461, 151.1772, 37000, source),
            timestamp.AddSeconds(30));
    }
}
