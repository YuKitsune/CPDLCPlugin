using System.Collections.Concurrent;
using CPDLCPlugin.Configuration;
using CPDLCServer.Contracts;
using vatsys;

namespace CPDLCPlugin;

/// <param name="Report">The latest ADS-C report for the aircraft.</param>
/// <param name="Shown">The time the report was shown on the ASD.</param>
/// <param name="Extrapolated">False until the first normal position extrapolation after the report.</param>
public record AdscTrack(AdscReportDto Report, DateTimeOffset Shown, bool Extrapolated);

/// <summary>
/// Holds the ADS-C tracks that move flight plan tracks on the ASD.
/// The patches in <see cref="AdscTrackPatches"/> read this class on the vatSys threads, so all members are synchronous.
/// </summary>
public class AdscTrackDisplay(IClock clock, PluginConfiguration configuration)
{
    // vatSys extrapolates the flight plan positions every 10 seconds, and also each time it processes a flight plan.
    // Show the reported position for this minimum time, so that a controller can see the ADSC symbol.
    readonly TimeSpan _minimumReportedPositionTime = TimeSpan.FromSeconds(configuration.ADSCTrackExtrapolationThreshold);

    readonly ConcurrentDictionary<string, AdscTrack> _tracks = new();

    /// <summary>
    /// Shows the reported position on the ASD (update position extrapolation), unless a newer report for the same aircraft is shown.
    /// The position stays until the first flight plan extrapolation after the ADS-C track extrapolation threshold.
    /// The reported altitude becomes the pilot reported level (PRL) of the flight plan.
    /// </summary>
    public void Show(AdscReportDto report)
    {
        var track = new AdscTrack(report, clock.UtcNow(), Extrapolated: false);
        var shownTrack = _tracks.AddOrUpdate(
            report.Callsign,
            track,
            (_, existing) => existing.Report.Timestamp > report.Timestamp ? existing : track);

        if (!ReferenceEquals(shownTrack, track))
            return;

        var fdr = FindFdr(report.Callsign);
        if (fdr is null)
            return;

        FDP2.SetPRL(fdr, report.Altitude);

        if (fdr.CoupledTrack is null)
            fdr.PredictedPosition.Location = new Coordinate(report.Latitude, report.Longitude);
    }

    public void Hide(string callsign)
    {
        if (_tracks.TryRemove(callsign, out _))
            RestoreFlightPlanPosition(callsign);
    }

    public void Clear()
    {
        var callsigns = _tracks.Keys.ToArray();
        _tracks.Clear();

        foreach (var callsign in callsigns)
            RestoreFlightPlanPosition(callsign);
    }

    public bool TryGetTrack(string callsign, out AdscTrack track)
    {
        return _tracks.TryGetValue(callsign, out track);
    }

    /// <summary>
    /// Returns the position of the ADS-C track, or null if the aircraft has no ADS-C track.
    /// Until the ADS-C track extrapolation threshold after the report was shown, this is the reported position.
    /// After that, this is the normal extrapolated position.
    /// </summary>
    public Coordinate? Extrapolate(FDP2.FDR fdr)
    {
        if (!_tracks.TryGetValue(fdr.Callsign, out var track))
            return null;

        var now = clock.UtcNow();
        if (now - track.Shown < _minimumReportedPositionTime)
            return new Coordinate(track.Report.Latitude, track.Report.Longitude);

        var position = AdscExtrapolation.Extrapolate(track.Report, fdr.ParsedRoute.ToList(), now);

        // Do not change a newer report that arrived during the extrapolation
        _tracks.TryUpdate(fdr.Callsign, track with { Extrapolated = true }, track);

        return position;
    }

    // vatSys extrapolates the flight plan positions every 10 seconds.
    // Calculate the position now, so that the track goes back to its flight plan position immediately.
    static void RestoreFlightPlanPosition(string callsign)
    {
        var fdr = FindFdr(callsign);
        if (fdr is not null)
            FDP2.GenerateTrackPosition(fdr);
    }

    static FDP2.FDR? FindFdr(string callsign)
    {
        return FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == callsign);
    }
}
