using CPDLCServer.Contracts;

namespace CPDLCPlugin;

public class AircraftConnectionStore
{
    readonly List<AircraftConnectionDto> _connectedAircraft = new();
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Populate(AircraftConnectionDto[] connections, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _connectedAircraft.Clear();
            _connectedAircraft.AddRange(connections);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Upsert(AircraftConnectionDto connection, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Match by both callsign and stationId to support multiple connections per aircraft
            var existing = _connectedAircraft.FirstOrDefault(c =>
                c.Callsign == connection.Callsign && c.StationId == connection.StationId);
            if (existing is not null)
            {
                _connectedAircraft.Remove(existing);
            }

            _connectedAircraft.Add(connection);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<AircraftConnectionDto>> All(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _connectedAircraft.ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> IsConnected(string callsign, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var isConnected = _connectedAircraft.Any(c => c.Callsign == callsign);
             return isConnected;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Removes the specific connection for the callsign and stationId.
    /// </summary>
    public async Task<bool> Remove(string callsign, string stationId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _connectedAircraft.RemoveAll(c => c.Callsign == callsign && c.StationId == stationId) > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Removes all connections for the callsign.
    /// </summary>
    public async Task<bool> RemoveAll(string callsign, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _connectedAircraft.RemoveAll(c => c.Callsign == callsign) > 0;
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
            _connectedAircraft.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
