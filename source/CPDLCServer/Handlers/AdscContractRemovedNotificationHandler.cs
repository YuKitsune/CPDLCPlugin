using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Handlers;

public class AdscContractRemovedNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<AdscContractRemovedNotification>
{
    public async Task Handle(AdscContractRemovedNotification notification, CancellationToken cancellationToken)
    {
        var controllers = await controllerRepository.All(cancellationToken);
        if (controllers.Length == 0)
        {
            logger.Verbose(
                "No controllers to notify about removed ADS-C contract for {Callsign}",
                notification.Callsign);
            return;
        }

        await hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("AdscContractRemoved", notification.Callsign, notification.StationId, cancellationToken);

        logger.Information(
            "Notified {ControllerCount} controller(s) about removed ADS-C contract for {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
