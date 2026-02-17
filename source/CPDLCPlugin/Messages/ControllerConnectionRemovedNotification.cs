using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record ControllerConnectionRemovedNotification(string Callsign) : INotification;

public class ControllerConnectionRemovedNotificationHandler(ControllerConnectionStore controllerConnectionStore, ILogger logger)
    : INotificationHandler<ControllerConnectionRemovedNotification>
{
    public async Task Handle(ControllerConnectionRemovedNotification notification, CancellationToken cancellationToken)
    {
        logger.Verbose("Controller {Callsign} disconnected", notification.Callsign);

        if (!await controllerConnectionStore.Remove(notification.Callsign, cancellationToken))
        {
            logger.Warning("Controller {Callsign} was not tracked", notification.Callsign);
        }
    }
}
