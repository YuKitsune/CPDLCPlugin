namespace CPDLCServer.Controllers;

public class ControllerInfo(
    Guid userId,
    string connectionId,
    string stationId,
    string callsign)
{
    public Guid UserId { get; } = userId;
    public string ConnectionId { get; } = connectionId;
    public string StationId { get; } = stationId;
    public string Callsign { get; } = callsign;
}
