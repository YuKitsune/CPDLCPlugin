using CPDLCServer.Contracts;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AdscReportReceivedNotification(AdscReportDto AdscReportDto) : INotification;

public class AdscReportReceivedNotificationHandler(
    AdscReportStore adscReportStore,
    AdscTrackDisplay adscTrackDisplay,
    ILogger logger)
    : INotificationHandler<AdscReportReceivedNotification>
{
    public async Task Handle(AdscReportReceivedNotification notification, CancellationToken cancellationToken)
    {
        var report = notification.AdscReportDto;
        logger.Information(
            "ADS-C report from {Callsign} via {StationId}: {Latitude} {Longitude} {Altitude} at {Timestamp}",
            report.Callsign,
            report.StationId,
            report.Latitude,
            report.Longitude,
            report.Altitude,
            report.Timestamp);

        await adscReportStore.Add(report, cancellationToken);

        // The server sends reports only for active contracts
        adscTrackDisplay.Show(report);
    }
}
