namespace CPDLCServer.AdscContracts.Reports;

// Holds the history of ADS-C reports, and the latest report for each aircraft
public interface IAdscReportRepository
{
    // Adds the report to the history.
    // The report becomes the latest report, unless the repository has a newer report for the same aircraft.
    // A report from Hoppie always replaces a report from VATSIM as the latest report.
    Task Add(AircraftAdscReport report, CancellationToken cancellationToken);
    Task<AircraftAdscReport?> FindLatest(string callsign, CancellationToken cancellationToken);
    Task<AircraftAdscReport[]> AllLatest(CancellationToken cancellationToken);

    Task RemoveOlderThan(DateTimeOffset cutoff, CancellationToken cancellationToken);
}
