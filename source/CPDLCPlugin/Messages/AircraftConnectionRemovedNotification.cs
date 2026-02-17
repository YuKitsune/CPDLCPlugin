using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AircraftConnectionRemovedNotification(string Callsign, string StationId) : INotification;

public class AircraftConnectionRemovedNotificationNotificationHandler(AircraftConnectionStore aircraftConnectionStore, ILogger logger)
    : INotificationHandler<AircraftConnectionRemovedNotification>
{
    public async Task Handle(AircraftConnectionRemovedNotification notification, CancellationToken cancellationToken)
    {
        logger.Verbose("Aircraft {Callsign} disconnected from {StationId}", notification.Callsign, notification.StationId);

        if (!await aircraftConnectionStore.Remove(notification.Callsign, notification.StationId, cancellationToken))
        {
            logger.Warning("Aircraft {Callsign} was not tracked on {StationId}", notification.Callsign, notification.StationId);
        }

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}
