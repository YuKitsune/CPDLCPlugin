namespace CPDLCPlugin;

/// <summary>
/// The IDs of the uplink elements that the plugin must find.
/// </summary>
public static class UplinkElementIds
{
    public const int Unable = 0;
    public const int Standby = 1;
    public const int RequestDeferred = 2;
    public const int EndService = 161;
    public const int DueToTraffic = 166;
    public const int DueToAirspaceRestriction = 167;
    public const int FreeText = 169;
    public const int DistressFreeText = 170;
}

/// <summary>
/// The IDs of the downlink elements that the plugin must find.
/// </summary>
public static class DownlinkElementIds
{
    public const int Wilco = 0;
    public const int Unable = 1;
    public const int Error = 62;
    public const int FreeText = 67;
    public const int DistressFreeText = 68;
}
