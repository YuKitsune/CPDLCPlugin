using System.Text.Json.Serialization;

namespace CPDLCServer.Clients;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(HoppiesConfiguration), "Hoppie")]
public abstract class AcarsConfiguration
{
    public abstract string ClientId { get; }

    // The reporting interval in minutes for the ADS-C contract that is requested when an aircraft logs on with CPDLC
    public int DefaultAdscReportingInterval { get; init; } = 10;
}

public class HoppiesConfiguration : AcarsConfiguration
{
    public override string ClientId => $"Hoppies/{StationIdentifier}";

    public required Uri Url { get; init; }
    public required string AuthenticationCode { get; init; }
    public required string FlightSimulationNetwork { get; init; }
    public required string StationIdentifier { get; init; }
}
