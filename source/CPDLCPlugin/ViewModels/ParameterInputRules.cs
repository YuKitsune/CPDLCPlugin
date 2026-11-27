using System.Text.RegularExpressions;
using CPDLC.Messages;

namespace CPDLCPlugin.ViewModels;

// The rules for the manual entry of uplink parameter values
public static class ParameterInputRules
{
    static readonly Regex DegreesRegex = new(@"^\d{3}[TM]$");

    static readonly string[] ToFromValues = ["TO", "FROM"];

    static readonly string[] DirectionValues =
    [
        "LEFT", "RIGHT", "EITHER SIDE",
        "NORTH", "SOUTH", "EAST", "WEST",
        "NORTH EAST", "NORTH WEST", "SOUTH EAST", "SOUTH WEST"
    ];

    // These types accept only the values in their popup menus
    public static bool AllowsManualEntry(ParameterType type)
    {
        return type is not (ParameterType.ToFrom or ParameterType.Direction or ParameterType.IcaoUnitName);
    }

    // The values in the popup menu. An empty list means that the type has no popup menu.
    public static string[] FixedValues(ParameterType type, string[] icaoUnitNames)
    {
        return type switch
        {
            ParameterType.ToFrom => ToFromValues,
            ParameterType.Direction => DirectionValues,
            ParameterType.IcaoUnitName => icaoUnitNames,
            _ => []
        };
    }

    // The maximum number of characters. 0 means that there is no limit.
    public static int MaxLength(ParameterType type)
    {
        return type switch
        {
            ParameterType.Frequency => 7,
            ParameterType.BeaconCode => 4,
            _ => 0
        };
    }

    public static bool IsAllowedCharacter(ParameterType type, char character)
    {
        return type switch
        {
            ParameterType.Frequency => IsDigit(character) || character == '.',
            ParameterType.BeaconCode => character is >= '0' and <= '7',
            ParameterType.DistanceOffset => IsDigit(character),
            _ => true
        };
    }

    // Returns false with an error message if the full value is not valid
    public static bool TryValidate(ParameterType type, string value, out string error)
    {
        switch (type)
        {
            case ParameterType.BeaconCode when value.Length != 4:
                error = $"{value} is not a valid beacon code. Use 4 digits from 0 to 7, for example 3000.";
                return false;

            case ParameterType.Degrees when !DegreesRegex.IsMatch(value):
                error = $"{value} is not a valid direction in degrees. Use 3 digits and T or M, for example 090M.";
                return false;

            default:
                error = string.Empty;
                return true;
        }
    }

    static bool IsDigit(char character) => character is >= '0' and <= '9';
}
