using vatsys;

namespace CPDLCPlugin.LabelItems;

public class CpdlcStatusLabelItemProvider(LabelItemCache labelItemCache) : ILabelItemProvider
{
    public string ItemTypePrefix => "CPDLCPLUGIN_CPDLCSTATUS";

    public CustomStripOrLabelItem? GetLabelItem(FDP2.FDR flightDataRecord)
    {
        var callsign = flightDataRecord.Callsign;
        if (string.IsNullOrEmpty(callsign))
            return null;

        return labelItemCache.Find(callsign);
    }
}
