using vatsys;
using vatsys.Plugin;

namespace CPDLCPlugin.LabelItems;

public static class LabelItemConverter
{
    /// <summary>
    ///     Converts a <see cref="CustomStripOrLabelItem"/> to a vatSys <see cref="CustomLabelItem"/>.
    /// </summary>
    /// <remarks>
    ///     vatSys bug workaround: Custom background colours can't be drawn selectively.
    ///     vatSys won't draw the custom background if the original colour (specified in the Labels.xml file) is transparent.
    ///     To work around this, we define two label items - one with background (_BG suffix), and one without.
    ///     This method returns null for the non-matching variant based on whether a background colour is needed.
    /// </remarks>
    public static CustomLabelItem? ToVatSysLabelItem(
        CustomStripOrLabelItem? item,
        string itemType,
        string itemTypePrefix)
    {
        var isBgVariant = itemType.EndsWith("_BG");
        var hasBackgroundColour = item?.BackgroundColour is not null;

        // Return null for the wrong variant
        if (hasBackgroundColour && !isBgVariant)
            return null;

        if (!hasBackgroundColour && isBgVariant)
            return null;

        // Build the label item
        var labelItem = new CustomLabelItem
        {
            Type = itemType,
            Text = item?.Text ?? " ",
            Border = string.IsNullOrWhiteSpace(item?.Text)
                ? BorderFlags.None
                : BorderFlags.All,
        };

        if (item?.BackgroundColour is not null)
        {
            labelItem.BackColourIdentity = Colours.Identities.Custom;
            labelItem.CustomBackColour = item.BackgroundColour;
        }

        if (item?.ForegroundColour is not null)
        {
            labelItem.ForeColourIdentity = Colours.Identities.Custom;
            labelItem.CustomForeColour = item.ForegroundColour;
        }

        return labelItem;
    }
}
