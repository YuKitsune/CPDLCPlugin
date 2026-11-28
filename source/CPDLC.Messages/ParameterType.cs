namespace CPDLC.Messages;

/// <summary>
/// The FANS 1/A parameter types, as RTCA DO-258A defines them.
/// </summary>
public enum ParameterType
{
    /// <summary>A flight level, or an altitude in feet or metres.</summary>
    Altitude,

    /// <summary>A UTC time in hours and minutes.</summary>
    Time,

    /// <summary>A fix, a navaid, an airport, a latitude and longitude, or a place-bearing-distance.</summary>
    Position,

    /// <summary>An indicated airspeed, a ground speed, or a Mach number.</summary>
    Speed,

    /// <summary>A lateral offset distance, in nautical miles or kilometres.</summary>
    DistanceOffset,

    /// <summary>A distance, in nautical miles or kilometres.</summary>
    Distance,

    /// <summary>LEFT, RIGHT, EITHER SIDE, NORTH, SOUTH, EAST, WEST, NORTH EAST, NORTH WEST, SOUTH EAST or SOUTH WEST.</summary>
    Direction,

    /// <summary>A magnetic or true heading or track, in degrees.</summary>
    Degrees,

    /// <summary>The length of a holding leg, as a distance or a time.</summary>
    LegType,

    /// <summary>A route clearance.</summary>
    RouteClearance,

    /// <summary>The name of an arrival, an approach or a departure procedure.</summary>
    ProcedureName,

    /// <summary>The name of an ATS unit and its function, for example "BRISBANE CENTER".</summary>
    IcaoUnitName,

    /// <summary>An HF, VHF, UHF or SATCOM frequency.</summary>
    Frequency,

    /// <summary>A four-digit SSR code.</summary>
    BeaconCode,

    /// <summary>An altimeter setting, in hectopascals or inches of mercury.</summary>
    Altimeter,

    /// <summary>An ATIS letter.</summary>
    AtisCode,

    /// <summary>An error code.</summary>
    ErrorInformation,

    /// <summary>A four-letter ICAO facility designator, for example "YBBB".</summary>
    IcaoFacilityDesignation,

    /// <summary>The TP4 table (labels A or B).</summary>
    Tp4Table,

    /// <summary>Free text.</summary>
    FreeText,

    /// <summary>A vertical rate, in feet or metres per minute.</summary>
    VerticalRate,

    /// <summary>TO or FROM.</summary>
    ToFrom,

    /// <summary>A pre-departure clearance.</summary>
    PredepartureClearance,

    /// <summary>A position report.</summary>
    PositionReport,

    /// <summary>The fuel that remains, as a time in hours and minutes.</summary>
    RemainingFuel,

    /// <summary>The number of persons on board.</summary>
    RemainingSouls,

    /// <summary>The CPDLC version number.</summary>
    VersionNumber
}
