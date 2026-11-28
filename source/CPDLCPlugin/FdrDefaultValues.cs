using System.Text.RegularExpressions;

namespace CPDLCPlugin;

// Replaces the variables in a default parameter value with the values from the flight data of the aircraft.
// The variables are written in CPDLC.yaml, for example beaconCode: "$AssignedSsrCode".
public static class FdrDefaultValues
{
    static readonly Regex VariableRegex = new(@"\$([A-Za-z]+)");

    static readonly Dictionary<string, Func<FlightData, string>> Variables = new()
    {
        { "Callsign", flightData => flightData.Callsign },
        { "AircraftType", flightData => flightData.AircraftType },
        { "Departure", flightData => flightData.Departure },
        { "Destination", flightData => flightData.Destination },
        { "AssignedSsrCode", flightData => flightData.AssignedSsrCode ?? string.Empty },
        { "RequestedLevel", flightData => Level(flightData.RequestedLevel) },
        { "ClearedLevel", flightData => Level(flightData.ClearedLevel) },
        { "NextWaypoint", flightData => flightData.NextWaypoints.FirstOrDefault() ?? string.Empty }
    };

    // A variable is empty if there is no flight data, or if the flight data has no value for it.
    // A name that is not a variable stays in the value.
    public static string Resolve(string value, FlightData? flightData)
    {
        return VariableRegex.Replace(value, match =>
        {
            if (!Variables.TryGetValue(match.Groups[1].Value, out var getValue))
                return match.Value;

            return flightData is null ? string.Empty : getValue(flightData);
        });
    }

    static string Level(int? feet)
    {
        return feet is null ? string.Empty : Levels.FormatHundredsOfFeet(feet.Value);
    }
}
