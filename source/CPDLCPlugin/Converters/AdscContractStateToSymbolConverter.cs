using System.Globalization;
using System.Windows.Data;
using CPDLCServer.Contracts;

namespace CPDLCPlugin.Converters;

// Values: the AdscContractState, then true when the server made the latest report from the VATSIM status data
public class AdscContractStateToSymbolConverter : IMultiValueConverter
{
    public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isUsingVatsimData = values.Length > 1 && values[1] is true;

        return values[0] switch
        {
            AdscContractState.Sent => "→",
            AdscContractState.Accepted when isUsingVatsimData => "★",
            AdscContractState.Accepted => "✓",
            AdscContractState.Rejected => "×",
            _ => string.Empty
        };
    }

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
