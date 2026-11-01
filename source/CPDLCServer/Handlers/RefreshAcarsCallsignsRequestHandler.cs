using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class RefreshAcarsCallsignsRequestHandler(
    IClientManager clientManager,
    IAircraftRepository aircraftRepository,
    IAcarsStationRepository acarsConnectedCallsignsRepository,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    IClock clock,
    ILogger logger) : IRequestHandler<RefreshAcarsCallsignsRequest>
{
    public async Task Handle(RefreshAcarsCallsignsRequest request, CancellationToken cancellationToken)
    {
        var client = await clientManager.GetAcarsClient(request.AcarsClientId, cancellationToken);
        var activeConnections = await client.ListConnections(cancellationToken);

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
            .Where(c => c.AcarsClientId == request.AcarsClientId)
            .Where(c => activeConnections.Contains(c.Callsign));

        var now = clock.UtcNow();
        foreach (var connection in matchingConnections)
        {
            connection.LogLastSeen(now);
            logger.Debug("Updated last seen for {Callsign}", connection.Callsign);
        }

        // An aircraft that is connected to the ACARS network keeps its ADS-C contracts with this station
        var contracts = await adscContractRepository.All(cancellationToken);
        var matchingContracts = contracts
            .Where(c => c.StationId == client.StationId)
            .Where(c => activeConnections.Contains(c.Callsign));

        foreach (var contract in matchingContracts)
        {
            contract.LogActivity(now);
            logger.Debug("Updated ADS-C contract activity for {Callsign}", contract.Callsign);
        }
    }
}
