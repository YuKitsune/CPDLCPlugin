using CPDLCServer.Controllers;
using CPDLCServer.Persistence;
using CPDLCServer.Web;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Cpdlc.Connections;

public class AircraftConnectionTerminatedNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : INotificationHandler<AircraftConnectionTerminated>
{
    public async Task Handle(AircraftConnectionTerminated notification, CancellationToken cancellationToken)
    {
        logger.Information(
            "Aircraft {Callsign} disconnected from {AcarsConnectionId}",
            notification.Callsign,
            notification.AcarsConnectionId);

        var controllers = await controllerRepository.All(cancellationToken);
        if (!controllers.Any())
        {
            logger.Information(
                "No controllers to notify about disconnected aircraft {Callsign}",
                notification.Callsign);
            return;
        }

        // Notify all controllers that an aircraft has disconnected
        unitOfWork.AfterCommit(ct => hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("AircraftConnectionRemoved", notification.Callsign, notification.StationId, ct));

        logger.Information(
            "Notified {ControllerCount} controller(s) about disconnected aircraft {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
