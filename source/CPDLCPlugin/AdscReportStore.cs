using CPDLCServer.Contracts;

namespace CPDLCPlugin;

/// <summary>
/// Holds the latest ADS-C report for each aircraft.
/// The reports stay after the contract ends, until the client disconnects.
/// </summary>
public class AdscReportStore
{
    readonly Dictionary<string, AdscReportDto> _latestReports = new();
    readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Stores the report, unless the store has a newer report for the same aircraft.
    /// An aircraft can report to more than one ATSU, so reports can arrive out of order.
    /// </summary>
    public async Task Add(AdscReportDto report, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_latestReports.TryGetValue(report.Callsign, out var existing) && existing.Timestamp > report.Timestamp)
                return;

            _latestReports[report.Callsign] = report;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<AdscReportDto?> Find(string callsign, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _latestReports.TryGetValue(callsign, out var report);
            return report;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<AdscReportDto>> All(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _latestReports.Values.ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Clear(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _latestReports.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
