using System.Text.RegularExpressions;
using CPDLCPlugin.Extensions;
using vatsys;

namespace CPDLCPlugin;

// Replaces the variables in a default parameter value with the values from the FDR of the aircraft.
// The variables are written in CPDLC.yaml, for example beaconCode: "$AssignedSsrCode".
public static class FdrDefaultValues
{
    static readonly Regex VariableRegex = new(@"\$([A-Za-z]+)");

    static readonly Dictionary<string, Func<FDP2.FDR, string>> Variables = new()
    {
        { "Callsign", fdr => fdr.Callsign ?? string.Empty },
        { "AircraftType", fdr => fdr.AircraftType ?? string.Empty },
        { "Departure", fdr => fdr.DepAirport ?? string.Empty },
        { "Destination", fdr => fdr.DesAirport ?? string.Empty },
        { "AssignedSsrCode", AssignedSsrCode },
        { "RequestedLevel", fdr => Level(fdr.RFL) },
        { "ClearedLevel", ClearedLevel },
        { "NextWaypoint", fdr => fdr.GetNextWaypoints(1).FirstOrDefault() ?? string.Empty }
    };

    // A variable is empty if the FDR is not found, or if the FDR has no value for it.
    // A name that is not a variable stays in the value.
    public static string Resolve(string value, string callsign)
    {
        if (!VariableRegex.IsMatch(value))
            return value;

        var fdr = FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == callsign);

        return VariableRegex.Replace(value, match =>
        {
            if (!Variables.TryGetValue(match.Groups[1].Value, out var getValue))
                return match.Value;

            return fdr is null ? string.Empty : getValue(fdr);
        });
    }

    static string AssignedSsrCode(FDP2.FDR fdr)
    {
        return fdr.AssignedSSRCode == -1
            ? string.Empty
            : Convert.ToString(fdr.AssignedSSRCode, 8).PadLeft(4, '0');
    }

    // A block level has no single value, so it is empty
    static string ClearedLevel(FDP2.FDR fdr)
    {
        return fdr.TryGetClearedLevel(out var feet)
            ? Level(feet)
            : string.Empty;
    }

    // The special values (no level, 000 and VSA) and a level that is not set are empty
    static string Level(int feet)
    {
        return feet <= FDP2.FDR.LEVEL_VSA
            ? string.Empty
            : Levels.FormatHundredsOfFeet(feet);
    }
}
