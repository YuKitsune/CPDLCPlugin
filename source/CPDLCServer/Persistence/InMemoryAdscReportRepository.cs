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
            if (_reports.TryGetValue(report.Callsign, out var existing))
            {
                // A report from Hoppie always replaces a report from VATSIM
                var replacesVatsimReport = report.Report.Source == AdscReportSource.Hoppie &&
                    existing.Report.Source == AdscReportSource.VATSIM;

                // An aircraft can report to more than one ATSU, so reports can arrive out of order
                if (!replacesVatsimReport && existing.Report.Timestamp > report.Report.Timestamp)
                    return;
            }

            _reports[report.Callsign] = report;
        }
    }

    public async Task<AircraftAdscReport?> Find(string callsign, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _reports.GetValueOrDefault(callsign);
        }
    }

    public async Task<AircraftAdscReport[]> All(CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _reports.Values.ToArray();
        }
    }

    public async Task RemoveOlderThan(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            foreach (var report in _reports.Values.Where(r => r.Received < cutoff).ToArray())
                _reports.Remove(report.Callsign);
        }
    }
}
