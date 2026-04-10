using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class TerminateConnectionOnDialogueClosedHandler(
    IAircraftRepository aircraftRepository,
    IMediator mediator,
    ILogger logger)
    : INotificationHandler<DialogueChangedNotification>
{
    public async Task Handle(DialogueChangedNotification notification, CancellationToken cancellationToken)
    {
        // Only act when dialogue is closed
        if (!notification.Dialogue.IsClosed)
            return;

        // Check if any uplink in the dialogue contains END SERVICE
        var hasEndService = notification.Dialogue.Messages
            .OfType<UplinkMessage>()
            .Any(m => ControlMessages.IsEndServiceUplink(m));

        if (!hasEndService)
            return;

        // Find the aircraft connection (CDA state)
        var aircraftConnections = await aircraftRepository.All(cancellationToken);
        var aircraftConnection = aircraftConnections.FirstOrDefault(c =>
            c.Callsign == notification.Dialogue.AircraftCallsign &&
            c.DataAuthorityState == DataAuthorityState.CurrentDataAuthority);

        if (aircraftConnection is null)
        {
            logger.Warning(
                "Cannot terminate connection for {Callsign}: aircraft connection not found",
                notification.Dialogue.AircraftCallsign);
            return;
        }

        logger.Information(
            "END SERVICE dialogue closed for {Callsign}, terminating connection",
            aircraftConnection.Callsign);

        await mediator.Send(
            new TerminateConnectionRequest(
                aircraftConnection.Callsign,
                aircraftConnection.AcarsClientId),
            cancellationToken);
    }
}
