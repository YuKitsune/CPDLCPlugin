using CPDLCServer.Contracts;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record ControllerConnectionUpdatedNotification(ControllerConnectionDto ControllerConnectionDto) : INotification;

public class ControllerConnectionUpdatedNotificationHandler(ControllerConnectionStore controllerConnectionStore, ILogger logger)
    : INotificationHandler<ControllerConnectionUpdatedNotification>
{
    public async Task Handle(ControllerConnectionUpdatedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Upserting connection for controller {Callsign}",
            notification.ControllerConnectionDto.Callsign);

        await controllerConnectionStore.Upsert(notification.ControllerConnectionDto, cancellationToken);
    }
}
