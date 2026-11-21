using System.Text.Json.Serialization;

namespace CPDLCServer.AcarsConnections;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(HoppiesConfiguration), "Hoppie")]
public abstract class AcarsConfiguration
{
    public abstract string AcarsConnectionId { get; }

    // The reporting interval in minutes for the ADS-C contract that is requested when an aircraft logs on with CPDLC
    public int DefaultAdscReportingInterval { get; init; } = 10;

    // A CPDLC free text message that is sent to each aircraft after it logs on. Null when there is no message.
    public string? Motd { get; init; }

    // The time from the logon to the message of the day
    public TimeSpan MotdDelay { get; init; } = TimeSpan.FromSeconds(30);
}

public class HoppiesConfiguration : AcarsConfiguration
{
    public override string AcarsConnectionId => $"Hoppies/{StationIdentifier}";

    public required Uri Url { get; init; }
    public required string AuthenticationCode { get; init; }
    public required string StationIdentifier { get; init; }
}
