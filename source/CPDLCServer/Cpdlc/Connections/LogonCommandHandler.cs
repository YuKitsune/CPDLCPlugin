using CPDLCServer.AcarsConnections;
using CPDLCServer.Controllers;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public class LogonCommandHandler(
    IAcarsConnectionManager acarsConnectionManager,
    IAircraftRepository aircraftRepository,
    IControllerRepository controllerRepository,
    IDialogueRepository dialogueRepository,
    IMessageIdProvider messageIdProvider,
    IClock clock,
    IMediator mediator,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<LogonCommand>
{
    public async Task Handle(LogonCommand request, CancellationToken cancellationToken)
    {
        var acarsConnection = await acarsConnectionManager.GetAcarsConnection(request.AcarsConnectionId, cancellationToken);

        // If the aircraft already exists, simply accept the connection again
        // Likely an issue with the aircraft requiring a reconnect
        var aircraft = await aircraftRepository.Find(
            new(request.Callsign, request.AcarsConnectionId),
            cancellationToken);

        if (aircraft is not null)
        {
            logger.Verbose("Duplicate connection request received from {Callsign} on {AcarsConnectionId}. Accepting the request.", request.Callsign, request.AcarsConnectionId);

            await Reply(acarsConnection, request, "LOGON ACCEPTED", cancellationToken);

            return;
        }

        aircraft = new AircraftConnection(
            request.Callsign,
            request.AcarsConnectionId,
            acarsConnection.StationId,
            DataAuthorityState.NextDataAuthority);

        aircraft.RequestLogon(clock.UtcNow());

        await aircraftRepository.Add(
            new(request.Callsign, request.AcarsConnectionId),
            aircraft,
            cancellationToken);

        // Don't accept the logon if no controllers are connected
        var activeControllers = await controllerRepository.All(cancellationToken);
        if (activeControllers.Length == 0)
        {
            logger.Information("New connection request received from {Callsign} on {AcarsConnectionId}, but no ATS is online. Rejecting the request.", request.Callsign, request.AcarsConnectionId);

            await Reply(acarsConnection, request, "LOGON REJECTED. NO ATS AVBL.", cancellationToken);

            await aircraftRepository.Remove(
                new AircraftKey(request.Callsign, request.AcarsConnectionId),
                cancellationToken);

            return;
        }

        logger.Information("New connection request received from {Callsign} on {AcarsConnectionId}. Accepting the request.", request.Callsign, request.AcarsConnectionId);

        // Immediately accept it for now
        aircraft.AcceptLogon(clock.UtcNow());

        await Reply(acarsConnection, request, "LOGON ACCEPTED", cancellationToken);

        await mediator.Publish(
            new AircraftConnectionEstablished(
                request.AcarsConnectionId,
                aircraft.StationId,
                request.Callsign,
                aircraft.DataAuthorityState),
            cancellationToken);
    }

    async Task Reply(
        IAcarsConnection acarsConnection,
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
            request.AcarsConnectionId,
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

        unitOfWork.AfterCommit(async ct =>
        {
            await acarsConnection.Send(uplinkMessage, ct);
            logger.Information("Sent CPDLC message from SYSTEM to {Callsign}", request.Callsign);
        });
    }
}
