using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public class DisconnectedNotification : INotification;

public class DisconnectedNotificationBridge(
    DialogueStore dialogueStore,
    AircraftConnectionStore aircraftConnectionStore,
    ControllerConnectionStore controllerConnectionStore,
    ILogger logger) : INotificationHandler<DisconnectedNotification>
{
    public async Task Handle(DisconnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Clearing dialogue store");
        await dialogueStore.Clear(cancellationToken);

        logger.Information("Clearing aircraft connection store");
        await aircraftConnectionStore.Clear(cancellationToken);

        logger.Information("Clearing controller connection store");
        await controllerConnectionStore.Clear(cancellationToken);

        WeakReferenceMessenger.Default.Send(notification);
    }
}
