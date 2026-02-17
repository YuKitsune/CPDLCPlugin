using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public interface IAircraftRepository
{
    Task Add(AircraftKey aircraftKey, AircraftConnection connection, CancellationToken cancellationToken);
    Task<AircraftConnection?> Find(AircraftKey aircraftKey, CancellationToken cancellationToken);
    Task<AircraftConnection[]> All(CancellationToken cancellationToken);
    Task<bool> Remove(AircraftKey aircraftKey, CancellationToken cancellationToken);
}

public record AircraftKey(string Callsign, string AcarsClientId);
