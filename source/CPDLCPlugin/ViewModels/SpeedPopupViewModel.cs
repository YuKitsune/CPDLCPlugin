using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CPDLC.Messages;

namespace CPDLCPlugin.ViewModels;

// The speeds in the speed popup, in knots or in Mach, with the highest speed at the top.
// The default speed comes from the value of the parameter. The cleared level converts it to the other unit.
public partial class SpeedPopupViewModel : ObservableObject
{
    const int MaximumKnots = 9990;
    const int KnotsIncrement = 10;

    // Mach numbers are in hundredths, so 82 is M0.82
    const int MinimumMach = 10;
    const int MaximumMach = 125;

    static readonly Regex KnotsRegex = new(@"^(\d{1,4})$");
    static readonly Regex MachRegex = new(@"^M?(\d?)\.(\d{2})$");

    readonly UplinkMessageTemplateElementComponentViewModel _templatePart;
    readonly int? _clearedLevel;

    // The default speed in the unit that the parameter value uses
    readonly int? _defaultSpeed;
    readonly bool _defaultIsMach;

    public string Callsign { get; }

    [ObservableProperty]
    bool isMach;

    [ObservableProperty]
    ScrollListOption[] options = [];

    public string UnitText => IsMach ? "MACH" : "KNOTS";

#if DEBUG
    // For testing in the designer
    public SpeedPopupViewModel()
        : this(new UplinkMessageTemplateElementComponentViewModel("[speed]", ParameterType.Speed, []) { Value = "280" }, "QFA1", 35000)
    {
    }
#endif

    // clearedLevel is in feet. It is null if the aircraft has no cleared level.
    public SpeedPopupViewModel(UplinkMessageTemplateElementComponentViewModel templatePart, string callsign, int? clearedLevel)
    {
        _templatePart = templatePart;
        _clearedLevel = clearedLevel;
        Callsign = callsign;

        if (TryParseSpeed(templatePart.Value, out var speed, out var speedIsMach))
        {
            _defaultSpeed = speed;
            _defaultIsMach = speedIsMach;
        }

        IsMach = _defaultSpeed is not null && _defaultIsMach;
        Options = CreateOptions();
    }

    partial void OnIsMachChanged(bool value)
    {
        OnPropertyChanged(nameof(UnitText));
        Options = CreateOptions();
    }

    public void Select(ScrollListOption option)
    {
        _templatePart.Value = option.Text;
    }

    ScrollListOption[] CreateOptions()
    {
        var defaultSpeed = GetDefaultSpeed();

        if (IsMach)
        {
            return Enumerable.Range(MinimumMach, MaximumMach - MinimumMach + 1)
                .Reverse()
                .Select(mach => new ScrollListOption(FormatMach(mach), mach == defaultSpeed))
                .ToArray();
        }

        return Enumerable.Range(0, MaximumKnots / KnotsIncrement + 1)
            .Select(i => i * KnotsIncrement)
            .Reverse()
            .Select(knots => new ScrollListOption(knots.ToString("0000"), knots == defaultSpeed))
            .ToArray();
    }

    // Returns the default speed in the unit that the popup shows, rounded to the increment of that unit.
    // Returns null if there is no default speed, or if the units are different and there is no cleared level.
    int? GetDefaultSpeed()
    {
        if (_defaultSpeed is not { } speed)
            return null;

        if (_defaultIsMach == IsMach)
            return IsMach ? speed : RoundToKnotsIncrement(speed);

        if (_clearedLevel is not { } clearedLevel)
            return null;

        return IsMach
            ? (int)Math.Round(SpeedConversions.IndicatedAirspeedToMach(speed, clearedLevel) * 100)
            : RoundToKnotsIncrement(SpeedConversions.MachToIndicatedAirspeed(speed / 100.0, clearedLevel));
    }

    static int RoundToKnotsIncrement(double knots)
    {
        return (int)Math.Round(knots / KnotsIncrement) * KnotsIncrement;
    }

    static string FormatMach(int mach)
    {
        return string.Format(CultureInfo.InvariantCulture, "M{0:0.00}", mach / 100.0);
    }

    // Accepts knots, for example "280" or "0280", and Mach, for example "M0.82", "M.82" or "0.82"
    static bool TryParseSpeed(string? value, out int speed, out bool isMach)
    {
        var text = value?.Trim().ToUpperInvariant() ?? string.Empty;

        var knotsMatch = KnotsRegex.Match(text);
        if (knotsMatch.Success)
        {
            speed = int.Parse(knotsMatch.Groups[1].Value);
            isMach = false;
            return true;
        }

        var machMatch = MachRegex.Match(text);
        if (machMatch.Success)
        {
            var units = machMatch.Groups[1].Value;
            speed = (units.Length > 0 ? int.Parse(units) * 100 : 0) + int.Parse(machMatch.Groups[2].Value);
            isMach = true;
            return true;
        }

        speed = 0;
        isMach = false;
        return false;
    }
}
