using vatsys.Plugin;

namespace CPDLCPlugin.LabelItems;

public record CustomStripOrLabelItem(
    string Text,
    CustomColour? BackgroundColour,
    CustomColour? ForegroundColour,
    Action LeftClickCallback);
