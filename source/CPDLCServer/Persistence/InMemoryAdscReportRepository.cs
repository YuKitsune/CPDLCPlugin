using CPDLCServer.Extensions;
using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public class InMemoryAdscReportRepository : IAdscReportRepository
{
    readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly Dictionary<string, AircraftAdscReport> _reports = new();

    public async Task Add(AircraftAdscReport report, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            // An aircraft can report to more than one ATSU, so reports can arrive out of order
            if (_reports.TryGetValue(report.Callsign, out var existing) &&
                existing.Report.Timestamp > report.Report.Timestamp)
                return;

            _reports[report.Callsign] = report;
        }
    }

    public async Task<AircraftAdscReport[]> All(CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _reports.Values.ToArray();
        }
    }
}
