using System.Text.RegularExpressions;
using CPDLCPlugin.Extensions;
using vatsys;

namespace CPDLCPlugin;

// Replaces the variables in a default parameter value with the values from the FDR of the aircraft.
// The variables are written in CPDLC.yaml, for example beaconCode: "$AssignedSsrCode".
public static class FdrDefaultValues
{
    static readonly Regex VariableRegex = new(@"\$([A-Za-z]+)");

    // The int is the transition level in feet
    static readonly Dictionary<string, Func<FDP2.FDR, int, string>> Variables = new()
    {
        { "Callsign", (fdr, _) => fdr.Callsign ?? string.Empty },
        { "AircraftType", (fdr, _) => fdr.AircraftType ?? string.Empty },
        { "Departure", (fdr, _) => fdr.DepAirport ?? string.Empty },
        { "Destination", (fdr, _) => fdr.DesAirport ?? string.Empty },
        { "AssignedSsrCode", (fdr, _) => AssignedSsrCode(fdr) },
        { "RequestedLevel", (fdr, transitionLevel) => Level(fdr.RFL, transitionLevel) },
        { "ClearedLevel", ClearedLevel },
        { "NextWaypoint", (fdr, _) => fdr.GetNextWaypoints(1).FirstOrDefault() ?? string.Empty }
    };

    // A variable is empty if the FDR is not found, or if the FDR has no value for it.
    // A name that is not a variable stays in the value.
    public static string Resolve(string value, string callsign, int transitionLevel)
    {
        if (!VariableRegex.IsMatch(value))
            return value;

        var fdr = FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == callsign);

        return VariableRegex.Replace(value, match =>
        {
            if (!Variables.TryGetValue(match.Groups[1].Value, out var getValue))
                return match.Value;

            return fdr is null ? string.Empty : getValue(fdr, transitionLevel);
        });
    }

    static string AssignedSsrCode(FDP2.FDR fdr)
    {
        return fdr.AssignedSSRCode == -1
            ? string.Empty
            : Convert.ToString(fdr.AssignedSSRCode, 8).PadLeft(4, '0');
    }

    // A block level has no single value, so it is empty
    static string ClearedLevel(FDP2.FDR fdr, int transitionLevel)
    {
        return fdr.CFLLower == FDP2.FDR.LEVEL_NONE
            ? Level(fdr.CFLUpper, transitionLevel)
            : string.Empty;
    }

    // A level below the transition level is an altitude, for example 9000 is "A090".
    // A level at or above the transition level is a flight level, for example 35000 is "FL350".
    // The special values (no level, 000 and VSA) and a level that is not set are empty.
    static string Level(int feet, int transitionLevel)
    {
        if (feet <= FDP2.FDR.LEVEL_VSA)
            return string.Empty;

        var prefix = feet < transitionLevel ? "A" : "FL";
        return $"{prefix}{feet / 100:000}";
    }
}
