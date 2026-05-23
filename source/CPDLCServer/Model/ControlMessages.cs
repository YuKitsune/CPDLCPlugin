namespace CPDLCServer.Model;

public static class ControlMessages
{
    public static bool IsLogonRequest(ReceivedDownlink downlink)
    {
        return downlink.Content.Contains("REQUEST LOGON");
    }

    public static bool IsLogoffNotice(ReceivedDownlink downlink)
    {
        return downlink.Content.Contains("LOGOFF");
    }

    public static bool IsEndServiceUplink(UplinkMessage uplink)
    {
        return uplink.Content.Contains("END SERVICE");
    }

    public static bool IsNotCurrentDataAuthority(ReceivedDownlink downlink)
    {
        return downlink.Content.Contains("NOT CURRENT DATA AUTHORITY");
    }
}
