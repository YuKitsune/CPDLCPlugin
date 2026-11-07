using CPDLCServer.Controllers;
using CPDLCServer.Persistence;
using CPDLCServer.Web;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.AdscContracts.Reports;

public class AdscReportReceivedNotificationHandler(
    IAdscReportRepository adscReportRepository,
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : INotificationHandler<AdscReportReceivedNotification>
{
    public async Task Handle(AdscReportReceivedNotification notification, CancellationToken cancellationToken)
    {
        // Keep the report, so that controllers who connect later can load it
        await adscReportRepository.Add(
            new AircraftAdscReport(notification.Callsign, notification.StationId, notification.Report, notification.Received),
            cancellationToken);

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

        unitOfWork.AfterCommit(ct => hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("AdscReportReceived", reportDto, ct));

        logger.Information(
            "Notified {ControllerCount} controller(s) about ADS-C report from {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
