using CommunityToolkit.Mvvm.Messaging;
using CPDLCServer.Contracts;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AircraftConnectionUpdatedNotification(AircraftConnectionDto AircraftConnectionDto) : INotification;

public class AircraftConnectedNotificationHandler(AircraftConnectionStore aircraftConnectionStore, ILogger logger)
    : INotificationHandler<AircraftConnectionUpdatedNotification>
{
    public async Task Handle(AircraftConnectionUpdatedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Upserting connection for {Callsign} ({DataAuthorityState})",
            notification.AircraftConnectionDto.Callsign,
            notification.AircraftConnectionDto.DataAuthorityState);

        await aircraftConnectionStore.Upsert(notification.AircraftConnectionDto, cancellationToken);

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}
