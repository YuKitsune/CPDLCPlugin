using CPDLCServer.Controllers;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using CPDLCServer.Web;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Cpdlc.Connections;

public class AircraftConnectionLostNotificationHandler(
    IAircraftRepository aircraftRepository,
    IControllerRepository controllerRepository,
    IDialogueRepository dialogueRepository,
    IHubContext<ControllerHub> hubContext,
    IMessageIdProvider messageIdProvider,
    IPublisher publisher,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : INotificationHandler<AircraftConnectionLost>
{
    public async Task Handle(AircraftConnectionLost notification, CancellationToken cancellationToken)
    {
        var aircraft = await aircraftRepository.Find(
            new(notification.Callsign, notification.AcarsConnectionId),
            cancellationToken);
        if (aircraft is null)
        {
            logger.Information(
                "Aircraft {Callsign} already removed from tracking on {AcarsConnectionId}",
                notification.Callsign,
                notification.AcarsConnectionId);
            return;
        }

        // Remove aircraft from tracking
        await aircraftRepository.Remove(
            new(notification.Callsign, notification.AcarsConnectionId),
            cancellationToken);

        logger.Information(
            "Aircraft {Callsign} lost on {AcarsConnectionId}",
            notification.Callsign,
            notification.AcarsConnectionId);

        // Close the dialogues also when no controllers are online, so that no dialogue of the lost connection stays open
        var openDialogues = await dialogueRepository.FindOpenForAircraft(
            notification.Callsign,
            aircraft.StationId,
            cancellationToken);

        if (openDialogues.Any())
        {
            // Add error message to each open dialogue, then close the remaining open messages, because no response can arrive
            foreach (var dialogue in openDialogues)
            {
                var messageId = await messageIdProvider.GetNextMessageId(
                    notification.AcarsConnectionId,
                    notification.Callsign,
                    cancellationToken);

                dialogue.AbortWithError(messageId, "ERROR CONNECTION TIMED OUT", clock.UtcNow());

                // Publish DialogueChangedNotification
                await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

                logger.Information(
                    "Added error message to dialogue {DialogueId} for lost aircraft {Callsign}",
                    dialogue.Id,
                    notification.Callsign);
            }
        }
        else
        {
            // No open dialogues, create a new one with the error message
            var messageId = await messageIdProvider.GetNextMessageId(
                notification.AcarsConnectionId,
                notification.Callsign,
                cancellationToken);

            var dialogue = new Dialogue(notification.Callsign, aircraft.StationId);
            dialogue.AddDownlink(
                messageId,
                null,
                notification.Callsign,
                CpdlcDownlinkResponseType.NoResponse,
                AlertType.Medium,
                "ERROR CONNECTION TIMED OUT",
                clock.UtcNow());

            await dialogueRepository.Add(dialogue, cancellationToken);

            // Publish DialogueChangedNotification
            await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

            logger.Information(
                "Created new dialogue {DialogueId} with error message for lost aircraft {Callsign}",
                dialogue.Id,
                notification.Callsign);
        }

        // After the error messages, because they use the message IDs of the connection
        await messageIdProvider.Remove(notification.AcarsConnectionId, notification.Callsign, cancellationToken);

        // Notify controllers that the aircraft has disconnected
        var controllers = await controllerRepository.All(cancellationToken);
        if (!controllers.Any())
        {
            logger.Information(
                "No controllers to notify about lost aircraft {Callsign}",
                notification.Callsign);
            return;
        }

        var controllerConnectionIds = controllers.Select(c => c.ConnectionId).ToArray();
        unitOfWork.AfterCommit(ct => hubContext.Clients
            .Clients(controllerConnectionIds)
            .SendAsync("AircraftConnectionRemoved", notification.Callsign, aircraft.StationId, ct));

        logger.Information(
            "Notified {ControllerCount} controller(s) about lost aircraft {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
