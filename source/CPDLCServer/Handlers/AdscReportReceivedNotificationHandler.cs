using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Handlers;

public class AdscReportReceivedNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<AdscReportReceivedNotification>
{
    public async Task Handle(AdscReportReceivedNotification notification, CancellationToken cancellationToken)
    {
        var controllers = await controllerRepository.All(cancellationToken);
        if (controllers.Length == 0)
        {
            logger.Verbose(
                "No controllers to notify about ADS-C report from {Callsign}",
                notification.Callsign);
            return;
        }

        var reportDto = AdscContractConverter.ToDto(
            notification.Callsign,
            notification.StationId,
            notification.Report,
            notification.Received);

        await hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("AdscReportReceived", reportDto, cancellationToken);

        logger.Information(
            "Notified {ControllerCount} controller(s) about ADS-C report from {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
