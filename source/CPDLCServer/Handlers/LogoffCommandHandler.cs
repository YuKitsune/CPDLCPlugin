using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class LogoffCommandHandler(IAircraftRepository aircraftRepository, IMediator mediator)
    : IRequestHandler<LogoffCommand>
{
    public async Task Handle(LogoffCommand request, CancellationToken cancellationToken)
    {
        var didRemove = await aircraftRepository.Remove(
            new AircraftKey(request.Callsign, request.AcarsClientId),
            cancellationToken);

        if (!didRemove)
            return;

        await mediator.Publish(
            new AircraftDisconnected(
                request.AcarsClientId,
                request.Callsign),
            cancellationToken);
    }
}
