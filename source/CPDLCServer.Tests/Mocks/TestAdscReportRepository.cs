using CPDLCServer.AdscContracts.Reports;

namespace CPDLCServer.Tests.Mocks;

public class TestAdscReportRepository : IAdscReportRepository
{
    private readonly List<AircraftAdscReport> _reports = [];

    public Task Add(AircraftAdscReport report, CancellationToken cancellationToken)
    {
        _reports.RemoveAll(r => r.Callsign == report.Callsign && r.StationId == report.StationId);
        _reports.Add(report);
        return Task.CompletedTask;
    }

    public Task<AircraftAdscReport?> Find(string callsign, string stationId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_reports.FirstOrDefault(r => r.Callsign == callsign && r.StationId == stationId));
    }

    public Task<AircraftAdscReport?> FindLatest(string callsign, CancellationToken cancellationToken)
    {
        var reports = _reports.Where(r => r.Callsign == callsign).ToArray();
        return Task.FromResult(reports.Length == 0 ? null : SelectLatest(reports));
    }

    public Task<AircraftAdscReport[]> AllLatest(CancellationToken cancellationToken)
    {
        return Task.FromResult(_reports.GroupBy(r => r.Callsign).Select(SelectLatest).ToArray());
    }

    public Task<AircraftAdscReport[]> All(CancellationToken cancellationToken)
    {
        return Task.FromResult(_reports.ToArray());
    }

    public Task Remove(AircraftAdscReport report, CancellationToken cancellationToken)
    {
        _reports.Remove(report);
        return Task.CompletedTask;
    }

    static AircraftAdscReport SelectLatest(IEnumerable<AircraftAdscReport> reports)
    {
        return reports
            .OrderByDescending(r => r.Report.Source == AdscReportSource.Hoppie)
            .ThenByDescending(r => r.Report.Timestamp)
            .First();
    }
}
