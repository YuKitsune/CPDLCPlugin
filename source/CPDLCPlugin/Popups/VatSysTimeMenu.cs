using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using HarmonyLib;

namespace CPDLCPlugin.Popups;

// Opens the time menu of vatSys. The vatSys class is internal, so it is found by its name.
public static class VatSysTimeMenu
{
    const string TimeMenuTypeName = "vatsys.TimeMenu";

    // The same range as the vatSys default, in minutes before and after the seed time
    const int MinutesBefore = -90;
    const int MinutesAfter = 90;

    // The format of a [time] parameter, for example "1234"
    const string TimeFormat = "HHmm";

    // Shows the menu at the screen location, and returns the time that the controller selects, for example "1234".
    // The menu starts at the time in the value, or at the current time if the value is not a time.
    // Returns false if the controller closes the menu without a time.
    public static bool TryShow(string? value, Point screenLocation, out string selectedTime)
    {
        var type = AccessTools.TypeByName(TimeMenuTypeName)
            ?? throw new MissingMemberException($"Cannot find the vatSys class {TimeMenuTypeName}");

        var valueIsTime = TryParseTime(value, out var seedTime);
        if (!valueIsTime)
            seedTime = DateTime.UtcNow;

        // The arguments are SeedTime, TimeSelected, CancelButton, TimeCountMin and TimeCountMax
        using var menu = (Form)Activator.CreateInstance(type, seedTime, valueIsTime, false, MinutesBefore, MinutesAfter)!;
        menu.StartPosition = FormStartPosition.Manual;
        menu.Location = screenLocation;

        if (menu.ShowDialog() != DialogResult.OK)
        {
            selectedTime = string.Empty;
            return false;
        }

        var time = (DateTime)AccessTools.Field(type, "SelectedTime").GetValue(menu)!;
        selectedTime = time.ToString(TimeFormat, CultureInfo.InvariantCulture);
        return true;
    }

    // Accepts "1234" and "1234Z". The time is on the current UTC date.
    static bool TryParseTime(string? value, out DateTime time)
    {
        var text = value?.Trim().ToUpperInvariant().TrimEnd('Z') ?? string.Empty;

        if (DateTime.TryParseExact(text, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            time = DateTime.UtcNow.Date + parsed.TimeOfDay;
            return true;
        }

        time = default;
        return false;
    }
}
