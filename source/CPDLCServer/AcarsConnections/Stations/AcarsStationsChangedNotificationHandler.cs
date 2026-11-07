using CPDLCServer.Persistence;
using CPDLCServer.Web;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.AcarsConnections.Stations;

public class AcarsStationsChangedNotificationHandler(
    IHubContext<ControllerHub> hubContext,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : INotificationHandler<AcarsStationsChangedNotification>
{
    public Task Handle(AcarsStationsChangedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Broadcasting ACARS connected callsigns update ({Count} callsigns)", notification.Callsigns.Length);

        unitOfWork.AfterCommit(ct => hubContext.Clients.All.SendAsync(
            "AcarsStationsUpdated",
            notification.Callsigns,
            ct));

        return Task.CompletedTask;
    }
}
