using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class TerminateConnectionHandler(IAircraftRepository aircraftRepository, IMediator mediator, ILogger logger)
    : IRequestHandler<TerminateConnectionRequest>
{
    public async Task Handle(TerminateConnectionRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Connection between {StationId} and {Callsign} terminating", request.AcarsClientId, request.Callsign);

        var aircraftKey = new AircraftKey(request.Callsign, request.AcarsClientId);

        // Get the aircraft connection before removing it so we can get the StationId
        var aircraftConnection = await aircraftRepository.Find(aircraftKey, cancellationToken);
        if (aircraftConnection is null)
            return;

        var didRemove = await aircraftRepository.Remove(aircraftKey, cancellationToken);
        if (!didRemove)
            return;

        await mediator.Publish(
            new AircraftConnectionTerminated(
                request.AcarsClientId,
                aircraftConnection.StationId,
                request.Callsign),
            cancellationToken);
    }
}
