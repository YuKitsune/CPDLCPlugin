using CPDLCPlugin.Extensions;
using vatsys;

namespace CPDLCPlugin;

// The flight data of an aircraft, from its FDR. The view models use it, so that they do not use the vatSys types.
// The levels are in feet. A level is null if the FDR has no value for it, and the cleared level is null for a block level.
// The assigned SSR code has four octal digits, for example "4021". It is null if no code is assigned.
public record FlightData(
    string Callsign,
    string AircraftType,
    string Departure,
    string Destination,
    string? AssignedSsrCode,
    int? RequestedLevel,
    int? ClearedLevel,
    string[] NextWaypoints);

public interface IFlightDataProvider
{
    // Returns null if there is no FDR for the callsign
    FlightData? Find(string callsign);
}

public class VatSysFlightDataProvider : IFlightDataProvider
{
    public FlightData? Find(string callsign)
    {
        var fdr = FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == callsign);
        if (fdr is null)
            return null;

        return new FlightData(
            fdr.Callsign ?? string.Empty,
            fdr.AircraftType ?? string.Empty,
            fdr.DepAirport ?? string.Empty,
            fdr.DesAirport ?? string.Empty,
            fdr.AssignedSSRCode == -1 ? null : Convert.ToString(fdr.AssignedSSRCode, 8).PadLeft(4, '0'),
            // The special values (no level, 000 and VSA) are not levels
            fdr.RFL > FDP2.FDR.LEVEL_VSA ? fdr.RFL : null,
            fdr.TryGetClearedLevel(out var clearedLevel) ? clearedLevel : null,
            fdr.GetNextWaypoints());
    }
}
