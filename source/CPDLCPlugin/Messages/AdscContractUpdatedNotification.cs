using CPDLCServer.Contracts;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AdscContractUpdatedNotification(AdscContractDto AdscContractDto) : INotification;

public class AdscContractUpdatedNotificationHandler(AdscContractStore adscContractStore, ILogger logger)
    : INotificationHandler<AdscContractUpdatedNotification>
{
    public async Task Handle(AdscContractUpdatedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Upserting ADS-C contract for {Callsign} on {StationId}",
            notification.AdscContractDto.Callsign,
            notification.AdscContractDto.StationId);

        await adscContractStore.Upsert(notification.AdscContractDto, cancellationToken);
    }
}
