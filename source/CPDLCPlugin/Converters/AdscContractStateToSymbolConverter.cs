using System.Globalization;
using System.Windows.Data;
using CPDLCServer.Contracts;

namespace CPDLCPlugin.Converters;

[ValueConversion(typeof(AdscContractState), typeof(string))]
public class AdscContractStateToSymbolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            AdscContractState.Sent => "→",
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
