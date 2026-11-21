using CPDLCServer.Persistence;
using CPDLCServer.Web;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Controllers;

public class ControllerDisconnectedNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : INotificationHandler<ControllerDisconnectedNotification>
{
    public async Task Handle(ControllerDisconnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Controller {Callsign} disconnected ", notification.Callsign);

        // Find all controllers on the same network and station
        // Note: The disconnected controller is already removed from the repository
        var controllers = await controllerRepository.All(cancellationToken);

        if (!controllers.Any())
        {
            logger.Information(
                "No controllers to notify about disconnected controller {Callsign}",
                notification.Callsign);
            return;
        }

        // Notify all remaining controllers that a peer controller has disconnected
        unitOfWork.AfterCommit(ct => hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("ControllerConnectionRemoved", notification.Callsign, ct));

        logger.Information(
            "Notified {ControllerCount} controller(s) about disconnected controller {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
