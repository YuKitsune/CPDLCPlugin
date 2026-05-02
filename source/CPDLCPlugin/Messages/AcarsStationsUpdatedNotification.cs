using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AcarsStationsUpdatedNotification(string[] Callsigns) : INotification;

public class AcarsStationsUpdatedNotificationHandler(
    AcarsStationStore acarsStationStore,
    ILogger logger)
    : INotificationHandler<AcarsStationsUpdatedNotification>
{
    public async Task Handle(AcarsStationsUpdatedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("ACARS connected callsigns updated: {Count} callsigns", notification.Callsigns.Length);

        await acarsStationStore.Populate(notification.Callsigns, cancellationToken);

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}
