namespace CPDLCServer.Model;

// A pilot from the VATSIM status data. Altitude is in feet.
public record VatsimPilot(
    string Callsign,
    double Latitude,
    double Longitude,
    int Altitude,
    DateTimeOffset LastUpdated,
    bool IsAdscEquipped)
{
    /// <summary>
    /// Finds the ADS-C capability in the ICAO aircraft field of the flight plan, for example "B77W/H-SDE1E3FGHIJ2J3J5M1RWXY/LB1D1".
    /// The surveillance equipment (Item 10b) is after the second '/'. D1 is ADS-C with FANS 1/A. G1 is ADS-C with ATN.
    /// </summary>
    public static bool HasAdscEquipment(string? aircraft)
    {
        if (string.IsNullOrWhiteSpace(aircraft))
            return false;

        var parts = aircraft.Split('/');
        if (parts.Length < 3)
            return false;

        var surveillanceEquipment = parts[2];
        return surveillanceEquipment.Contains("D1") || surveillanceEquipment.Contains("G1");
    }
}
