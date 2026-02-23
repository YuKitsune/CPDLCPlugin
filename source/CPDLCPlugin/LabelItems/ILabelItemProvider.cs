using vatsys;

namespace CPDLCPlugin.LabelItems;

public interface ILabelItemProvider
{
    string ItemTypePrefix { get; }
    CustomStripOrLabelItem? GetLabelItem(FDP2.FDR flightDataRecord);
}
