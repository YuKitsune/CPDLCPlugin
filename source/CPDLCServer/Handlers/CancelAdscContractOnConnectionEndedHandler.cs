using CPDLCServer.Clients;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class CancelAdscContractOnConnectionEndedHandler(
    IClientManager clientManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    ILogger logger)
    : INotificationHandler<AircraftConnectionTerminated>, INotificationHandler<AircraftConnectionLost>
{
    public async Task Handle(AircraftConnectionTerminated notification, CancellationToken cancellationToken)
    {
        await CancelContract(notification.Callsign, notification.StationId, cancellationToken);
    }

    public async Task Handle(AircraftConnectionLost notification, CancellationToken cancellationToken)
    {
        var client = await clientManager.GetAcarsClient(notification.AcarsClientId, cancellationToken);
        await CancelContract(notification.Callsign, client.StationId, cancellationToken);
    }

    async Task CancelContract(string callsign, string stationId, CancellationToken cancellationToken)
    {
        // A failure here must not stop the other handlers for the ended CPDLC connection
        try
        {
            var contract = await adscContractRepository.Find(new AdscContractKey(callsign, stationId), cancellationToken);
            if (contract is null)
                return;

            await mediator.Send(new CancelAdscContractCommand(stationId, callsign), cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.Error(
                ex,
                "Failed to cancel ADS-C contract with {Callsign} for {StationId}",
                callsign,
                stationId);
        }
    }
}
