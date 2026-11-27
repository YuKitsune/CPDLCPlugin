using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CPDLCPlugin.ViewModels;
using vatsys;
using vatSysToolbox;

namespace CPDLCPlugin.Converters;

// Changes a message colour to the brush from the vatSys profile, or to the brush from the Theme
[ValueConversion(typeof(MessageColour), typeof(SolidColorBrush))]
public class MessageColourToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MessageColour colour)
            throw new NotSupportedException();

        return colour switch
        {
            MessageColour.MessageBackground => VatSysColours.GetColour(Colours.Identities.CPDLCMessageBackground),
            MessageColour.Closed => Theme.CPDLCClosedColor,
            MessageColour.Urgent => Theme.CPDLCUrgentColor,
            MessageColour.Failed => Theme.CPDLCFailedColor,
            MessageColour.PilotLate => Theme.CPDLCPilotLateColor,
            MessageColour.ControllerLate => Theme.CPDLCControllerLateColor,
            MessageColour.Downlink => VatSysColours.GetColour(Colours.Identities.CPDLCDownlink),
            MessageColour.Uplink => VatSysColours.GetColour(Colours.Identities.CPDLCUplink),
            _ => throw new NotSupportedException()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
