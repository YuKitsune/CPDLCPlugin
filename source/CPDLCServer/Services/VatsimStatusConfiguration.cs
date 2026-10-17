namespace CPDLCServer.Services;

// Settings for the ADS-C reports that the server makes from the VATSIM status data
public class VatsimStatusConfiguration
{
    // When false, the server does not download the VATSIM status data
    public bool Enabled { get; init; }

    public Uri Url { get; init; } = new("https://data.vatsim.net/v3/vatsim-data.json");

    // The time between downloads of the VATSIM status data
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromMinutes(2);

    // When true, the server also makes reports for aircraft that do not have D1 or G1 in Item 10b of the flight plan
    public bool AllowNonEquippedAircraft { get; init; }
}
