using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public class TerminateConnectionHandler(
    IAircraftRepository aircraftRepository,
    IDialogueRepository dialogueRepository,
    IClock clock,
    IMediator mediator,
    ILogger logger)
    : IRequestHandler<TerminateConnectionRequest>
{
    public async Task Handle(TerminateConnectionRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Connection between {StationId} and {Callsign} terminating", request.AcarsConnectionId, request.Callsign);

        var aircraftKey = new AircraftKey(request.Callsign, request.AcarsConnectionId);

        // Get the aircraft connection before removing it so we can get the StationId
        var aircraftConnection = await aircraftRepository.Find(aircraftKey, cancellationToken);
        if (aircraftConnection is null)
            return;

        var didRemove = await aircraftRepository.Remove(aircraftKey, cancellationToken);
        if (!didRemove)
            return;

        // No response can arrive after the connection ends, so close the open dialogues
        var allDialogues = await dialogueRepository.All(cancellationToken);
        var openDialogues = allDialogues
            .Where(d => d.AircraftCallsign == request.Callsign && !d.IsClosed && !d.IsArchived)
            .ToArray();

        foreach (var dialogue in openDialogues)
        {
            dialogue.Abort(clock.UtcNow());
            await mediator.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

            logger.Information(
                "Closed dialogue {DialogueId} for terminated connection with {Callsign}",
                dialogue.Id,
                request.Callsign);
        }

        await mediator.Publish(
            new AircraftConnectionTerminated(
                request.AcarsConnectionId,
                aircraftConnection.StationId,
                request.Callsign),
            cancellationToken);
    }
}
