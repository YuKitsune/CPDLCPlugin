using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AdscReportExpiredNotification(string Callsign, string StationId) : INotification;

public class AdscReportExpiredNotificationHandler(
    AdscReportStore adscReportStore,
    AdscTrackDisplay adscTrackDisplay,
    ILogger logger)
    : INotificationHandler<AdscReportExpiredNotification>
{
    public async Task Handle(AdscReportExpiredNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("ADS-C report from {Callsign} via {StationId} expired", notification.Callsign, notification.StationId);

        // The aircraft can have a newer report from a different ATSU. Then the ADS-C track stays.
        await adscReportStore.Remove(notification.Callsign, notification.StationId, cancellationToken);
        adscTrackDisplay.Remove(notification.Callsign, notification.StationId);

        WeakReferenceMessenger.Default.Send(notification);
    }
}
