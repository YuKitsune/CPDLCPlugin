namespace CPDLCServer.Services;

public interface IMessageIdProvider
{
    Task<int> GetNextMessageId(
        string acarsClientId,
        string callsign,
        CancellationToken cancellationToken);
}

public class MessageIdProvider : IMessageIdProvider, IDisposable
{
    private readonly Dictionary<Key, int> _ids = new();
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<int> GetNextMessageId(
        string acarsClientId,
        string callsign,
        CancellationToken cancellationToken)
    {
        await  _semaphore.WaitAsync(cancellationToken);
        try
        {
            var key = new Key(acarsClientId, callsign);
            if (!_ids.TryGetValue(key, out var nextId))
            {
                nextId = 0;
            }

            return _ids[key] = nextId + 1;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    record Key(string AcarsClientId, string Callsign);

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
