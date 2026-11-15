using CPDLCServer.AdscContracts.Reports;
using CPDLCServer.Tests.Mocks;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Tests.AdscContracts.Reports;

public class AdscReportRepositoryTests
{
    static readonly DateTimeOffset Start = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Add_KeepsOneReportForEachContract()
    {
        // Arrange
        using var database = new TestDatabase();
        var first = CreateReport("UAL123", "YBBB", 0);
        var second = CreateReport("UAL123", "YBBB", 5);
        var otherStation = CreateReport("UAL123", "YMMM", 0);
        var otherAircraft = CreateReport("QFA1", "YBBB", 0);

        // Act
        await AddAndSave(database, first, otherStation, otherAircraft);
        await AddAndSave(database, second);

        // Assert
        await using var dbContext = database.CreateDbContext();
        var reports = await dbContext.AdscReports.ToArrayAsync();
        Assert.Equal(3, reports.Length);
        Assert.Contains(second, reports);
        Assert.Contains(otherStation, reports);
        Assert.Contains(otherAircraft, reports);
    }

    [Fact]
    public async Task Add_InSameUnitOfWork_KeepsOneReportForEachContract()
    {
        // Arrange
        using var database = new TestDatabase();
        var first = CreateReport("UAL123", "YBBB", 0);
        var second = CreateReport("UAL123", "YBBB", 5);

        // Act
        await AddAndSave(database, first, second);

        // Assert
        await using var dbContext = database.CreateDbContext();
        Assert.Equal([second], await dbContext.AdscReports.ToArrayAsync());
    }

    [Fact]
    public async Task AllLatest_ReturnsLatestReportForEachAircraft()
    {
        // Arrange
        using var database = new TestDatabase();
        var older = CreateReport("UAL123", "YBBB", 0);
        var newer = CreateReport("UAL123", "YMMM", 5);
        var other = CreateReport("QFA1", "YBBB", 0);
        await AddAndSave(database, older, newer, other);

        // Act
        var reports = await AllLatest(database);

        // Assert
        Assert.Equal(2, reports.Length);
        Assert.Contains(newer, reports);
        Assert.Contains(other, reports);
    }

    [Fact]
    public async Task AllLatest_WhenVatsimReportIsNewerThanHoppieReport_ReturnsHoppieReport()
    {
        // Arrange
        using var database = new TestDatabase();
        var hoppieReport = CreateReport("UAL123", "YBBB", 0);
        var vatsimReport = CreateReport("UAL123", "YMMM", 5, AdscReportSource.VATSIM);
        await AddAndSave(database, hoppieReport, vatsimReport);

        // Act & Assert
        Assert.Equal([hoppieReport], await AllLatest(database));
    }

    [Fact]
    public async Task FindLatest_ReturnsLatestReportForAircraft()
    {
        // Arrange
        using var database = new TestDatabase();
        var report = CreateReport("UAL123", "YBBB", 0);
        await AddAndSave(database, report);

        // Act & Assert
        await using var dbContext = database.CreateDbContext();
        var repository = new AdscReportRepository(dbContext);
        Assert.Equal(report, await repository.FindLatest("UAL123", CancellationToken.None));
        Assert.Null(await repository.FindLatest("QFA1", CancellationToken.None));
    }

    [Fact]
    public async Task RemoveOlderThan_RemovesOnlyOlderReports()
    {
        // Arrange
        using var database = new TestDatabase();
        var older = CreateReport("UAL123", "YBBB", 0);
        var atCutoff = CreateReport("QFA1", "YBBB", 5);
        var newer = CreateReport("VOZ1", "YBBB", 10);
        await AddAndSave(database, older, atCutoff, newer);

        // Act
        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AdscReportRepository(dbContext);
            await repository.RemoveOlderThan(atCutoff.Received, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        var reports = await AllLatest(database);
        Assert.Equal(2, reports.Length);
        Assert.Contains(atCutoff, reports);
        Assert.Contains(newer, reports);
    }

    [Fact]
    public async Task Find_ReturnsReportForContract()
    {
        // Arrange
        using var database = new TestDatabase();
        var report = CreateReport("UAL123", "YBBB", 0);
        await AddAndSave(database, report);

        // Act & Assert
        await using var dbContext = database.CreateDbContext();
        var repository = new AdscReportRepository(dbContext);
        Assert.Equal(report, await repository.Find("UAL123", "YBBB", CancellationToken.None));
        Assert.Null(await repository.Find("UAL123", "YMMM", CancellationToken.None));
    }

    [Fact]
    public async Task Remove_RemovesReport()
    {
        // Arrange
        using var database = new TestDatabase();
        var removed = CreateReport("UAL123", "YBBB", 0);
        var kept = CreateReport("QFA1", "YBBB", 0);
        await AddAndSave(database, removed, kept);

        // Act
        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new AdscReportRepository(dbContext);
            var report = await repository.Find("UAL123", "YBBB", CancellationToken.None);
            await repository.Remove(report!, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        Assert.Equal([kept], await AllLatest(database));
    }

    static async Task AddAndSave(TestDatabase database, params AircraftAdscReport[] reports)
    {
        await using var dbContext = database.CreateDbContext();
        var repository = new AdscReportRepository(dbContext);

        foreach (var report in reports)
            await repository.Add(report, CancellationToken.None);

        await dbContext.SaveChangesAsync();
    }

    static async Task<AircraftAdscReport[]> AllLatest(TestDatabase database)
    {
        await using var dbContext = database.CreateDbContext();
        var repository = new AdscReportRepository(dbContext);
        return await repository.AllLatest(CancellationToken.None);
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
