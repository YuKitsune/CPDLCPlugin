namespace CPDLCServer.Persistence;

public interface IAcarsConnectedCallsignsRepository
{
    Task<string[]> All(CancellationToken cancellationToken);
    Task<bool> Update(string[] callsigns, CancellationToken cancellationToken);
}
