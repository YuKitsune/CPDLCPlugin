namespace CPDLCServer.Model;

public class AdscContract(
    string callsign,
    string stationId,
    TimeSpan? reportingInterval,
    DateTimeOffset requested)
{
    // The ACARS clients poll for messages every 20 to 75 seconds. A shorter interval gives no more data.
    public static readonly TimeSpan MinimumReportingInterval = TimeSpan.FromSeconds(60);

    public string Callsign { get; } = callsign;
    public string StationId { get; } = stationId;

    // Null when only demand reports were requested
    public TimeSpan? ReportingInterval { get; private set; } = reportingInterval;
    public DateTimeOffset Requested { get; private set; } = requested;

    public void ChangeReportingInterval(TimeSpan reportingInterval, DateTimeOffset now)
    {
        ReportingInterval = reportingInterval;
        Requested = now;
    }
}
