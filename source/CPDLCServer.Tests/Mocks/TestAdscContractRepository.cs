using CPDLCServer.AdscContracts;

namespace CPDLCServer.Tests.Mocks;

public class TestAdscContractRepository : IAdscContractRepository
{
    private readonly Dictionary<AdscContractKey, AdscContract> _contracts = new();

    public Task Add(AdscContractKey contractKey, AdscContract contract, CancellationToken cancellationToken)
    {
        _contracts[contractKey] = contract;
        return Task.CompletedTask;
    }

    public Task<AdscContract?> Find(AdscContractKey contractKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(_contracts.GetValueOrDefault(contractKey));
    }

    public Task<AdscContract[]> All(CancellationToken cancellationToken)
    {
        return Task.FromResult(_contracts.Values.ToArray());
    }

    public Task<bool> Remove(AdscContractKey contractKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(_contracts.Remove(contractKey));
    }
}
