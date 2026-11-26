using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
namespace CPDLCServer.Cpdlc.Dialogues;

public static class ControlMessages
{
    public static bool IsEndServiceUplink(UplinkMessage uplink)
    {
        return uplink.HasElement(UplinkElementIds.EndService);
    }

    public static bool IsNotCurrentDataAuthority(ReceivedDownlink downlink)
    {
        return downlink.Elements.Any(e => e.Id == DownlinkElementIds.NotCurrentDataAuthority);
    }
}
