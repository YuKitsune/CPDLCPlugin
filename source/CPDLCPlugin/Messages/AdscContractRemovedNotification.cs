using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AdscContractRemovedNotification(string Callsign, string StationId) : INotification;

public class AdscContractRemovedNotificationHandler(
    AdscContractStore adscContractStore,
    AdscTrackDisplay adscTrackDisplay,
    ILogger logger)
    : INotificationHandler<AdscContractRemovedNotification>
{
    public async Task Handle(AdscContractRemovedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("ADS-C contract for {Callsign} on {StationId} removed", notification.Callsign, notification.StationId);

        if (!await adscContractStore.Remove(notification.Callsign, notification.StationId, cancellationToken))
        {
            logger.Warning("ADS-C contract for {Callsign} was not tracked on {StationId}", notification.Callsign, notification.StationId);
        }

        // Show the ADS-C track only while the aircraft has a contract with an ATSU
        var contracts = await adscContractStore.All(cancellationToken);
        if (contracts.All(c => c.Callsign != notification.Callsign))
            adscTrackDisplay.Hide(notification.Callsign);
    }
}
