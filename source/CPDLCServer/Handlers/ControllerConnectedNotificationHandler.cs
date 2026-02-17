using CPDLCServer.Contracts;
using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Handlers;

public class ControllerConnectedNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<ControllerConnectedNotification>
{
    public async Task Handle(ControllerConnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Controller {Callsign} connected", notification.Callsign);

        // Find all other controllers on the same network and station
        var controllers = await controllerRepository.All(cancellationToken);

        // Exclude the controller that just connected
        var otherControllers = controllers.Where(c => c.UserId != notification.UserId).ToArray();

        if (!otherControllers.Any())
        {
            logger.Information(
                "No other controllers to notify about connected controller {Callsign}",
                notification.Callsign);
            return;
        }

        // Notify all other controllers that a peer controller has connected
        await hubContext.Clients
            .Clients(otherControllers.Select(c => c.ConnectionId))
            .SendAsync(
                "ControllerConnectionUpdated",
                new ControllerConnectionDto(
                    notification.StationId,
                    notification.Callsign,
                    controllers.First(c => c.UserId == notification.UserId).VatsimCid),
                cancellationToken);

        logger.Information(
            "Notified {ControllerCount} controller(s) about connected controller {Callsign}",
            otherControllers.Length,
            notification.Callsign);
    }
}
