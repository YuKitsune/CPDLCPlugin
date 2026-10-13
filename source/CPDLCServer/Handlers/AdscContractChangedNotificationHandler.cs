using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Handlers;

public class AdscContractChangedNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<AdscContractChangedNotification>
{
    public async Task Handle(AdscContractChangedNotification notification, CancellationToken cancellationToken)
    {
        var contract = notification.Contract;

        var controllers = await controllerRepository.All(cancellationToken);
        if (controllers.Length == 0)
        {
            logger.Verbose(
                "No controllers to notify about ADS-C contract change for {Callsign}",
                contract.Callsign);
            return;
        }

        await hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("AdscContractUpdated", AdscContractConverter.ToDto(contract), cancellationToken);

        logger.Information(
            "Notified {ControllerCount} controller(s) about ADS-C contract change for {Callsign}",
            controllers.Length,
            contract.Callsign);
    }
}
