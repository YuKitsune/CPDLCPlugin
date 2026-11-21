using CPDLCServer.Controllers;
using CPDLCServer.Persistence;
using CPDLCServer.Web;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.AdscContracts.Reports;

public class AdscReportExpiredNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : INotificationHandler<AdscReportExpiredNotification>
{
    public async Task Handle(AdscReportExpiredNotification notification, CancellationToken cancellationToken)
    {
        var controllers = await controllerRepository.All(cancellationToken);
        if (controllers.Length == 0)
        {
            logger.Verbose(
                "No controllers to notify about expired ADS-C report from {Callsign}",
                notification.Callsign);
            return;
        }

        unitOfWork.AfterCommit(ct => hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("AdscReportExpired", notification.Callsign, notification.StationId, ct));

        logger.Information(
            "Notified {ControllerCount} controller(s) about expired ADS-C report from {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
