namespace CPDLCServer.AdscContracts.MessageLog;

public interface IAdscMessageLogRepository
{
    Task Add(AdscMessageLogEntry entry, CancellationToken cancellationToken);

    // Newest entries first
    Task<AdscMessageLogEntry[]> All(CancellationToken cancellationToken);

    Task RemoveOlderThan(DateTimeOffset cutoff, CancellationToken cancellationToken);
}
