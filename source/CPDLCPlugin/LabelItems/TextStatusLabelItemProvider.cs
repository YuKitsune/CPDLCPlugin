using vatsys;

namespace CPDLCPlugin.LabelItems;

public class TextStatusLabelItemProvider(ColourCache colourCache) : ILabelItemProvider
{
    public string ItemTypePrefix => "CPDLCPLUGIN_TEXTSTATUS";

    public CustomStripOrLabelItem? GetLabelItem(FDP2.FDR flightDataRecord)
    {
        var radioMessages = Network.GetRadioMessages;
        var lastTextMessage = radioMessages?
            .LastOrDefault(r => r.Address == flightDataRecord.Callsign && !r.Acknowledged);

        var text = " ";
        var backgroundColour = lastTextMessage is not null
            ? colourCache.DownlinkBackgroundColour
            : null;

        if (flightDataRecord.TextOnly)
        {
            text = "T";
        }
        else if (flightDataRecord.ReceiveOnly)
        {
            text = "R";
        }
        else if (lastTextMessage is not null)
        {
            text = "V";
        }

        return new CustomStripOrLabelItem(
            text,
            backgroundColour,
            ForegroundColour: null,
            () => HandleClick(flightDataRecord, lastTextMessage));
    }

    static void HandleClick(FDP2.FDR flightDataRecord, NetworkMessage? lastTextMessage)
    {
        if (lastTextMessage is not null)
        {
            MMI.OpenCPDLCMenu(lastTextMessage);
        }
        else
        {
            MMI.OpenCPDLCWindow(flightDataRecord);
        }
    }
}
