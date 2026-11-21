using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
namespace CPDLCServer.Cpdlc.Dialogues;

public static class ControlMessages
{
    public static bool IsLogonRequest(ReceivedDownlink downlink)
    {
        return downlink.Content.Contains("REQUEST LOGON") ||
               downlink.Content.Contains("REQUEST LOG ON");
    }

    public static bool IsLogoffNotice(ReceivedDownlink downlink)
    {
        return downlink.Content.Contains("LOGOFF") ||
               downlink.Content.Contains("LOG OFF");
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
