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

        // A new logon replaces the existing connection.
        // Hoppie identifies an aircraft only by its callsign, so the new logon can be from a different pilot.
        var existingAircraft = await aircraftRepository.Find(
            new(request.Callsign, request.AcarsConnectionId),
            cancellationToken);

        if (existingAircraft is not null)
        {
            await ResetConnection(existingAircraft, cancellationToken);
        }

        var aircraft = new AircraftConnection(
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

            var rejectMessageId = await messageIdProvider.GetNextMessageId(request.AcarsConnectionId, request.Callsign, cancellationToken);
            unitOfWork.AfterCommit(ct => acarsConnection.RejectLogon(request.Callsign, rejectMessageId, ct));

            await aircraftRepository.Remove(
                new AircraftKey(request.Callsign, request.AcarsConnectionId),
                cancellationToken);

            return;
        }

        logger.Information("New connection request received from {Callsign} on {AcarsConnectionId}. Accepting the request.", request.Callsign, request.AcarsConnectionId);

        // Immediately accept it for now
        aircraft.AcceptLogon(clock.UtcNow());

        var acceptMessageId = await messageIdProvider.GetNextMessageId(request.AcarsConnectionId, request.Callsign, cancellationToken);
        unitOfWork.AfterCommit(ct => acarsConnection.AcceptLogon(request.Callsign, acceptMessageId, ct));

        await mediator.Publish(
            new AircraftConnectionEstablished(
                request.AcarsConnectionId,
                aircraft.StationId,
                request.Callsign,
                aircraft.DataAuthorityState),
            cancellationToken);
    }

    // No response to the messages of the previous connection can arrive, so close its open dialogues
    async Task ResetConnection(AircraftConnection existingAircraft, CancellationToken cancellationToken)
    {
        logger.Information(
            "Connection request received from {Callsign} on {AcarsConnectionId}, which is already connected. Resetting the connection.",
            existingAircraft.Callsign,
            existingAircraft.AcarsConnectionId);

        var openDialogues = await dialogueRepository.FindOpenForAircraft(
            existingAircraft.Callsign,
            existingAircraft.StationId,
            cancellationToken);

        foreach (var dialogue in openDialogues)
        {
            var messageId = await messageIdProvider.GetNextMessageId(
                existingAircraft.AcarsConnectionId,
                existingAircraft.Callsign,
                cancellationToken);

            dialogue.AbortWithError(messageId, "CPDLC CONNECTION RESET", clock.UtcNow());
            await mediator.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

            logger.Information(
                "Closed dialogue {DialogueId} for reset connection with {Callsign}",
                dialogue.Id,
                existingAircraft.Callsign);
        }

        await mediator.Publish(
            new AircraftConnectionReset(
                existingAircraft.AcarsConnectionId,
                existingAircraft.StationId,
                existingAircraft.Callsign),
            cancellationToken);
    }
}
