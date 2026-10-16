namespace CPDLCServer.Model;

public enum AdscMessageDirection
{
    Uplink,
    Downlink
}

// An ADS-C packet as it was sent to or received from the ACARS network
public record AdscMessageLogEntry(
    DateTimeOffset Time,
    string AcarsClientId,
    string StationId,
    string Callsign,
    AdscMessageDirection Direction,
    string Packet);
