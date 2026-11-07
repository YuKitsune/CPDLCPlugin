namespace CPDLCServer.AdscContracts.Reports;

// An ADS-C report from an aircraft, and the ATSU that received it
public record AircraftAdscReport(
    string Callsign,
    string StationId,
    AdscReport Report,
    DateTimeOffset Received)
{
    // EF Core uses this constructor, because it cannot give the Report to the primary constructor
    private AircraftAdscReport() : this(null!, null!, null!, default)
    {
    }
}
