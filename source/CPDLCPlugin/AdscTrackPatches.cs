using System.Reflection;
using HarmonyLib;
using vatsys;

namespace CPDLCPlugin;

/// <summary>
/// Shows ADS-C tracks on the ASD.
/// vatSys does not draw ADS-C tracks, so these Harmony patches change the flight plan tracks that have an ADS-C report:
/// <list type="bullet">
/// <item>The track moves to the position in the latest ADS-C report.</item>
/// <item>The track uses the ADSC symbol, or the ADSCExtrapolated symbol when the report is old. The symbol does not rotate.</item>
/// </list>
/// A coupled radar track always has priority over an ADS-C report.
/// </summary>
public static class AdscTrackPatches
{
    const string HarmonyId = "CPDLCPlugin.AdscTracks";

    static AdscTrackDisplay? _display;
    static IClock? _clock;
    static TimeSpan _extrapolatedAfter;

    // The track that ASDControlDX.PaintASDTrack paints at this time.
    // Each ASD window paints on its own thread.
    [ThreadStatic]
    static Track? _trackBeingPainted;

    public static void Apply(AdscTrackDisplay display, IClock clock, TimeSpan extrapolatedAfter)
    {
        _display = display;
        _clock = clock;
        _extrapolatedAfter = extrapolatedAfter;

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
        if (_display is null || _clock is null || type != TrackSymbols.Types.FlightPlan)
            return;

        var track = _trackBeingPainted;
        if (track is null || track.Type != Track.TrackTypes.TRACK_TYPE_FP)
            return;

        var fdr = track.GetFDR();
        if (fdr is null || !_display.TryGetReport(fdr.Callsign, out var report))
            return;

        var reportAge = _clock.UtcNow() - report.Timestamp;
        type = reportAge > _extrapolatedAfter
            ? TrackSymbols.Types.ADSCExtrapolated
            : TrackSymbols.Types.ADSC;

        // vatSys does not rotate a symbol when the heading is -1
        aircraftHeading = -1;
    }

    static void GenerateTrackPositionPostfix(FDP2.FDR fdr)
    {
        if (_display is null || fdr.CoupledTrack is not null)
            return;

        if (!_display.TryGetReport(fdr.Callsign, out var report))
            return;

        fdr.PredictedPosition.Location = new Coordinate(report.Latitude, report.Longitude);
    }
}
