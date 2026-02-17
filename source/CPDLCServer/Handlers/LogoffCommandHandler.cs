using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class LogoffCommandHandler(IAircraftRepository aircraftRepository, IMediator mediator, ILogger logger)
    : IRequestHandler<LogoffCommand>
{
    public async Task Handle(LogoffCommand request, CancellationToken cancellationToken)
    {
        logger.Information("Logoff requested from {Callsign}", request.Callsign);

        var aircraftKey = new AircraftKey(request.Callsign, request.AcarsClientId);

        // Get the aircraft connection before removing it so we can get the StationId
        var aircraftConnection = await aircraftRepository.Find(aircraftKey, cancellationToken);
        if (aircraftConnection is null)
            return;

        var didRemove = await aircraftRepository.Remove(aircraftKey, cancellationToken);
        if (!didRemove)
            return;

        await mediator.Publish(
            new AircraftDisconnected(
                request.AcarsClientId,
                aircraftConnection.StationId,
                request.Callsign),
            cancellationToken);
    }
}
