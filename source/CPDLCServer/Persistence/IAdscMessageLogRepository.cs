using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public interface IAdscMessageLogRepository
{
    Task Add(AdscMessageLogEntry entry, CancellationToken cancellationToken);

    // Newest entries first
    Task<AdscMessageLogEntry[]> All(CancellationToken cancellationToken);
}
