using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
namespace CPDLCServer.Cpdlc.Dialogues;

public static class ControlMessages
{
    public static bool IsEndServiceUplink(UplinkMessage uplink)
    {
        return uplink.HasElement(UplinkElementIds.EndService);
    }

    // An END SERVICE with other elements that need a response ends the connection only after the response. The ACARS
    // connection removes the END SERVICE element, and sends the logoff after the response, so the logoff needs its own
    // message ID. An END SERVICE alone, or with elements that need no response, ends the connection immediately.
    public static bool NeedsLogoffAfterResponse(UplinkMessage uplink)
    {
        return IsEndServiceUplink(uplink) &&
               uplink.Elements.Count > 1 &&
               uplink.ResponseType != CpdlcUplinkResponseType.NoResponse;
    }

    public static bool IsNotCurrentDataAuthority(ReceivedDownlink downlink)
    {
        return downlink.Elements.Any(e => e.Id == DownlinkElementIds.NotCurrentDataAuthority);
    }
}
