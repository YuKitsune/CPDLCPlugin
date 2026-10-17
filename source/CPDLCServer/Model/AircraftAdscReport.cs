namespace CPDLCServer.Model;

// An ADS-C report from an aircraft, and the ATSU that received it
public record AircraftAdscReport(
    string Callsign,
    string StationId,
    AdscReport Report,
    DateTimeOffset Received);
