namespace CPDLCPlugin.ViewModels;

// The levels in the level popup, with the highest level at the top, the same as the vatSys CFL menu.
// The levels are in hundreds of feet with three digits, for example 350 and 095.
// The default level comes from the value of the parameter, rounded down to a level in the list.
public class LevelPopupViewModel
{
    // The highest level in the vatSys CFL menu
    const int MaximumLevel = 59000;

    readonly UplinkMessageTemplateElementComponentViewModel _templatePart;

    public string Callsign { get; }

    public ScrollListOption[] Options { get; }

    public LevelPopupViewModel(UplinkMessageTemplateElementComponentViewModel templatePart, string callsign)
    {
        _templatePart = templatePart;
        Callsign = callsign;

        var levels = GetLevels().Reverse().ToArray();

        // The levels are in descending sequence, so the first level that is not more than the value is the level below it
        int? defaultLevel = Levels.TryParse(templatePart.Value, out var feet)
            ? levels.Cast<int?>().FirstOrDefault(level => level <= feet)
            : null;

        Options = levels
            .Select(level => new ScrollListOption(Levels.FormatHundredsOfFeet(level), level == defaultLevel))
            .ToArray();
    }

    public void Select(ScrollListOption option)
    {
        _templatePart.Value = option.Text;
    }

    // 0 to 10,000 ft in increments of 500 ft, FL100 to FL410 in increments of 1,000 ft,
    // and FL410 to the maximum level in increments of 2,000 ft
    static IEnumerable<int> GetLevels()
    {
        for (var level = 0; level < 10000; level += 500)
            yield return level;

        for (var level = 10000; level < 41000; level += 1000)
            yield return level;

        for (var level = 41000; level <= MaximumLevel; level += 2000)
            yield return level;
    }
}
