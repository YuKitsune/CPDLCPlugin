using CPDLCServer.AdscContracts.Reports;

namespace CPDLCServer.Tests.Mocks;

public class TestAdscReportRepository : IAdscReportRepository
{
    private readonly List<AircraftAdscReport> _history = [];
    private readonly Dictionary<string, AircraftAdscReport> _latest = new();

    public Task Add(AircraftAdscReport report, CancellationToken cancellationToken)
    {
        _history.Add(report);

        if (_latest.TryGetValue(report.Callsign, out var latest))
        {
            var replacesVatsimReport = report.Report.Source == AdscReportSource.Hoppie &&
                latest.Report.Source == AdscReportSource.VATSIM;

            if (!replacesVatsimReport && latest.Report.Timestamp > report.Report.Timestamp)
                return Task.CompletedTask;
        }

        _latest[report.Callsign] = report;
        return Task.CompletedTask;
    }

    public Task<AircraftAdscReport?> FindLatest(string callsign, CancellationToken cancellationToken)
    {
        return Task.FromResult(_latest.GetValueOrDefault(callsign));
    }

    public Task<AircraftAdscReport[]> AllLatest(CancellationToken cancellationToken)
    {
        return Task.FromResult(_latest.Values.ToArray());
    }

    public Task RemoveOlderThan(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        _history.RemoveAll(r => r.Received < cutoff);

        foreach (var report in _latest.Values.Where(r => r.Received < cutoff).ToArray())
            _latest.Remove(report.Callsign);

        return Task.CompletedTask;
    }
}
