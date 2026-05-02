namespace CPDLCServer.Persistence;

public interface IAcarsStationRepository
{
    Task<string[]> All(CancellationToken cancellationToken);
    Task<bool> Update(string[] callsigns, CancellationToken cancellationToken);
}
