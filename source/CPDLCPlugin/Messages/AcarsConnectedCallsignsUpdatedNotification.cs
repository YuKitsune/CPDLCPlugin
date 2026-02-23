using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AcarsConnectedCallsignsUpdatedNotification(string[] Callsigns) : INotification;

public class AcarsConnectedCallsignsUpdatedNotificationHandler(
    AcarsConnectedCallsignStore acarsConnectedCallsignStore,
    ILogger logger)
    : INotificationHandler<AcarsConnectedCallsignsUpdatedNotification>
{
    public async Task Handle(AcarsConnectedCallsignsUpdatedNotification notification, CancellationToken cancellationToken)
    {
        logger.Verbose("ACARS connected callsigns updated: {Count} callsigns", notification.Callsigns.Length);

        await acarsConnectedCallsignStore.Populate(notification.Callsigns, cancellationToken);

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}
