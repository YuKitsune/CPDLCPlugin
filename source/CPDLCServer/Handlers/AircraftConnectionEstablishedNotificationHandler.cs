using CPDLCServer.Clients;
using CPDLCServer.Contracts;
using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Handlers;

public class AircraftConnectionEstablishedNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<AircraftConnectionEstablished>
{
    public async Task Handle(AircraftConnectionEstablished notification, CancellationToken cancellationToken)
    {
        logger.Information(
            "Aircraft {Callsign} connected on {AcarsClientId} with data authority state {DataAuthorityState}",
            notification.Callsign,
            notification.AcarsClientId,
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
        await hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync(
                "AircraftConnectionUpdated",
                new AircraftConnectionDto(notification.Callsign, notification.StationId, DialogueConverter.ToDto(notification.DataAuthorityState)),
                cancellationToken);

        logger.Information(
            "Notified {ControllerCount} controller(s) about connected aircraft {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
