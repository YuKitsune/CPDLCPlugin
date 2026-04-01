using vatsys;
using vatsys.Plugin;

namespace CPDLCPlugin.LabelItems;

public class TextStatusLabelItemProvider(ColourCache colourCache)
{
    public CustomLabelItem? GetLabelItem(string itemType, FDP2.FDR? fdr)
    {
        // vatSys bug: Custom background colours can't be drawn selectively.
        // vatSys won't draw the custom background if the original colour (specified in the Labels.xml file) is transparent (or empty).
        // To work around this, we define two label items. One with the background, and one without.
        // If we need to draw a custom background colour, we return `null` for the one without the background.

        if (fdr is null)
            return null;

        var radioMessages = Network.GetRadioMessages;
        var lastTextMessage = radioMessages?
            .LastOrDefault(r => r.Address == fdr.Callsign && !r.Acknowledged);

        var text = " ";
        var backgroundColour = lastTextMessage is not null
            ? colourCache.DownlinkBackgroundColour
            : null;

        if (fdr.TextOnly)
        {
            text = "T";
        }
        else if (fdr.ReceiveOnly)
        {
            text = "R";
        }
        else if (lastTextMessage is not null)
        {
            // Only show "V" when there is an unacknowledged message
            text = "V";
        }

        // vatSys bug: custom background colours can't be drawn selectively.
        // To work around this, we define two label items. One with the background, and one without.
        if (backgroundColour is not null && itemType != "CPDLCPLUGIN_TEXTSTATUS_BG")
            return null;

        if (backgroundColour is null && itemType != "CPDLCPLUGIN_TEXTSTATUS")
            return null;

        var textLabelItem = new CustomLabelItem
        {
            Type = itemType,
            Text = text,
            Border = string.IsNullOrWhiteSpace(text)
                ? BorderFlags.None
                : BorderFlags.All,
        };

        if (backgroundColour is not null)
        {
            textLabelItem.BackColourIdentity = Colours.Identities.Custom;
            textLabelItem.CustomBackColour = backgroundColour;
        }

        // Left-click to open the CPDLC Menu
        textLabelItem.OnMouseClick = args =>
        {
            try
            {
                if (args.Button != CustomLabelItemMouseButton.Left)
                    return;

                if (lastTextMessage is not null)
                {
                    MMI.OpenCPDLCMenu(lastTextMessage);
                }
                else
                {
                    MMI.OpenCPDLCWindow(fdr);
                }

                args.Handled = true;
            }
            catch (Exception ex)
            {
                Plugin.AddError(ex, "Failed to handle label item click");
            }
        };

        return textLabelItem;
    }

    public CustomStripItem? GetStripItem(string itemType, FDP2.FDR? fdr)
    {
        if (fdr is null)
            return null;

        var radioMessages = Network.GetRadioMessages;
        var lastTextMessage = radioMessages?
            .LastOrDefault(r => r.Address == fdr.Callsign && !r.Acknowledged);

        var text = " ";

        if (fdr.TextOnly)
        {
            text = "T";
        }
        else if (fdr.ReceiveOnly)
        {
            text = "R";
        }
        else if (lastTextMessage is not null)
        {
            // Only show "V" when there is an unacknowledged message
            text = "V";
        }

        var stripItem = new CustomStripItem
        {
            Text = text,
            Border = string.IsNullOrWhiteSpace(text)
                ? BorderFlags.None
                : BorderFlags.All,
        };

        // Left-click to open the CPDLC Menu
        stripItem.OnMouseClick = args =>
        {
            try
            {
                if (args.Button != CustomLabelItemMouseButton.Left)
                    return;

                if (lastTextMessage is not null)
                {
                    MMI.OpenCPDLCMenu(lastTextMessage);
                }
                else
                {
                    MMI.OpenCPDLCWindow(fdr);
                }

                args.Handled = true;
            }
            catch (Exception ex)
            {
                Plugin.AddError(ex, "Failed to handle strip item click");
            }
        };

        return stripItem;
    }
}
