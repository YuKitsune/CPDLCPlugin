using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Handlers;

public class DialogueChangedNotificationHandler(
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<DialogueChangedNotification>
{
    public async Task Handle(DialogueChangedNotification notification, CancellationToken cancellationToken)
    {
        var dialogue = notification.Dialogue;

        logger.Verbose(
            "Dialogue {DialogueId} changed for {Callsign} - Closed: {Closed}, Archived: {Archived}",
            dialogue.Id,
            dialogue.AircraftCallsign,
            dialogue.IsClosed,
            dialogue.IsArchived);

        // Find all controllers on the same network and station
        var controllers = await controllerRepository.All(cancellationToken);
        if (controllers.Length == 0)
        {
            logger.Verbose(
                "No controllers to notify about dialogue change for {Callsign}",
                dialogue.AircraftCallsign);
            return;
        }

        // Convert to DTO and broadcast
        var dialogueDto = DialogueConverter.ToDto(dialogue);

        await hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("DialogueChanged", dialogueDto, cancellationToken);

        logger.Information(
            "Notified {ControllerCount} controller(s) about dialogue {DialogueId} change for {Callsign}",
            controllers.Length,
            dialogue.Id,
            dialogue.AircraftCallsign);
    }
}
