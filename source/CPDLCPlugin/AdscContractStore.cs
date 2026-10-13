using CPDLCServer.Contracts;

namespace CPDLCPlugin;

public class AdscContractStore
{
    readonly List<AdscContractDto> _contracts = new();
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Populate(AdscContractDto[] contracts, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _contracts.Clear();
            _contracts.AddRange(contracts);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Upsert(AdscContractDto contractDto, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _contracts.RemoveAll(c => c.Callsign == contractDto.Callsign && c.StationId == contractDto.StationId);
            _contracts.Add(contractDto);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<AdscContractDto>> All(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _contracts.ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> Remove(string callsign, string stationId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _contracts.RemoveAll(c => c.Callsign == callsign && c.StationId == stationId) > 0;
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
            _contracts.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
