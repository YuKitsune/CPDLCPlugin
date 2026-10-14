using System.Collections.Concurrent;
using CPDLCServer.Contracts;
using vatsys;

namespace CPDLCPlugin;

/// <summary>
/// Holds the ADS-C reports that move flight plan tracks on the ASD.
/// The patches in <see cref="AdscTrackPatches"/> read this class on the vatSys threads, so all members are synchronous.
/// </summary>
public class AdscTrackDisplay
{
    readonly ConcurrentDictionary<string, AdscReportDto> _reports = new();

    /// <summary>
    /// Shows the report on the ASD, unless a newer report for the same aircraft is shown.
    /// </summary>
    public void Show(AdscReportDto report)
    {
        _reports.AddOrUpdate(
            report.Callsign,
            report,
            (_, existing) => existing.Timestamp > report.Timestamp ? existing : report);

        UpdateTrackPosition(report.Callsign);
    }

    public void Hide(string callsign)
    {
        if (_reports.TryRemove(callsign, out _))
            UpdateTrackPosition(callsign);
    }

    public void Clear()
    {
        var callsigns = _reports.Keys.ToArray();
        _reports.Clear();

        foreach (var callsign in callsigns)
            UpdateTrackPosition(callsign);
    }

    public bool TryGetReport(string callsign, out AdscReportDto report)
    {
        return _reports.TryGetValue(callsign, out report);
    }

    // vatSys calculates the track positions every 10 seconds.
    // Calculate the position now, so that the change shows immediately.
    static void UpdateTrackPosition(string callsign)
    {
        var fdr = FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == callsign);
        if (fdr is not null)
            FDP2.GenerateTrackPosition(fdr);
    }
}
