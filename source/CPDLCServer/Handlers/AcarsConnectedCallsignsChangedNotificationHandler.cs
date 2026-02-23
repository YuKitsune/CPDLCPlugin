using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Handlers;

public class AcarsConnectedCallsignsChangedNotificationHandler(
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<AcarsConnectedCallsignsChangedNotification>
{
    public async Task Handle(AcarsConnectedCallsignsChangedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Broadcasting ACARS connected callsigns update ({Count} callsigns)", notification.Callsigns.Length);

        await hubContext.Clients.All.SendAsync(
            "AcarsConnectedCallsignsUpdated",
            notification.Callsigns,
            cancellationToken);
    }
}
