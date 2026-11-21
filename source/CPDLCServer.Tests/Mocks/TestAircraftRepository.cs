using CPDLCServer.Cpdlc.Connections;

namespace CPDLCServer.Tests.Mocks;

public class TestAircraftRepository : IAircraftRepository
{
    private readonly Dictionary<AircraftKey, AircraftConnection> _connections = new();

    public Task Add(AircraftKey aircraftKey, AircraftConnection connection, CancellationToken cancellationToken)
    {
        _connections[aircraftKey] = connection;
        return Task.CompletedTask;
    }

    public Task<AircraftConnection?> Find(AircraftKey aircraftKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(_connections.GetValueOrDefault(aircraftKey));
    }

    public Task<AircraftConnection[]> All(CancellationToken cancellationToken)
    {
        return Task.FromResult(_connections.Values.ToArray());
    }

    public Task<bool> Remove(AircraftKey aircraftKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(_connections.Remove(aircraftKey));
    }
}
