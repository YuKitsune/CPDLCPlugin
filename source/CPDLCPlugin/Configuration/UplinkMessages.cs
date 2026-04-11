using System.Text.Json.Serialization;

namespace CPDLCPlugin.Configuration;

public enum ParameterType
{
    Altimeter,
    AtisCode,
    Code,
    Degree,
    Direction,
    DistanceOffset,
    FacilityDesignation,
    FreeText,
    Frequency,
    LegType,
    Level,
    Position,
    PreDepartureClearance,
    ProcedureName,
    RouteClearance,
    Speed,
    Time,
    ToFrom,
    UnitName,
    VerticalRate
}

public class UplinkMessageParameter
{
    public required string Name { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ParameterType Type { get; init; }
}

/// <summary>
/// Master uplink message definition
/// </summary>
public class UplinkMessageTemplate
{
    public required int Id { get; init; }
    public required string Template { get; init; }
    public required UplinkMessageParameter[] Parameters { get; init; } = [];

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required UplinkResponseType ResponseType { get; init; }
}

/// <summary>
/// Reference to an uplink message with optional default parameter values and response type override
/// </summary>
public class UplinkMessageReference
{
    public required int MessageId { get; init; }
    public Dictionary<string, string>? DefaultParameters { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UplinkResponseType? ResponseType { get; init; }
}

/// <summary>
/// Group/category of uplink messages
/// </summary>
public class UplinkMessageGroup
{
    public required string Name { get; init; }
    public required UplinkMessageReference[] Messages { get; init; } = [];
}

/// <summary>
/// Root configuration for uplink messages
/// </summary>
public class UplinkMessagesConfiguration
{
    public required UplinkMessageTemplate[] MasterMessages { get; init; } = [];
    public required UplinkMessageReference[] QuickAccessMessages { get; init; } = [];
    public required UplinkMessageGroup[] Groups { get; init; } = [];
}

public enum UplinkResponseType
{
    NoResponse,
    WilcoUnable,
    AffirmativeNegative,
    Roger
}
