using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public class CheckLostAircraftConnectionsRequestHandler(
    IAircraftRepository aircraftRepository,
    IClock clock,
    ServerStartTime serverStartTime,
    IMediator mediator,
    ILogger logger) : IRequestHandler<CheckLostAircraftConnectionsRequest>
{
    public async Task Handle(CheckLostAircraftConnectionsRequest request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow();

        // The last seen times do not change while the server is stopped
        if (now - serverStartTime.Started <= request.ConnectionTimeout)
        {
            logger.Debug("Server has not been online for longer than the connection timeout, not checking for lost connections");
            return;
        }

        var connections = await aircraftRepository.All(cancellationToken);

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
                new AircraftConnectionLost(connection.AcarsConnectionId, connection.Callsign),
                cancellationToken);
        }
    }
}
