namespace CPDLCServer.Cpdlc;

/// <summary>
/// The IDs of the uplink elements that the server must find.
/// </summary>
public static class UplinkElementIds
{
    public const int Unable = 0;
    public const int Standby = 1;
    public const int RequestDeferred = 2;
    public const int NextDataAuthority = 160;
    public const int EndService = 161;
    public const int DueToTraffic = 166;
    public const int DueToAirspaceRestriction = 167;
    public const int FreeText = 169;
}

/// <summary>
/// The IDs of the downlink elements that the server must find.
/// </summary>
public static class DownlinkElementIds
{
    public const int Standby = 2;
    public const int Error = 62;
    public const int NotCurrentDataAuthority = 63;
    public const int FreeText = 67;
}
