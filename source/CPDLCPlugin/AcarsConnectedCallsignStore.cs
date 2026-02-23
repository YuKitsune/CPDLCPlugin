namespace CPDLCPlugin;

public class AcarsConnectedCallsignStore
{
    readonly HashSet<string> _callsigns = new(StringComparer.OrdinalIgnoreCase);
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Populate(string[] callsigns, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _callsigns.Clear();
            foreach (var callsign in callsigns)
            {
                _callsigns.Add(callsign);
            }
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
            return _callsigns.Contains(callsign);
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
            _callsigns.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
