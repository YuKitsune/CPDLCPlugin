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

// Read-only settings of the ATSU the controller is connected to
public record AtsuSettingsDto(
    string StationId,
    TimeSpan DefaultAdscReportingInterval);

// ADS-C DTOs for SignalR API

public enum AdscContractState
{
    Sent,
    Accepted,
    Rejected
}

// ReportingInterval is the interval of the latest periodic request. It is null when no periodic contract was requested.
// SelectedContractEnds is null when no selected contract is in use.
public record AdscContractDto(
    string Callsign,
    string StationId,
    TimeSpan? ReportingInterval,
    AdscContractState? DefaultContractState,
    TimeSpan? SelectedReportingInterval,
    AdscContractState? SelectedContractState,
    DateTimeOffset? SelectedContractEnds,
    AdscContractState? DemandContractState);

public enum AdscReportSource
{
    Hoppie,
    VATSIM
}

// Timestamp is the time of the position, as reported by the aircraft.
// Received is the time the server received the report.
// Source is the system that supplied the report.
public record AdscReportDto(
    string Callsign,
    string StationId,
    DateTimeOffset Timestamp,
    double Latitude,
    double Longitude,
    int Altitude,
    DateTimeOffset Received,
    AdscReportSource Source);

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


