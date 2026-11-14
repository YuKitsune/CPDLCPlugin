using System.ComponentModel.DataAnnotations;

namespace CPDLCServer.Settings;

/// <summary>
/// Settings that can be changed while the server is running.
/// </summary>
public record ServerSettings
{
    [Range(1, int.MaxValue)]
    public int ConnectivityCheckIntervalMinutes { get; init; } = 15;

    [Range(1, int.MaxValue)]
    public int AircraftConnectionTimeoutMinutes { get; init; } = 45;

    [Range(1, int.MaxValue)]
    public int AdscContractTimeoutMinutes { get; init; } = 120;

    [Range(1, int.MaxValue)]
    public int AdscReportRetentionHours { get; init; } = 24;

    [Range(0, int.MaxValue)]
    public int HandoffNotificationLeadTimeMinutes { get; init; } = 20;

    public bool VatsimStatusEnabled { get; init; }

    [Required]
    public Uri VatsimStatusUrl { get; init; } = new("https://data.vatsim.net/v3/vatsim-data.json");

    [Range(1, int.MaxValue)]
    public int VatsimStatusPollingIntervalSeconds { get; init; } = 120;

    public bool VatsimStatusAllowNonEquippedAircraft { get; init; }
}
