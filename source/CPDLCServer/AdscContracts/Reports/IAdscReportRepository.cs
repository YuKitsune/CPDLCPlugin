namespace CPDLCServer.AdscContracts.Reports;

// Holds one report for each ADS-C contract (aircraft and ATSU)
public interface IAdscReportRepository
{
    // Adds the report. The report replaces the previous report of the same aircraft and ATSU.
    Task Add(AircraftAdscReport report, CancellationToken cancellationToken);
    Task<AircraftAdscReport?> Find(string callsign, string stationId, CancellationToken cancellationToken);

    // The latest report of an aircraft, from all the ATSUs.
    // A report from Hoppie is used before a report from VATSIM. Then the report with the latest timestamp is used.
    Task<AircraftAdscReport?> FindLatest(string callsign, CancellationToken cancellationToken);
    Task<AircraftAdscReport[]> AllLatest(CancellationToken cancellationToken);

    Task<AircraftAdscReport[]> All(CancellationToken cancellationToken);
    Task Remove(AircraftAdscReport report, CancellationToken cancellationToken);
    Task RemoveOlderThan(DateTimeOffset cutoff, CancellationToken cancellationToken);
}
