using System.Windows.Forms;
using HarmonyLib;
using vatsys;

namespace CPDLCPlugin;

/// <summary>
/// A test tool that sends fake ADS-C reports.
/// After <see cref="Arm"/>, the next left click on an ASD goes to the callback with the clicked position.
/// vatSys does not get that click.
/// </summary>
public static class FakeAdscReportPatch
{
    const string HarmonyId = "CPDLCPlugin.FakeAdscReport";

    static Action<Coordinate>? _onClick;
    static volatile bool _armed;

    public static void Apply(Action<Coordinate> onClick)
    {
        _onClick = onClick;

        var method = AccessTools.Method("vatsys.ASDControlDX:ASD_MouseDown")
            ?? throw new MissingMethodException("Cannot find the vatSys method vatsys.ASDControlDX:ASD_MouseDown");

        new Harmony(HarmonyId).Patch(
            method,
            prefix: new HarmonyMethod(typeof(FakeAdscReportPatch), nameof(AsdMouseDownPrefix)));
    }

    public static void Arm()
    {
        _armed = true;
    }

    static bool AsdMouseDownPrefix(object __instance, MouseEventArgs e)
    {
        if (!_armed || e.Button != MouseButtons.Left)
            return true;

        _armed = false;

        try
        {
            // ASDControlDX sets this field to the cursor position when the mouse moves
            var position = Traverse.Create(__instance).Field("CursorLatLong").GetValue<Coordinate>();
            _onClick?.Invoke(position);
        }
        catch (Exception ex)
        {
            Plugin.AddError(ex, "Cannot send a fake ADS-C report");
        }

        return false;
    }
}
