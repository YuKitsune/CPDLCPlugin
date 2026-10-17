using CPDLCServer.Extensions;
using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public class InMemoryAdscContractRepository : IAdscContractRepository
{
    readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly Dictionary<AdscContractKey, AdscContract> _contracts = new();

    public async Task Add(AdscContractKey contractKey, AdscContract contract, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _contracts[contractKey] = contract;
        }
    }

    public async Task<AdscContract?> Find(AdscContractKey contractKey, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _contracts.TryGetValue(contractKey, out var contract);
            return contract;
        }
    }

    public async Task<AdscContract[]> All(CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _contracts
                .Select(kvp => kvp.Value)
                .ToArray();
        }
    }

    public async Task<bool> Remove(AdscContractKey contractKey, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _contracts.Remove(contractKey);
        }
    }
}
