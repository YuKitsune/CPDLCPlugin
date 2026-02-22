using CPDLCServer.Contracts;

namespace CPDLCPlugin;

public class AircraftConnectionStore
{
    readonly List<AircraftConnection> _connectedAircraft = new();
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Populate(AircraftConnectionDto[] connections, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _connectedAircraft.Clear();
            foreach (var dto in connections)
            {
                _connectedAircraft.Add(new AircraftConnection(dto));
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Upsert(AircraftConnectionDto connectionDto, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Match by both callsign and stationId to support multiple connections per aircraft
            var existing = _connectedAircraft.FirstOrDefault(c =>
                c.Callsign == connectionDto.Callsign && c.StationId == connectionDto.StationId);
            if (existing is not null)
            {
                existing.UpdateDto(connectionDto);
            }
            else
            {
                _connectedAircraft.Add(new AircraftConnection(connectionDto));
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<AircraftConnection>> All(CancellationToken cancellationToken = default)
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
