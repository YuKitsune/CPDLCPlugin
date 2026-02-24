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
    IPublisher publisher,
    ILogger logger)
    : INotificationHandler<DownlinkReceivedNotification>
{
    public async Task Handle(DownlinkReceivedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Downlink message received from {Callsign}", notification.Downlink.Sender);

        // Intercept logon requests and automatically respond
        Dialogue? dialogue;
        if (ControlMessages.IsLogonRequest(notification.Downlink))
        {
            // Create a dialogue for the logon request
            dialogue = new Dialogue(
                notification.Downlink.Sender,
                notification.Downlink);
            await dialogueRepository.Add(dialogue, cancellationToken);

            await mediator.Send(
                new LogonCommand(
                    notification.Downlink.MessageId,
                    notification.Downlink.Sender,
                    notification.AcarsClientId),
                cancellationToken);
            return;
        }

        var aircraftConnection = await aircraftRepository.Find(
            new (notification.Downlink.Sender, notification.AcarsClientId),
            cancellationToken);

        if (aircraftConnection is null)
        {
            logger.Information("{Callsign} is not known by this ATSU, sending error uplink", notification.Downlink.Sender);

            // Connection not known, reject.
            await mediator.Send(
                new SendUplinkCommand(
                    "SYSTEM",
                    notification.Downlink.Sender,
                    notification.Downlink.MessageId,
                    CpdlcUplinkResponseType.NoResponse,
                    "ERROR. CONNECTION NOT ESTABLISHED."),
                cancellationToken);
            return;
        }

        // Intercept logoff messages
        if (ControlMessages.IsLogoffNotice(notification.Downlink))
        {
            await mediator.Send(
                new TerminateConnectionRequest(
                    notification.Downlink.Sender,
                    notification.AcarsClientId),
                cancellationToken);

            // Allow these to flow through to the controller
        }

        // Promote aircraft to CurrentDataAuthority on first downlink, unless
        // the aircraft explicitly indicates we are not the current data authority
        if (aircraftConnection.DataAuthorityState == DataAuthorityState.NextDataAuthority &&
            !ControlMessages.IsNotCurrentDataAuthority(notification.Downlink))
        {
            aircraftConnection.PromoteToCurrentDataAuthority();
            logger.Information("{Callsign} promoted to CurrentDataAuthority", notification.Downlink.Sender);

            // Notify all controllers that the aircraft has been promoted to CurrentDataAuthority
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

        // Add or update the dialogue
        dialogue = notification.Downlink.MessageReference.HasValue
            ? await dialogueRepository.FindDialogueForMessage(
                notification.Downlink.Sender,
                notification.Downlink.MessageReference.Value,
                cancellationToken)
            : null;

        if (dialogue is null)
        {
            dialogue = new Dialogue(notification.Downlink.Sender, notification.Downlink);
            logger.Information("Dialogue {DialogueId} created for downlink from {Callsign}", dialogue.Id, notification.Downlink.Sender);
            await dialogueRepository.Add(dialogue, cancellationToken);
        }
        else
        {
            dialogue.AddMessage(notification.Downlink);
            logger.Information("Downlink from {Callsign} appended to dialogue {DialogueId}", notification.Downlink.Sender, dialogue.Id);
        }

        // Publish DialogueChangedNotification instead of broadcasting directly
        await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

        logger.Information(
            "Published dialogue change notification for downlink from {From}",
            notification.Downlink.Sender);
    }
}
