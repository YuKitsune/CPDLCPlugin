using CPDLCServer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.AdscContracts;

// The changes are saved when the unit of work is committed.
// Queries load the rows into the context, then read the local entities, so that the results include entities that are
// added in the unit of work and exclude entities that are removed in it.
public class AdscContractRepository(CpdlcDbContext dbContext) : IAdscContractRepository
{
    public async Task Add(AdscContractKey contractKey, AdscContract contract, CancellationToken cancellationToken)
    {
        // A new contract replaces the contract with the same key
        var existing = await Find(contractKey, cancellationToken);
        if (existing is not null)
            dbContext.AdscContracts.Remove(existing);

        dbContext.AdscContracts.Add(contract);
    }

    public async Task<AdscContract?> Find(AdscContractKey contractKey, CancellationToken cancellationToken)
    {
        await dbContext.AdscContracts
            .Where(c => c.Callsign == contractKey.Callsign && c.StationId == contractKey.StationId)
            .LoadAsync(cancellationToken);

        return dbContext.AdscContracts.Local
            .FirstOrDefault(c => c.Callsign == contractKey.Callsign && c.StationId == contractKey.StationId);
    }

    public async Task<AdscContract[]> All(CancellationToken cancellationToken)
    {
        await dbContext.AdscContracts.LoadAsync(cancellationToken);
        return dbContext.AdscContracts.Local.ToArray();
    }

    public async Task<bool> Remove(AdscContractKey contractKey, CancellationToken cancellationToken)
    {
        var contract = await Find(contractKey, cancellationToken);
        if (contract is null)
            return false;

        dbContext.AdscContracts.Remove(contract);
        return true;
    }
}
