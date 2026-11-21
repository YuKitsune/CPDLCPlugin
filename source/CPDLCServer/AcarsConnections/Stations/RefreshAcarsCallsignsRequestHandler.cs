using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.AcarsConnections.Stations;

public class RefreshAcarsCallsignsRequestHandler(
    IAcarsConnectionManager acarsConnectionManager,
    IAircraftRepository aircraftRepository,
    IAcarsStationRepository acarsConnectedCallsignsRepository,
    IMediator mediator,
    IClock clock,
    ILogger logger) : IRequestHandler<RefreshAcarsCallsignsRequest>
{
    public async Task Handle(RefreshAcarsCallsignsRequest request, CancellationToken cancellationToken)
    {
        var acarsConnection = await acarsConnectionManager.GetAcarsConnection(request.AcarsConnectionId, cancellationToken);
        var activeConnections = await acarsConnection.ListConnections(cancellationToken);

        // Update the repository and broadcast if changed
        var changed = await acarsConnectedCallsignsRepository.Update(activeConnections, cancellationToken);
        if (changed)
        {
            logger.Information("ACARS connected callsigns changed: {Count} callsigns", activeConnections.Length);
            await mediator.Publish(
                new AcarsStationsChangedNotification(activeConnections),
                cancellationToken);
        }

        // Update last seen for tracked connections
        var trackedConnections = await aircraftRepository.All(cancellationToken);
        var matchingConnections = trackedConnections
            .Where(c => c.AcarsConnectionId == request.AcarsConnectionId)
            .Where(c => activeConnections.Contains(c.Callsign));

        var now = clock.UtcNow();
        foreach (var connection in matchingConnections)
        {
            connection.LogLastSeen(now);
            logger.Debug("Updated last seen for {Callsign}", connection.Callsign);
        }
    }
}
