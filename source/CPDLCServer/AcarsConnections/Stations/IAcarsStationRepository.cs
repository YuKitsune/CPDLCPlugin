namespace CPDLCServer.AcarsConnections.Stations;

public interface IAcarsStationRepository
{
    Task<string[]> All(CancellationToken cancellationToken);
    Task<bool> Update(string[] callsigns, CancellationToken cancellationToken);
}
