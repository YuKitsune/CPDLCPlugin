using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

// Holds the latest ADS-C report for each aircraft
public interface IAdscReportRepository
{
    // Stores the report, unless the repository has a newer report for the same aircraft.
    // A report from Hoppie always replaces a report from VATSIM.
    Task Add(AircraftAdscReport report, CancellationToken cancellationToken);
    Task<AircraftAdscReport?> Find(string callsign, CancellationToken cancellationToken);
    Task<AircraftAdscReport[]> All(CancellationToken cancellationToken);
}
