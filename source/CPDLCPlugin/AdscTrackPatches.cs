using System.Reflection;
using HarmonyLib;
using vatsys;

namespace CPDLCPlugin;

/// <summary>
/// Shows ADS-C tracks on the ASD.
/// vatSys does not draw ADS-C tracks, so these Harmony patches change the flight plan tracks that have an ADS-C report:
/// <list type="bullet">
/// <item>When a report arrives, the track moves to the reported position and uses the ADSC symbol.</item>
/// <item>Each time vatSys extrapolates the flight plan positions, the track moves along the route from the reported position and uses the ADSCExtrapolated symbol.</item>
/// </list>
/// The symbol does not rotate.
/// A coupled radar track always has priority over an ADS-C report.
/// </summary>
public static class AdscTrackPatches
{
    const string HarmonyId = "CPDLCPlugin.AdscTracks";

    static AdscTrackDisplay? _display;

    // The track that ASDControlDX.PaintASDTrack paints at this time.
    // Each ASD window paints on its own thread.
    [ThreadStatic]
    static Track? _trackBeingPainted;

    public static void Apply(AdscTrackDisplay display)
    {
        _display = display;

        var harmony = new Harmony(HarmonyId);
        try
        {
            // Patch the symbol before the position, so that an ADS-C position never shows with the flight plan symbol
            harmony.Patch(
                RequireMethod("vatsys.ASDControlDX:PaintASDTrack"),
                prefix: new HarmonyMethod(typeof(AdscTrackPatches), nameof(PaintASDTrackPrefix)),
                finalizer: new HarmonyMethod(typeof(AdscTrackPatches), nameof(PaintASDTrackFinalizer)));

            harmony.Patch(
                RequireMethod("vatsys.ASDControlDX:PaintTrackGeometryRealisation"),
                prefix: new HarmonyMethod(typeof(AdscTrackPatches), nameof(PaintTrackGeometryRealisationPrefix)));

            harmony.Patch(
                RequireMethod("vatsys.FDP2:GenerateTrackPosition"),
                postfix: new HarmonyMethod(typeof(AdscTrackPatches), nameof(GenerateTrackPositionPostfix)));
        }
        catch
        {
            harmony.UnpatchAll(HarmonyId);
            throw;
        }
    }

    static MethodInfo RequireMethod(string typeColonMethodName)
    {
        return AccessTools.Method(typeColonMethodName)
            ?? throw new MissingMethodException($"Cannot find the vatSys method {typeColonMethodName}");
    }

    static void PaintASDTrackPrefix(Track tl)
    {
        _trackBeingPainted = tl;
    }

    static void PaintASDTrackFinalizer()
    {
        _trackBeingPainted = null;
    }

    static void PaintTrackGeometryRealisationPrefix(ref TrackSymbols.Types type, ref double aircraftHeading)
    {
        if (_display is null || type != TrackSymbols.Types.FlightPlan)
            return;

        var paintedTrack = _trackBeingPainted;
        if (paintedTrack is null || paintedTrack.Type != Track.TrackTypes.TRACK_TYPE_FP)
            return;

        var fdr = paintedTrack.GetFDR();
        if (fdr is null || !_display.TryGetTrack(fdr.Callsign, out var adscTrack))
            return;

        // TODO: Temporary test. The symbols are swapped. Revert this change.
        type = adscTrack.Extrapolated
            ? TrackSymbols.Types.ADSC
            : TrackSymbols.Types.ADSCExtrapolated;

        // vatSys does not rotate a symbol when the heading is -1
        aircraftHeading = -1;
    }

    static void GenerateTrackPositionPostfix(FDP2.FDR fdr)
    {
        if (_display is null || fdr.CoupledTrack is not null)
            return;

        var position = _display.Extrapolate(fdr);
        if (position is not null)
            fdr.PredictedPosition.Location = position;
    }
}
