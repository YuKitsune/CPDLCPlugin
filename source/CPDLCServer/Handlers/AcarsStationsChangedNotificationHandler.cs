using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Handlers;

public class AcarsStationsChangedNotificationHandler(
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<AcarsStationsChangedNotification>
{
    public async Task Handle(AcarsStationsChangedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Broadcasting ACARS connected callsigns update ({Count} callsigns)", notification.Callsigns.Length);

        await hubContext.Clients.All.SendAsync(
            "AcarsStationsUpdated",
            notification.Callsigns,
            cancellationToken);
    }
}
