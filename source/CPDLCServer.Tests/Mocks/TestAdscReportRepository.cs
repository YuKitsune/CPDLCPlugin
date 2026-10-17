using CPDLCServer.Model;
using CPDLCServer.Persistence;

namespace CPDLCServer.Tests.Mocks;

public class TestAdscReportRepository : IAdscReportRepository
{
    private readonly InMemoryAdscReportRepository _inner = new();

    public Task Add(AircraftAdscReport report, CancellationToken cancellationToken)
    {
        return _inner.Add(report, cancellationToken);
    }

    public Task<AircraftAdscReport[]> All(CancellationToken cancellationToken)
    {
        return _inner.All(cancellationToken);
    }
}
