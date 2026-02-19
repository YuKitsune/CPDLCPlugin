using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class CheckAircraftConnectionsRequestHandler(
    IClientManager clientManager,
    IAircraftRepository aircraftRepository,
    IClock clock,
    ILogger logger) : IRequestHandler<CheckAircraftConnectionsRequest>
{
    public async Task Handle(CheckAircraftConnectionsRequest request, CancellationToken cancellationToken)
    {
        var client = await clientManager.GetAcarsClient(request.AcarsClientId, cancellationToken);
        var activeConnections = await client.ListConnections(cancellationToken);

        var trackedConnections = await aircraftRepository.All(cancellationToken);
        var matchingConnections = trackedConnections
            .Where(c => c.AcarsClientId == request.AcarsClientId)
            .Where(c => activeConnections.Contains(c.Callsign));

        var now = clock.UtcNow();
        foreach (var connection in matchingConnections)
        {
            connection.LogLastSeen(now);
            logger.Debug("Updated last seen for {Callsign}", connection.Callsign);
        }
    }
}
