using CPDLCServer.Extensions;
using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public class InMemoryAdscMessageLogRepository : IAdscMessageLogRepository
{
    // The oldest entries are removed when the log is full
    public const int Capacity = 1000;

    readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly Queue<AdscMessageLogEntry> _entries = new();

    public async Task Add(AdscMessageLogEntry entry, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _entries.Enqueue(entry);
            while (_entries.Count > Capacity)
                _entries.Dequeue();
        }
    }

    public async Task<AdscMessageLogEntry[]> All(CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _entries.Reverse().ToArray();
        }
    }

    public async Task RemoveOlderThan(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            // The entries are added in time order, so the oldest entries are at the front
            while (_entries.Count > 0 && _entries.Peek().Time < cutoff)
                _entries.Dequeue();
        }
    }
}
