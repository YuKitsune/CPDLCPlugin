using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public interface IAdscContractRepository
{
    Task Add(AdscContractKey contractKey, AdscContract contract, CancellationToken cancellationToken);
    Task<AdscContract?> Find(AdscContractKey contractKey, CancellationToken cancellationToken);
    Task<AdscContract[]> All(CancellationToken cancellationToken);
    Task<bool> Remove(AdscContractKey contractKey, CancellationToken cancellationToken);
}

public record AdscContractKey(string Callsign, string StationId);
