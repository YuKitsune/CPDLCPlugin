using CPDLCServer.Model;
using CPDLCServer.Persistence;

namespace CPDLCServer.Tests.Mocks;

public class TestAdscMessageLogRepository : IAdscMessageLogRepository
{
    private readonly InMemoryAdscMessageLogRepository _inner = new();

    public Task Add(AdscMessageLogEntry entry, CancellationToken cancellationToken)
    {
        return _inner.Add(entry, cancellationToken);
    }

    public Task<AdscMessageLogEntry[]> All(CancellationToken cancellationToken)
    {
        return _inner.All(cancellationToken);
    }

    public Task RemoveOlderThan(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        return _inner.RemoveOlderThan(cutoff, cancellationToken);
    }
}
