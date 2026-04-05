namespace CPDLCServer.Model;

public static class ControlMessages
{
    public static bool IsLogonRequest(DownlinkMessage downlinkMessage)
    {
        return downlinkMessage.Content.Contains("REQUEST LOGON");
    }

    public static bool IsLogoffNotice(DownlinkMessage downlinkMessage)
    {
        return downlinkMessage.Content.Contains("LOGOFF");
    }

    public static bool IsEndServiceUplink(UplinkMessage downlinkMessage)
    {
        return downlinkMessage.Content.Contains("END SERVICE");
    }

    public static bool IsNotCurrentDataAuthority(DownlinkMessage downlinkMessage)
    {
        return downlinkMessage.Content.Contains("NOT CURRENT DATA AUTHORITY");
    }
}
