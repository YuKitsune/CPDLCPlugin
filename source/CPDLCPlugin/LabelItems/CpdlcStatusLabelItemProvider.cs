using vatsys;
using vatsys.Plugin;

namespace CPDLCPlugin.LabelItems;

public class CpdlcStatusLabelItemProvider
{
    readonly object _gate = new();
    IDictionary<string, CustomLabelItem> _items = new Dictionary<string, CustomLabelItem>();

    public CustomLabelItem? GetLabelItem(string itemType, FDP2.FDR? fdr)
    {
        var callsign = fdr?.Callsign;
        if (string.IsNullOrEmpty(callsign))
        {
            if (itemType == "CPDLCPLUGIN_CPDLCSTATUS_BG")
                return null;

            return new CustomLabelItem
            {
                Type = itemType,
                Text = " "
            };
        }

        CustomLabelItem? item;
        lock (_gate)
        {
            _items.TryGetValue(callsign, out item);
        }

        if (item is null)
        {
            if (itemType == "CPDLCPLUGIN_CPDLCSTATUS_BG")
                return null;

            return new CustomLabelItem
            {
                Type = itemType,
                Text = " "
            };
        }

        // vatSys bug: Custom background colours can't be drawn selectively.
        // vatSys won't draw the custom background if the original colour (specified in the Labels.xml file) is transparent (or empty).
        // To work around this, we define two label items. One with the background, and one without.
        // If we need to draw a custom background colour, we return `null` for the one without the background.
        var hasBackgroundColour = item.BackColourIdentity == Colours.Identities.Custom;
        if (hasBackgroundColour && itemType != "CPDLCPLUGIN_CPDLCSTATUS_BG")
            return null;

        if (!hasBackgroundColour && itemType != "CPDLCPLUGIN_CPDLCSTATUS")
            return null;

        return item;
    }

    public CustomStripItem? GetStripItem(string itemType, FDP2.FDR? fdr)
    {
        var callsign = fdr?.Callsign;
        if (string.IsNullOrEmpty(callsign))
            return null;

        CustomLabelItem? labelItem;
        lock (_gate)
        {
            _items.TryGetValue(callsign, out labelItem);
        }

        if (labelItem is null)
            return null;

        // Re-using the label item and ignoring the colour for simplicity
        return new CustomStripItem
        {
            Text = labelItem.Text,
            OnMouseClick = labelItem.OnMouseClick
        };
    }

    internal void Replace(IDictionary<string, CustomLabelItem> newItems)
    {
        lock (_gate)
        {
            _items = newItems;
        }
    }

    internal void Clear()
    {
        lock (_gate)
        {
            _items.Clear();
        }
    }
}
