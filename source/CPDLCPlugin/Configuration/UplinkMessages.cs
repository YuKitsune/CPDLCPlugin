namespace CPDLCPlugin.Configuration;

/// <summary>
/// Reference to an uplink element with optional default parameter values
/// </summary>
public class UplinkMessageReference
{
    // The FANS 1/A uplink element, for example "UM20"
    public required string Element { get; init; }

    // The key is the name of the parameter in the element, for example "altitude"
    public Dictionary<string, string>? DefaultParameters { get; init; }

    public int ElementId => ElementReference.ParseUplink(Element);
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
    public required UplinkMessageReference[] QuickAccessMessages { get; init; } = [];
    public required UplinkMessageGroup[] Groups { get; init; } = [];

    // The values that the controller can select for an [icao unit name] parameter
    public string[] IcaoUnitNames { get; init; } = [];

    // The transition level in feet. The level variables below this level are altitudes, for example "A090".
    public int TransitionLevel { get; init; } = 11000;
}
