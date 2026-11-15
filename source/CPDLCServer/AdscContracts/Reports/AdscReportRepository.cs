using CPDLCServer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.AdscContracts.Reports;

// The changes are saved when the unit of work is committed.
// Queries load the rows into the context, then read the local entities, so that the results include entities that are
// added in the unit of work and exclude entities that are removed in it.
public class AdscReportRepository(CpdlcDbContext dbContext) : IAdscReportRepository
{
    public async Task Add(AircraftAdscReport report, CancellationToken cancellationToken)
    {
        var previous = await Find(report.Callsign, report.StationId, cancellationToken);
        if (previous is not null)
            dbContext.AdscReports.Remove(previous);

        dbContext.AdscReports.Add(report);
    }

    public async Task<AircraftAdscReport?> Find(string callsign, string stationId, CancellationToken cancellationToken)
    {
        await dbContext.AdscReports
            .Where(r => r.Callsign == callsign && r.StationId == stationId)
            .LoadAsync(cancellationToken);

        return dbContext.AdscReports.Local.FirstOrDefault(r => r.Callsign == callsign && r.StationId == stationId);
    }

    public async Task<AircraftAdscReport?> FindLatest(string callsign, CancellationToken cancellationToken)
    {
        await dbContext.AdscReports
            .Where(r => r.Callsign == callsign)
            .LoadAsync(cancellationToken);

        var reports = dbContext.AdscReports.Local.Where(r => r.Callsign == callsign).ToArray();
        return reports.Length == 0 ? null : SelectLatest(reports);
    }

    public async Task<AircraftAdscReport[]> AllLatest(CancellationToken cancellationToken)
    {
        var reports = await All(cancellationToken);
        return reports
            .GroupBy(r => r.Callsign)
            .Select(SelectLatest)
            .ToArray();
    }

    public async Task<AircraftAdscReport[]> All(CancellationToken cancellationToken)
    {
        await dbContext.AdscReports.LoadAsync(cancellationToken);
        return dbContext.AdscReports.Local.ToArray();
    }

    public Task Remove(AircraftAdscReport report, CancellationToken cancellationToken)
    {
        dbContext.AdscReports.Remove(report);
        return Task.CompletedTask;
    }

    public async Task RemoveOlderThan(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        await dbContext.AdscReports
            .Where(r => r.Received < cutoff)
            .LoadAsync(cancellationToken);

        var oldReports = dbContext.AdscReports.Local.Where(r => r.Received < cutoff).ToArray();
        dbContext.AdscReports.RemoveRange(oldReports);
    }

    // An aircraft can report to more than one ATSU, so the reports can arrive out of order
    static AircraftAdscReport SelectLatest(IEnumerable<AircraftAdscReport> reports)
    {
        return reports
            .OrderByDescending(r => r.Report.Source == AdscReportSource.Hoppie)
            .ThenByDescending(r => r.Report.Timestamp)
            .First();
    }
}
