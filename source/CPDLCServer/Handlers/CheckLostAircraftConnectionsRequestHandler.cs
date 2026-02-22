using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class CheckLostAircraftConnectionsRequestHandler(
    IAircraftRepository aircraftRepository,
    IClock clock,
    IMediator mediator,
    ILogger logger) : IRequestHandler<CheckLostAircraftConnectionsRequest>
{
    public async Task Handle(CheckLostAircraftConnectionsRequest request, CancellationToken cancellationToken)
    {
        var connections = await aircraftRepository.All(cancellationToken);
        var now = clock.UtcNow();

        foreach (var connection in connections)
        {
            var timeSinceLastSeen = now - connection.LastSeen;
            if (timeSinceLastSeen <= request.ConnectionTimeout)
            {
                continue;
            }

            logger.Information(
                "Aircraft {Callsign} not seen for {Duration}, publishing AircraftLost",
                connection.Callsign,
                timeSinceLastSeen);

            await mediator.Publish(
                new AircraftConnectionLost(connection.AcarsClientId, connection.Callsign),
                cancellationToken);
        }
    }
}
