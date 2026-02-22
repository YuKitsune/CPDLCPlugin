namespace CPDLCServer.Model;

public static class ControlMessages
{
    public static bool IsLogonRequest(DownlinkMessage downlinkMessage)
    {
        return downlinkMessage.Content == "REQUEST LOGON";
    }

    public static bool IsLogoffNotice(DownlinkMessage downlinkMessage)
    {
        return downlinkMessage.Content == "LOGOFF";
    }

    public static bool IsEndServiceUplink(UplinkMessage downlinkMessage)
    {
        return downlinkMessage.Content == "END SERVICE";
    }
}
