namespace CPDLC.Messages;

/// <summary>
/// The response that the pilot must send to an uplink element.
/// </summary>
public enum UplinkResponseAttribute
{
    /// <summary>
    /// FANS 1/A "NE". The pilot cannot send WILCO, UNABLE, AFFIRM, NEGATIVE or ROGER.
    /// Some elements, for example CONFIRM ALTITUDE, need a specific downlink as the response.
    /// </summary>
    NoResponse,

    /// <summary>FANS 1/A "W/U".</summary>
    WilcoUnable,

    /// <summary>FANS 1/A "A/N".</summary>
    AffirmativeNegative,

    /// <summary>FANS 1/A "R".</summary>
    Roger
}

/// <summary>
/// Tells if the controller must send a response to a downlink element.
/// </summary>
public enum DownlinkResponseAttribute
{
    /// <summary>FANS 1/A "N".</summary>
    NoResponse,

    /// <summary>FANS 1/A "Y".</summary>
    ResponseRequired
}
