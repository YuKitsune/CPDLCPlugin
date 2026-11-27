using System.Text.RegularExpressions;

namespace CPDLCPlugin;

// Converts levels in feet to and from the text in an [altitude] parameter.
// The editor and the config use three digits, for example "350". Only the uplink text has a prefix, for example "F350".
public static class Levels
{
    static readonly Regex HundredsOfFeetRegex = new(@"^(?:FL|F|A)?\s?(\d{3})$");
    static readonly Regex FeetRegex = new(@"^(\d{4,5})\s?(?:FT)?$");

    // Hundreds of feet with three digits, for example 35000 is "350" and 9500 is "095"
    public static string FormatHundredsOfFeet(int feet)
    {
        return (feet / 100).ToString("000");
    }

    // The text in the uplink. FANS 1/A must know if the level is an altitude or a flight level.
    // A level below the transition level is an altitude, for example 9500 is "A095".
    // A level at or above the transition level is a flight level, for example 35000 is "F350".
    public static string FormatForUplink(int feet, int transitionLevel)
    {
        var prefix = feet < transitionLevel ? "A" : "F";
        return $"{prefix}{feet / 100:000}";
    }

    // Accepts FL350, F350, A090 and 350 (hundreds of feet), and 35000 or 35000 FT (feet)
    public static bool TryParse(string? text, out int feet)
    {
        var value = text?.Trim().ToUpperInvariant() ?? string.Empty;

        var hundredsOfFeetMatch = HundredsOfFeetRegex.Match(value);
        if (hundredsOfFeetMatch.Success)
        {
            feet = int.Parse(hundredsOfFeetMatch.Groups[1].Value) * 100;
            return true;
        }

        var feetMatch = FeetRegex.Match(value);
        if (feetMatch.Success)
        {
            feet = int.Parse(feetMatch.Groups[1].Value);
            return true;
        }

        feet = -1;
        return false;
    }
}
