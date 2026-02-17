using CPDLCServer.Extensions;
using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public class InMemoryAircraftRepository : IAircraftRepository
{
    readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly Dictionary<AircraftKey, AircraftConnection> _connections = new();

    public async Task Add(AircraftKey aircraftKey, AircraftConnection connection, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _connections[aircraftKey] = connection;
        }
    }

    public async Task<AircraftConnection?> Find(AircraftKey aircraftKey, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _connections.TryGetValue(aircraftKey, out var connection);
            return connection;
        }
    }

    public async Task<AircraftConnection[]> All(CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _connections
                .Select(kvp => kvp.Value)
                .ToArray();
        }
    }

    public async Task<bool> Remove(AircraftKey aircraftKey, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _connections.Remove(aircraftKey);
        }
    }
}
