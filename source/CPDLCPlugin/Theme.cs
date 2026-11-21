using System.Windows.Media;
using CPDLCPlugin.Extensions;

namespace CPDLCPlugin;

// The CPDLC colours that the vatSys profile does not have.
// For the colours in the profile, use VatSysColours or the VatSysColour markup extension.
public static class Theme
{
    public static SolidColorBrush CPDLCSelectedDownlinkBackgroundColor { get; set; } = CreateColor(255, 220, 220, 220);
    public static SolidColorBrush CPDLCClosedColor { get; set; } = CreateColor(255, 000, 000, 000);
    public static SolidColorBrush CPDLCControllerLateColor { get; set; } = CreateColor(255, 170, 255, 170);
    public static SolidColorBrush CPDLCPilotLateColor { get; set; } = CreateColor(255, 000, 255 ,255);
    public static SolidColorBrush CPDLCUrgentColor { get; set; } = CreateColor(255, 209, 046, 046);
    public static SolidColorBrush CPDLCFailedColor { get; set; } = CreateColor(255, 255, 255, 0);

    public static SolidColorBrush CPDLCUnableDownlinkColor { get; set; } = CreateColor(255, 230, 127, 127);
    public static SolidColorBrush CPDLCSuspendedColor { get; set; } = CreateColor(255, 255, 255, 255);

    public static SolidColorBrush SelectedUtilityColor { get; set; } = CreateColor(255, 0, 255, 255);

    static SolidColorBrush CreateColor(int alpha, int red, int green, int blue)
    {
        return new SolidColorBrush(System.Drawing.Color.FromArgb(alpha, red, green, blue).ToWindowsColor());
    }
}
