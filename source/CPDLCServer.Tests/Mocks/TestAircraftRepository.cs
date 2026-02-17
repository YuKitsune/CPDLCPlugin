using CPDLCServer.Model;
using CPDLCServer.Persistence;

namespace CPDLCServer.Tests.Mocks;

public class TestAircraftRepository : IAircraftRepository
{
    private readonly InMemoryAircraftRepository _inner = new();

    public Task Add(AircraftKey aircraftKey, AircraftConnection connection, CancellationToken cancellationToken)
    {
        return _inner.Add(aircraftKey, connection, cancellationToken);
    }

    public Task<AircraftConnection?> Find(AircraftKey aircraftKey, CancellationToken cancellationToken)
    {
        return _inner.Find(aircraftKey, cancellationToken);
    }

    public Task<AircraftConnection[]> All(CancellationToken cancellationToken)
    {
        return _inner.All(cancellationToken);
    }

    public Task<bool> Remove(AircraftKey aircraftKey, CancellationToken cancellationToken)
    {
        return _inner.Remove(aircraftKey, cancellationToken);
    }
}
