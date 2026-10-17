using System.Globalization;
using System.Windows.Data;
using CPDLCServer.Contracts;

namespace CPDLCPlugin.Converters;

[ValueConversion(typeof(AdscTransmissionState), typeof(string))]
public class AdscContractStateToSymbolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AdscTransmissionState transmissionState)
            return string.Empty;

        return transmissionState.State switch
        {
            AdscContractState.Sent => "→",
            AdscContractState.Accepted when transmissionState.IsUsingVatsimData => "★",
            AdscContractState.Accepted => "✓",
            AdscContractState.Rejected => "×",
            _ => string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
