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
        var latest = await FindLatest(report.Callsign, cancellationToken);

        var isLatest = latest is null || ReplacesLatest(report, latest);
        if (isLatest && latest is not null)
            SetIsLatest(latest, false);

        dbContext.AdscReports.Add(report);
        SetIsLatest(report, isLatest);
    }

    public async Task<AircraftAdscReport?> FindLatest(string callsign, CancellationToken cancellationToken)
    {
        await dbContext.AdscReports
            .Where(r => r.Callsign == callsign && EF.Property<bool>(r, AircraftAdscReportConfiguration.IsLatestProperty))
            .LoadAsync(cancellationToken);

        return dbContext.AdscReports.Local.FirstOrDefault(r => r.Callsign == callsign && IsLatest(r));
    }

    public async Task<AircraftAdscReport[]> AllLatest(CancellationToken cancellationToken)
    {
        await dbContext.AdscReports
            .Where(r => EF.Property<bool>(r, AircraftAdscReportConfiguration.IsLatestProperty))
            .LoadAsync(cancellationToken);

        return dbContext.AdscReports.Local.Where(IsLatest).ToArray();
    }

    public async Task RemoveOlderThan(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        await dbContext.AdscReports
            .Where(r => r.Received < cutoff)
            .LoadAsync(cancellationToken);

        var oldReports = dbContext.AdscReports.Local.Where(r => r.Received < cutoff).ToArray();
        dbContext.AdscReports.RemoveRange(oldReports);
    }

    static bool ReplacesLatest(AircraftAdscReport report, AircraftAdscReport latest)
    {
        // A report from Hoppie always replaces a report from VATSIM
        if (report.Report.Source == AdscReportSource.Hoppie && latest.Report.Source == AdscReportSource.VATSIM)
            return true;

        // An aircraft can report to more than one ATSU, so reports can arrive out of order
        return report.Report.Timestamp >= latest.Report.Timestamp;
    }

    bool IsLatest(AircraftAdscReport report)
    {
        return dbContext.Entry(report).Property<bool>(AircraftAdscReportConfiguration.IsLatestProperty).CurrentValue;
    }

    void SetIsLatest(AircraftAdscReport report, bool isLatest)
    {
        dbContext.Entry(report).Property<bool>(AircraftAdscReportConfiguration.IsLatestProperty).CurrentValue = isLatest;
    }
}
