using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CPDLCServer.AcarsConnections.ConnectionSettings;

public enum AcarsNetworkType
{
    Hoppie
}

// The settings of an ACARS connection. The initial values are the default values.
// The settings page binds its form to this type, so the form cannot set the ID or the encrypted authentication code.
public class AcarsConnectionSettings
{
    [BindNever]
    public Guid Id { get; init; } = Guid.NewGuid();

    public AcarsNetworkType Type { get; set; } = AcarsNetworkType.Hoppie;

    // The server starts only the enabled connections
    public bool Enabled { get; set; } = true;

    [Required]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Use only letters and digits.")]
    public string StationIdentifier { get; set; } = string.Empty;

    [Required]
    public string FlightSimulationNetwork { get; set; } = "VATSIM";

    [Required]
    public Uri Url { get; set; } = new("http://www.hoppie.nl/acars/system/connect.html");

    // Encrypted with the master key. Use MasterKeyEncryption to decrypt it. The plain code is not kept.
    [BindNever]
    [ValidateNever]
    public string EncryptedAuthenticationCode { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DefaultAdscReportingInterval { get; set; } = 10;

    [Range(1, int.MaxValue)]
    public int SelectedAdscContractDuration { get; set; } = 60;

    // Free text for the users. The server does not use it.
    [StringLength(2000)]
    public string? Notes { get; set; }
}
