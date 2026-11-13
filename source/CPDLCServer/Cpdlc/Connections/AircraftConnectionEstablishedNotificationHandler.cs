using CPDLCServer.Controllers;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Persistence;
using CPDLCServer.Web;
using CPDLCServer.Contracts;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Cpdlc.Connections;

public class AircraftConnectionEstablishedNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : INotificationHandler<AircraftConnectionEstablished>
{
    public async Task Handle(AircraftConnectionEstablished notification, CancellationToken cancellationToken)
    {
        logger.Information(
            "Aircraft {Callsign} connected on {AcarsConnectionId} with data authority state {DataAuthorityState}",
            notification.Callsign,
            notification.AcarsConnectionId,
            notification.DataAuthorityState);

        // Find all controllers on the same network and station
        var controllers = await controllerRepository.All(cancellationToken);
        if (!controllers.Any())
        {
            logger.Information(
                "No controllers to notify about connected aircraft {Callsign}",
                notification.Callsign);
            return;
        }

        // Notify all controllers that an aircraft has connected
        unitOfWork.AfterCommit(ct => hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync(
                "AircraftConnectionUpdated",
                new AircraftConnectionDto(notification.Callsign, notification.StationId, DialogueConverter.ToDto(notification.DataAuthorityState)),
                ct));

        logger.Information(
            "Notified {ControllerCount} controller(s) about connected aircraft {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
