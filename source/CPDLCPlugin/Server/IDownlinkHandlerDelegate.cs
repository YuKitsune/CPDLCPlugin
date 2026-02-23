using CPDLCServer.Contracts;

namespace CPDLCPlugin.Server;

public interface IDownlinkHandlerDelegate
{
    Task DialogueChanged(DialogueDto dialogue, CancellationToken cancellationToken);
    Task AircraftConnectionUpdated(AircraftConnectionDto aircraftConnectionDto, CancellationToken cancellationToken);
    Task AircraftConnectionRemoved(string callsign, string stationId, CancellationToken cancellationToken);
    Task ControllerConnectionUpdated(ControllerConnectionDto controllerConnectionDto, CancellationToken cancellationToken);
    Task ControllerConnectionRemoved(string callsign, CancellationToken cancellationToken);
    Task AcarsConnectedCallsignsUpdated(string[] callsigns, CancellationToken cancellationToken);
    void Error(Exception exception);
}
