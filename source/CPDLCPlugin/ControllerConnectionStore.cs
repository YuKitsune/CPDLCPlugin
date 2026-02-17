using CPDLCServer.Contracts;

namespace CPDLCPlugin;

public class ControllerConnectionStore
{
    readonly List<ControllerConnectionDto> _connectedControllers = new();
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Populate(ControllerConnectionDto[] connections, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _connectedControllers.Clear();
            _connectedControllers.AddRange(connections);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Upsert(ControllerConnectionDto connection, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var existing = _connectedControllers.FirstOrDefault(c => c.Callsign == connection.Callsign);
            if (existing is not null)
            {
                _connectedControllers.Remove(existing);
            }

            _connectedControllers.Add(connection);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ControllerConnectionDto?> Find(string callsign, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var connection = _connectedControllers.FirstOrDefault(c => c.Callsign == callsign);
            return connection;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<ControllerConnectionDto>> All(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _connectedControllers.ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public bool IsConnected(string callsign)
    {
        _semaphore.Wait();
        try
        {
            return _connectedControllers.Any(c => c.Callsign == callsign);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> Remove(string callsign, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _connectedControllers.RemoveAll(c => c.Callsign == callsign) > 0;
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
            _connectedControllers.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
