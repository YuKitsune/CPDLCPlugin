using CPDLCServer.Controllers;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using CPDLCServer.Web;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Cpdlc.Downlinks;

public class DownlinkReceivedNotificationHandler(
    IAircraftRepository aircraftRepository,
    IMediator mediator,
    IClock clock,
    IControllerRepository controllerRepository,
    IDialogueRepository dialogueRepository,
    IHubContext<ControllerHub> hubContext,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : INotificationHandler<DownlinkReceivedNotification>
{
    public async Task Handle(DownlinkReceivedNotification notification, CancellationToken cancellationToken)
    {
        var downlink = notification.Downlink;
        logger.Information("Downlink message received from {Callsign}", downlink.Sender);

        // Scenario 1: Logon request
        // TODO: Handle this in hoppies
        if (ControlMessages.IsLogonRequest(downlink))
        {
            await mediator.Send(
                new LogonCommand(
                    downlink.MessageId,
                    downlink.MessageReference,
                    downlink.Sender,
                    downlink.ResponseType,
                    downlink.AlertType,
                    downlink.Content,
                    downlink.Received,
                    notification.AcarsConnectionId),
                cancellationToken);
            return;
        }

        var aircraftConnection = await aircraftRepository.Find(
            new(downlink.Sender, notification.AcarsConnectionId),
            cancellationToken);

        // Scenario 2: Unknown aircraft
        if (aircraftConnection is null)
        {
            logger.Information("{Callsign} is not known by this ATSU, ignoring downlink", downlink.Sender);
            return;
        }

        // Scenario 3: Logoff notice
        // Show the logoff to the controller, then terminate the connection.
        // Return early so that the terminated connection is not promoted to CurrentDataAuthority.
        // TODO: Handle this in hoppies
        if (ControlMessages.IsLogoffNotice(downlink))
        {
            var logoffDialogue = new Dialogue(downlink.Sender);
            logoffDialogue.AddDownlink(
                downlink.MessageId,
                downlink.MessageReference,
                downlink.Sender,
                downlink.ResponseType,
                downlink.AlertType,
                downlink.Content,
                downlink.Received);

            await dialogueRepository.Add(logoffDialogue, cancellationToken);
            logger.Information("Dialogue {DialogueId} created for logoff from {Callsign}", logoffDialogue.Id, downlink.Sender);

            await mediator.Publish(new DialogueChangedNotification(logoffDialogue), cancellationToken);

            await mediator.Send(new TerminateConnectionRequest(downlink.Sender, notification.AcarsConnectionId), cancellationToken);
            return;
        }

        // Promote aircraft to CurrentDataAuthority on first downlink, unless
        // the aircraft explicitly indicates we are not the current data authority
        if (aircraftConnection.DataAuthorityState == DataAuthorityState.NextDataAuthority &&
            !ControlMessages.IsNotCurrentDataAuthority(downlink))
        {
            aircraftConnection.PromoteToCurrentDataAuthority();
            logger.Information("{Callsign} promoted to CurrentDataAuthority", downlink.Sender);

            var controllers = await controllerRepository.All(cancellationToken);
            if (controllers.Any())
            {
                var aircraftConnectionDto = new Contracts.AircraftConnectionDto(
                    aircraftConnection.Callsign,
                    notification.StationId,
                    DialogueConverter.ToDto(aircraftConnection.DataAuthorityState));

                unitOfWork.AfterCommit(ct => hubContext.Clients
                    .Clients(controllers.Select(c => c.ConnectionId))
                    .SendAsync("AircraftConnectionUpdated", aircraftConnectionDto, ct));

                logger.Information(
                    "Notified {ControllerCount} controller(s) that aircraft {Callsign} was promoted to CurrentDataAuthority",
                    controllers.Length,
                    aircraftConnection.Callsign);
            }
        }

        aircraftConnection.LogLastSeen(clock.UtcNow());

        // Scenario 4: Aircraft replies to an uplink
        if (downlink.MessageReference.HasValue)
        {
            var dialogue = await dialogueRepository.FindOpenDialogueByUplink(
                downlink.Sender,
                downlink.MessageReference.Value,
                cancellationToken);
            if (dialogue is not null)
            {
                dialogue.AddDownlink(downlink.MessageId, downlink.MessageReference, downlink.Sender, downlink.ResponseType, downlink.AlertType, downlink.Content, downlink.Received);
                logger.Information("Downlink from {Callsign} appended to dialogue {DialogueId}", downlink.Sender, dialogue.Id);
            }
            else
            {
                // TODO: This could lead to a bug if the Aircraft thinks we're still in the original message chain.
                //  Consider returning an error here instead.
                logger.Warning("No open dialogue found for uplink reference {MessageReference} from {Callsign} - starting new dialogue", downlink.MessageReference.Value, downlink.Sender);
                dialogue = new Dialogue(downlink.Sender);
                dialogue.AddDownlink(
                    downlink.MessageId,
                    downlink.MessageReference,
                    downlink.Sender,
                    downlink.ResponseType,
                    downlink.AlertType,
                    downlink.Content,
                    downlink.Received);

                await dialogueRepository.Add(dialogue, cancellationToken);
            }

            await mediator.Publish(new DialogueChangedNotification(dialogue), cancellationToken);
        }
        else // Scenario 5: Aircraft initiates a new dialogue
        {
            var dialogue = new Dialogue(downlink.Sender);
            dialogue.AddDownlink(
                downlink.MessageId,
                downlink.MessageReference,
                downlink.Sender,
                downlink.ResponseType,
                downlink.AlertType,
                downlink.Content,
                downlink.Received);

            await dialogueRepository.Add(dialogue, cancellationToken);
            logger.Information("Dialogue {DialogueId} created for downlink from {Callsign}", dialogue.Id, downlink.Sender);

            await mediator.Publish(new DialogueChangedNotification(dialogue), cancellationToken);
        }
    }
}
