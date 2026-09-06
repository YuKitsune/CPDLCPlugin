using CPDLCServer.Hubs;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Handlers;

public class DownlinkReceivedNotificationHandler(
    IAircraftRepository aircraftRepository,
    IMediator mediator,
    IClock clock,
    IControllerRepository controllerRepository,
    IDialogueRepository dialogueRepository,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<DownlinkReceivedNotification>
{
    public async Task Handle(DownlinkReceivedNotification notification, CancellationToken cancellationToken)
    {
        var downlink = notification.Downlink;
        logger.Information("Downlink message received from {Callsign}", downlink.Sender);

        // Scenario 1: Logon request
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
                    notification.AcarsClientId),
                cancellationToken);
            return;
        }

        var aircraftConnection = await aircraftRepository.Find(
            new(downlink.Sender, notification.AcarsClientId),
            cancellationToken);

        // Scenario 2: Unknown aircraft
        if (aircraftConnection is null)
        {
            logger.Information("{Callsign} is not known by this ATSU, ignoring downlink", downlink.Sender);
            return;
        }

        // Intercept logoff messages
        if (ControlMessages.IsLogoffNotice(downlink))
        {
            await mediator.Send(new TerminateConnectionRequest(downlink.Sender, notification.AcarsClientId), cancellationToken);
            // Allow these to flow through to the controller
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
                await hubContext.Clients
                    .Clients(controllers.Select(c => c.ConnectionId))
                    .SendAsync(
                        "AircraftConnectionUpdated",
                        new Contracts.AircraftConnectionDto(
                            aircraftConnection.Callsign,
                            notification.StationId,
                            DialogueConverter.ToDto(aircraftConnection.DataAuthorityState)),
                        cancellationToken);

                logger.Information(
                    "Notified {ControllerCount} controller(s) that aircraft {Callsign} was promoted to CurrentDataAuthority",
                    controllers.Length,
                    aircraftConnection.Callsign);
            }
        }

        aircraftConnection.LogLastSeen(clock.UtcNow());

        // Scenario 3: Aircraft replies to an uplink
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
        else // Scenario 4: Aircraft initiates a new dialogue
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
