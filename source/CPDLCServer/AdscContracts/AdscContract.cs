using CPDLCServer.AdscContracts.Reports;

namespace CPDLCServer.AdscContracts;

/// <summary>
/// The ADS-C requests that an ATSU sent to an aircraft.
/// The aircraft has only one periodic contract. A new periodic request replaces the previous one.
/// <list type="bullet">
/// <item>The default contract uses the default reporting interval of the ATSU.</item>
/// <item>A controller can select a different reporting interval. The selected contract stays until a controller requests the default contract.</item>
/// <item>A demand request asks for one report.</item>
/// </list>
/// </summary>
public class AdscContract(string callsign, string stationId, TimeSpan defaultReportingInterval, DateTimeOffset created)
{
    // The ACARS connections poll for messages every 20 to 75 seconds. A shorter interval gives no more data.
    public static readonly TimeSpan MinimumReportingInterval = TimeSpan.FromSeconds(60);

    // The maximum delivery time of a report
    static readonly TimeSpan ReportDeliveryTolerance = TimeSpan.FromMinutes(3);

    public string Callsign { get; } = callsign;
    public string StationId { get; } = stationId;

    // The time that the ATSU made the contract
    public DateTimeOffset Created { get; } = created;

    public TimeSpan DefaultReportingInterval { get; private set; } = defaultReportingInterval;

    public TimeSpan? ReportingInterval { get; private set; }

    public AdscContractState? DefaultContractState { get; private set; }

    public TimeSpan? SelectedReportingInterval { get; private set; }
    public AdscContractState? SelectedContractState { get; private set; }

    public AdscContractState? DemandContractState { get; private set; }

    // The latest time that a request was sent, or that a message was received from the aircraft
    public DateTimeOffset LastActivity { get; private set; }

    public DateTimeOffset? LastReportReceived { get; private set; }

    public AdscReportSource? ReportingSource { get; private set; }

    public bool HasPeriodicContract =>
        DefaultContractState is AdscContractState.Sent or AdscContractState.Accepted ||
        SelectedContractState is AdscContractState.Sent or AdscContractState.Accepted;

    // The aircraft sends reports only for a request that it did not reject
    public bool IsActive =>
        HasPeriodicContract ||
        DemandContractState is AdscContractState.Sent or AdscContractState.Accepted;

    // A report expires when the next two periodic reports do not arrive.
    // A contract with only a demand request uses the default reporting interval.
    public TimeSpan ReportExpiry => (ReportingInterval ?? DefaultReportingInterval) * 2 + ReportDeliveryTolerance;

    public void LogActivity(DateTimeOffset now)
    {
        LastActivity = now;
    }

    /// <summary>
    /// The default contract replaces a selected contract that is in use.
    /// A rejected selected contract stays, so that the controller can see the rejection.
    /// </summary>
    public void RequestDefaultContract(TimeSpan reportingInterval)
    {
        // The default reporting interval of the ATSU can change while the server is running
        DefaultReportingInterval = reportingInterval;
        ReportingInterval = reportingInterval;
        DefaultContractState = AdscContractState.Sent;

        if (SelectedContractState is not (AdscContractState.Sent or AdscContractState.Accepted))
            return;

        SelectedReportingInterval = null;
        SelectedContractState = null;
    }

    public void RequestSelectedContract(TimeSpan reportingInterval)
    {
        ReportingInterval = reportingInterval;
        SelectedReportingInterval = reportingInterval;
        SelectedContractState = AdscContractState.Sent;
    }

    public void RequestDemandReport()
    {
        DemandContractState = AdscContractState.Sent;
    }

    /// <summary>
    /// The aircraft does not acknowledge a request, so a report accepts the latest periodic request and the demand request.
    /// When the selected contract is accepted, it replaces the default contract on the aircraft.
    /// </summary>
    public void ReportReceived(AdscReport report)
    {
        ReportingSource = report.Source;
        LastReportReceived = report.Timestamp;

        if (SelectedContractState == AdscContractState.Sent)
        {
            SelectedContractState = AdscContractState.Accepted;
            DefaultContractState = null;
        }
        else if (DefaultContractState == AdscContractState.Sent)
        {
            DefaultContractState = AdscContractState.Accepted;
        }

        if (DemandContractState == AdscContractState.Sent)
        {
            DemandContractState = AdscContractState.Accepted;
        }
    }

    /// <summary>
    /// The last report is too old to show the position of the aircraft.
    /// The time of the last report stays, so that the next periodic report is due immediately.
    /// The source is removed, so that reports from VATSIM can replace the reports from Hoppie.
    /// </summary>
    public void ReportTimedOut()
    {
        ReportingSource = null;
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
            selectedContractRejected = true;
        }
        else if (DefaultContractState is AdscContractState.Sent or AdscContractState.Accepted)
        {
            DefaultContractState = AdscContractState.Rejected;
        }

        if (DemandContractState == AdscContractState.Sent)
        {
            DemandContractState = AdscContractState.Rejected;
        }

        return selectedContractRejected;
    }
}
