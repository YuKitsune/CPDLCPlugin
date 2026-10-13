using CPDLCServer.Clients;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class RequestAdscContractOnConnectionEstablishedHandler(
    IClientManager clientManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    ILogger logger)
    : INotificationHandler<AircraftConnectionEstablished>
{
    public async Task Handle(AircraftConnectionEstablished notification, CancellationToken cancellationToken)
    {
        // A failure here must not stop the other handlers for the new CPDLC connection
        try
        {
            // Keep a periodic contract that a controller requested before the logon
            var contract = await adscContractRepository.Find(
                new AdscContractKey(notification.Callsign, notification.StationId),
                cancellationToken);
            if (contract?.ReportingInterval is not null)
            {
                logger.Information(
                    "{Callsign} already has a periodic ADS-C contract with {StationId}",
                    notification.Callsign,
                    notification.StationId);
                return;
            }

            var configuration = await clientManager.GetConfigurationForStation(notification.StationId, cancellationToken);

            await mediator.Send(
                new RequestPeriodicAdscContractCommand(
                    notification.StationId,
                    notification.Callsign,
                    TimeSpan.FromMinutes(configuration.DefaultAdscReportingInterval)),
                cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.Error(
                ex,
                "Failed to request ADS-C contract with {Callsign} for {StationId}",
                notification.Callsign,
                notification.StationId);
        }
    }
}
