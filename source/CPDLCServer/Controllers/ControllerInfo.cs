namespace CPDLCServer.Controllers;

// A controller that is connected to the hub with the plugin.
// The user ID identifies the hub connection. The CID and the name come from the plugin token.
public class ControllerInfo(
    Guid userId,
    string connectionId,
    string stationId,
    string callsign,
    string cid,
    string name)
{
    public Guid UserId { get; } = userId;
    public string ConnectionId { get; } = connectionId;
    public string StationId { get; } = stationId;
    public string Callsign { get; } = callsign;
    public string Cid { get; } = cid;
    public string Name { get; } = name;
}
