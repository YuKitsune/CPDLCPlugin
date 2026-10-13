using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AdscContractRemovedNotification(string Callsign, string StationId) : INotification;

public class AdscContractRemovedNotificationHandler(AdscContractStore adscContractStore, ILogger logger)
    : INotificationHandler<AdscContractRemovedNotification>
{
    public async Task Handle(AdscContractRemovedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("ADS-C contract for {Callsign} on {StationId} removed", notification.Callsign, notification.StationId);

        if (!await adscContractStore.Remove(notification.Callsign, notification.StationId, cancellationToken))
        {
            logger.Warning("ADS-C contract for {Callsign} was not tracked on {StationId}", notification.Callsign, notification.StationId);
        }
    }
}
