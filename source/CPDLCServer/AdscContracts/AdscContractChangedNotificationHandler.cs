using CPDLCServer.Controllers;
using CPDLCServer.Persistence;
using CPDLCServer.Web;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.AdscContracts;

public class AdscContractChangedNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    IUnitOfWork unitOfWork,
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

        var contractDto = AdscContractConverter.ToDto(contract);
        unitOfWork.AfterCommit(ct => hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("AdscContractUpdated", contractDto, ct));

        logger.Information(
            "Notified {ControllerCount} controller(s) about ADS-C contract change for {Callsign}",
            controllers.Length,
            contract.Callsign);
    }
}
