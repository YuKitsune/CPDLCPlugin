namespace CPDLCPlugin;

public class AcarsStationStore
{
    readonly HashSet<string> _stationIds = new(StringComparer.OrdinalIgnoreCase);
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Populate(string[] stationIds, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _stationIds.Clear();
            foreach (var stationId in stationIds)
            {
                _stationIds.Add(stationId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> IsOnline(string stationId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _stationIds.Contains(stationId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<AcarsStation[]> All(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _stationIds.Select(id => new AcarsStation(id)).ToArray();
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
            _stationIds.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
