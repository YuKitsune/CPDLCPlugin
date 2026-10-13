using CPDLCServer.Model;
using CPDLCServer.Persistence;

namespace CPDLCServer.Tests.Mocks;

public class TestAdscContractRepository : IAdscContractRepository
{
    private readonly InMemoryAdscContractRepository _inner = new();

    public Task Add(AdscContractKey contractKey, AdscContract contract, CancellationToken cancellationToken)
    {
        return _inner.Add(contractKey, contract, cancellationToken);
    }

    public Task<AdscContract?> Find(AdscContractKey contractKey, CancellationToken cancellationToken)
    {
        return _inner.Find(contractKey, cancellationToken);
    }

    public Task<AdscContract[]> All(CancellationToken cancellationToken)
    {
        return _inner.All(cancellationToken);
    }

    public Task<bool> Remove(AdscContractKey contractKey, CancellationToken cancellationToken)
    {
        return _inner.Remove(contractKey, cancellationToken);
    }
}
