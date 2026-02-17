using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Handlers;

public class AircraftDisconnectedNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<AircraftDisconnected>
{
    public async Task Handle(AircraftDisconnected notification, CancellationToken cancellationToken)
    {
        logger.Information(
            "Aircraft {Callsign} disconnected from {AcarsClientId}",
            notification.Callsign,
            notification.AcarsClientId);

        // Find all controllers on the same network and station
        var controllers = await controllerRepository.All(cancellationToken);

        if (!controllers.Any())
        {
            logger.Information(
                "No controllers to notify about disconnected aircraft {Callsign}",
                notification.Callsign);
            return;
        }

        // Notify all controllers that an aircraft has disconnected
        await hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("AircraftConnectionRemoved", notification.Callsign, notification.StationId, cancellationToken);

        logger.Information(
            "Notified {ControllerCount} controller(s) about disconnected aircraft {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
