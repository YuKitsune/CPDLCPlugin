using System.Text.Json.Serialization;

namespace CPDLCServer.Contracts;

public enum CpdlcDownlinkResponseType
{
    NoResponse,
    ResponseRequired
}

public enum CpdlcUplinkResponseType
{
    NoResponse,
    WilcoUnable,
    AffirmativeNegative,
    Roger
}

public record AircraftConnectionDto(
    string Callsign,
    string StationId,
    DataAuthorityState DataAuthorityState);

public enum DataAuthorityState
{
    NextDataAuthority,
    CurrentDataAuthority
}

public enum AlertType
{
    High,
    Medium,
    Low,
    None
}

public record ControllerConnectionDto(string StationId, string Callsign, string VatsimCid);

// Dialogue DTOs for SignalR API

public record DialogueDto(
    Guid Id,
    string AircraftCallsign,
    IReadOnlyList<CpdlcMessageDto> Messages,
    DateTimeOffset Opened,
    DateTimeOffset? Closed,
    DateTimeOffset? Archived)
{
    [JsonIgnore]
    public bool IsClosed => Closed.HasValue;

    [JsonIgnore]
    public bool IsArchived => Archived.HasValue;
}

[JsonDerivedType(typeof(UplinkMessageDto), "uplink")]
[JsonDerivedType(typeof(DownlinkMessageDto), "downlink")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
public abstract class CpdlcMessageDto
{
    public required Guid DialogueId { get; init; }
    public required int MessageId { get; init; }
    public int? MessageReference { get; init; }
    public required AlertType AlertType { get; init; }
    public abstract DateTimeOffset Time { get; }
    public DateTimeOffset? Closed { get; init; }
    public DateTimeOffset? Acknowledged { get; init; }

    [JsonIgnore]
    public bool IsClosed => Closed.HasValue;

    [JsonIgnore]
    public bool IsAcknowledged => Acknowledged.HasValue;
}

public class UplinkMessageDto : CpdlcMessageDto
{
    public override DateTimeOffset Time => Sent;
    public required string Recipient { get; init; }
    public required string SenderCallsign { get; init; }
    public required CpdlcUplinkResponseType ResponseType { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset Sent { get; init; }
    public required bool IsClosedManually { get; init; }
    public required bool IsPilotLate { get; init; }
    public required bool IsTransmissionFailed { get; init; }

    [JsonIgnore]
    public bool IsSpecial => Content is "STANDBY" or "REQUEST DEFERRED";
}

public class DownlinkMessageDto : CpdlcMessageDto
{
    public override DateTimeOffset Time => Received;
    public required string Sender { get; init; }
    public required CpdlcDownlinkResponseType ResponseType { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset Received { get; init; }
    public required bool IsControllerLate { get; init; }

    [JsonIgnore]
    public bool IsSpecial => Content is "STANDBY";
}


