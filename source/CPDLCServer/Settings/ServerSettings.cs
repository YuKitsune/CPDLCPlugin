using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CPDLCServer.Settings;

/// <summary>
/// Settings that can be changed while the server is running.
/// </summary>
public record ServerSettings
{
    public const string DefaultServerName = "CPDLC Server";
    public const int MaxServerNameLength = 64;
    public const int MaxMotdLength = 1000;
    public const int MaxOperatorNameLength = 128;
    public const int MaxPrivacyContactLength = 256;

    // The person or organisation that operates the server. The privacy policy shows it, and the setup sets it.
    [Required]
    [StringLength(MaxOperatorNameLength)]
    public string OperatorName { get; init; } = string.Empty;

    // An email address or a URL, for questions about the privacy policy and requests to delete data
    [Required]
    [StringLength(MaxPrivacyContactLength)]
    [PrivacyContact]
    public string PrivacyContact { get; init; } = string.Empty;

    // Branding: the dashboard shows the name, the banner and the message of the day at the top of the sidebar and on
    // the login page.
    [Required]
    [StringLength(MaxServerNameLength)]
    public string ServerName { get; init; } = DefaultServerName;

    // Plain text. The dashboard keeps the line breaks.
    [StringLength(MaxMotdLength)]
    public string? Motd { get; init; }

    // The banner is changed with SetBannerImageCommand, not with the settings form
    [BindNever]
    [ValidateNever]
    public byte[]? BannerImage { get; set; }

    [BindNever]
    [ValidateNever]
    public string? BannerContentType { get; set; }

    // Increases each time that the banner changes, so that the browser does not show an old banner from its cache
    [BindNever]
    [ValidateNever]
    public int BannerVersion { get; set; }

    [Range(1, int.MaxValue)]
    public int ConnectivityCheckIntervalMinutes { get; init; } = 15;

    [Range(1, int.MaxValue)]
    public int AircraftConnectionTimeoutMinutes { get; init; } = 45;

    [Range(1, int.MaxValue)]
    public int AdscContractTimeoutMinutes { get; init; } = 120;

    [Range(1, int.MaxValue)]
    public int DialogueRetentionHours { get; init; } = 24;

    [Range(0, int.MaxValue)]
    public int HandoffNotificationLeadTimeMinutes { get; init; } = 20;

    public bool VatsimStatusEnabled { get; init; }

    [Required]
    public Uri VatsimStatusUrl { get; init; } = new("https://data.vatsim.net/v3/vatsim-data.json");

    [Range(1, int.MaxValue)]
    public int VatsimStatusPollingIntervalSeconds { get; init; } = 120;

    public bool VatsimStatusAllowNonEquippedAircraft { get; init; }

    [Range(0, int.MaxValue)]
    public int VatsimStatusReportDelayMinutes { get; init; } = 5;
}
