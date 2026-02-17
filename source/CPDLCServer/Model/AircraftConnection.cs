namespace CPDLCServer.Model;

public class AircraftConnection(
    string callsign,
    string acarsClientId,
    string stationId,
    DataAuthorityState dataAuthorityState)
{
    public string Callsign { get; } = callsign;
    public string AcarsClientId { get; } = acarsClientId;
    public string StationId { get; } = stationId;

    public DataAuthorityState DataAuthorityState { get; private set; } = dataAuthorityState;
    public ConnectionState ConnectionState { get; private set; }

    public DateTimeOffset LogonRequested { get; private set; }
    public DateTimeOffset? LogonAccepted { get; private set; }
    public DateTimeOffset LastSeen { get; private set; }

    public void RequestLogon(DateTimeOffset now)
    {
        ConnectionState = ConnectionState.Pending;
        LogonRequested = now;
        LogonAccepted = null;
        LogLastSeen(now);
    }

    public void AcceptLogon(DateTimeOffset now)
    {
        ConnectionState = ConnectionState.Connected;
        LogonAccepted = now;
        LogLastSeen(now);
    }

    public void LogLastSeen(DateTimeOffset now)
    {
        LastSeen = now;
    }

    public void PromoteToCurrentDataAuthority()
    {
        DataAuthorityState = DataAuthorityState.CurrentDataAuthority;
    }
}
