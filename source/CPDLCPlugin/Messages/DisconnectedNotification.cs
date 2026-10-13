using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public class DisconnectedNotification : INotification;

public class DisconnectedNotificationBridge(
    DialogueStore dialogueStore,
    AircraftConnectionStore aircraftConnectionStore,
    AcarsStationStore acarsStationStore,
    AdscContractStore adscContractStore,
    AdscReportStore adscReportStore,
    AtsuSettingsStore atsuSettingsStore,
    ILogger logger) : INotificationHandler<DisconnectedNotification>
{
    public async Task Handle(DisconnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Clearing dialogue store");
        await dialogueStore.Clear(cancellationToken);

        logger.Information("Clearing aircraft connection store");
        await aircraftConnectionStore.Clear(cancellationToken);

        logger.Information("Clearing ACARS station store");
        await acarsStationStore.Clear(cancellationToken);

        logger.Information("Clearing ADS-C contract store");
        await adscContractStore.Clear(cancellationToken);

        logger.Information("Clearing ADS-C report store");
        await adscReportStore.Clear(cancellationToken);

        logger.Information("Clearing ATSU settings");
        atsuSettingsStore.Clear();

        WeakReferenceMessenger.Default.Send(notification);
    }
}
