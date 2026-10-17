namespace CPDLCServer.Model;

/// <summary>
/// The ADS-C requests that an ATSU sent to an aircraft.
/// The aircraft has only one periodic contract. A new periodic request replaces the previous one.
/// <list type="bullet">
/// <item>The default contract uses the default reporting interval of the ATSU.</item>
/// <item>A controller can select a different reporting interval. The selected contract ends at a fixed time, then the default contract is requested again.</item>
/// <item>A demand request asks for one report.</item>
/// </list>
/// </summary>
public class AdscContract(string callsign, string stationId)
{
    // The shortest reporting interval that a controller can request
    public static readonly TimeSpan MinimumReportingInterval = TimeSpan.FromMinutes(2);

    public string Callsign { get; } = callsign;
    public string StationId { get; } = stationId;

    // The interval of the latest periodic request. Null when no periodic contract was requested.
    public TimeSpan? ReportingInterval { get; private set; }

    public AdscContractState? DefaultContractState { get; private set; }

    public TimeSpan? SelectedReportingInterval { get; private set; }
    public AdscContractState? SelectedContractState { get; private set; }

    // Null when no selected contract is in use
    public DateTimeOffset? SelectedContractEnds { get; private set; }

    public AdscContractState? DemandContractState { get; private set; }

    public bool HasPeriodicContract =>
        DefaultContractState is AdscContractState.Sent or AdscContractState.Accepted ||
        SelectedContractState is AdscContractState.Sent or AdscContractState.Accepted;

    // The aircraft sends reports only for a request that it did not reject
    public bool IsActive =>
        HasPeriodicContract ||
        DemandContractState is AdscContractState.Sent or AdscContractState.Accepted;

    /// <summary>
    /// The default contract replaces a selected contract that is in use.
    /// A rejected selected contract stays, so that the controller can see the rejection.
    /// </summary>
    public void RequestDefaultContract(TimeSpan reportingInterval)
    {
        ReportingInterval = reportingInterval;
        DefaultContractState = AdscContractState.Sent;

        if (SelectedContractState is AdscContractState.Sent or AdscContractState.Accepted)
        {
            SelectedReportingInterval = null;
            SelectedContractState = null;
            SelectedContractEnds = null;
        }
    }

    public void RequestSelectedContract(TimeSpan reportingInterval, DateTimeOffset ends)
    {
        ReportingInterval = reportingInterval;
        SelectedReportingInterval = reportingInterval;
        SelectedContractState = AdscContractState.Sent;
        SelectedContractEnds = ends;
    }

    public void RequestDemandReport()
    {
        DemandContractState = AdscContractState.Sent;
    }

    /// <summary>
    /// The aircraft does not acknowledge a request, so a report accepts the latest periodic request and the demand request.
    /// When the selected contract is accepted, it replaces the default contract on the aircraft.
    /// </summary>
    /// <returns>True if the state of a request changed.</returns>
    public bool ReportReceived()
    {
        var changed = false;

        if (SelectedContractState == AdscContractState.Sent)
        {
            SelectedContractState = AdscContractState.Accepted;
            DefaultContractState = null;
            changed = true;
        }
        else if (DefaultContractState == AdscContractState.Sent)
        {
            DefaultContractState = AdscContractState.Accepted;
            changed = true;
        }

        if (DemandContractState == AdscContractState.Sent)
        {
            DemandContractState = AdscContractState.Accepted;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// A rejection does not identify the request.
    /// It rejects the latest periodic request that is in use, and a demand request that is not answered.
    /// </summary>
    /// <returns>True if the selected contract was rejected. The default contract must then be requested again.</returns>
    public bool Reject()
    {
        var selectedContractRejected = false;

        if (SelectedContractState is AdscContractState.Sent or AdscContractState.Accepted)
        {
            SelectedContractState = AdscContractState.Rejected;
            SelectedContractEnds = null;
            selectedContractRejected = true;
        }
        else if (DefaultContractState is AdscContractState.Sent or AdscContractState.Accepted)
        {
            DefaultContractState = AdscContractState.Rejected;
        }

        if (DemandContractState == AdscContractState.Sent)
            DemandContractState = AdscContractState.Rejected;

        return selectedContractRejected;
    }
}
