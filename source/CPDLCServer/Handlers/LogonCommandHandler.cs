using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Services;
using MediatR;

namespace CPDLCServer.Handlers;

public class LogonCommandHandler(
    IClientManager clientManager,
    IAircraftRepository aircraftRepository,
    IControllerRepository controllerRepository,
    IDialogueRepository dialogueRepository,
    IMessageIdProvider messageIdProvider,
    IClock clock,
    IMediator mediator,
    ILogger logger)
    : IRequestHandler<LogonCommand>
{
    public async Task Handle(LogonCommand request, CancellationToken cancellationToken)
    {
        var client = await clientManager.GetAcarsClient(request.AcarsClientId, cancellationToken);

        // If the aircraft already exists, simply accept the connection again
        // Likely an issue with the aircraft requiring a reconnect
        var aircraft = await aircraftRepository.Find(
            new(request.AcarsClientId, request.Callsign),
            cancellationToken);

        if (aircraft is not null)
        {
            logger.Verbose("Duplicate connection request received from {Callsign} on {ClientId}. Accepting the request.", request.Callsign, request.AcarsClientId);

            await Reply(client, request, "LOGON ACCEPTED", cancellationToken);

            return;
        }

        aircraft = new AircraftConnection(
            request.Callsign,
            request.AcarsClientId,
            client.StationId,
            DataAuthorityState.NextDataAuthority);

        aircraft.RequestLogon(clock.UtcNow());

        await aircraftRepository.Add(
            new(request.Callsign, request.AcarsClientId),
            aircraft,
            cancellationToken);

        // Don't accept the logon if no controllers are connected
        var activeControllers = await controllerRepository.All(cancellationToken);
        if (activeControllers.Length == 0)
        {
            logger.Information("New connection request received from {Callsign} on {ClientId}, but no ATS is online. Rejecting the request.", request.Callsign, request.AcarsClientId);

            await Reply(client, request, "LOGON REJECTED. NO ATS AVBL.", cancellationToken);

            await aircraftRepository.Remove(
                new AircraftKey(request.Callsign, request.AcarsClientId),
                cancellationToken);

            return;
        }

        logger.Information("New connection request received from {Callsign} on {ClientId}. Accepting the request.", request.Callsign, request.AcarsClientId);

        // Immediately accept it for now
        aircraft.AcceptLogon(clock.UtcNow());

        await Reply(client, request, "LOGON ACCEPTED", cancellationToken);

        await mediator.Publish(
            new AircraftConnectionEstablished(
                request.AcarsClientId,
                aircraft.StationId,
                request.Callsign,
                aircraft.DataAuthorityState),
            cancellationToken);
    }

    async Task Reply(
        IAcarsClient client,
        LogonCommand request,
        string content,
        CancellationToken cancellationToken)
    {
        var dialogue = new Dialogue(request.Callsign);
        dialogue.AddDownlink(
            request.DownlinkId,
            request.DownlinkMessageReference,
            request.Callsign,
            request.DownlinkResponseType,
            request.DownlinkAlertType,
            request.DownlinkContent,
            request.DownlinkReceived);

        var messageId = await messageIdProvider.GetNextMessageId(
            request.AcarsClientId,
            request.Callsign,
            cancellationToken);

        var uplinkMessage = dialogue.AddUplink(
            messageId,
            request.DownlinkId,
            request.Callsign,
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            content,
            clock.UtcNow());

        await dialogueRepository.Add(dialogue, cancellationToken);
        logger.Information("Dialogue {DialogueId} created for logon reply to {Callsign}", dialogue.Id, request.Callsign);

        await mediator.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

        await client.Send(uplinkMessage, cancellationToken);
        logger.Information("Sent CPDLC message from SYSTEM to {Callsign}", request.Callsign);
    }
}
