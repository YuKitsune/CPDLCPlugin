using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record ConnectedNotification : INotification;

public class ConnectedNotificationHandler(
    Plugin plugin,
    DialogueStore dialogueStore,
    AircraftConnectionStore aircraftConnectionStore,
    AcarsStationStore acarsStationStore,
    ILogger logger)
    : INotificationHandler<ConnectedNotification>
{
    public async Task Handle(ConnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Connected to server");
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        // Load dialogues
        logger.Verbose("Loading all dialogues");
        var dialogues = await plugin.ConnectionManager.GetAllDialogues(cancellationToken);
        await dialogueStore.Populate(dialogues, cancellationToken);
        logger.Information("Loaded {DialogueCount} dialogue(s)", dialogues.Length);

        // Load aircraft connections
        logger.Verbose("Loading all aircraft connections");
        var connectedAircraft = await plugin.ConnectionManager.GetConnectedAircraft(cancellationToken);
        await aircraftConnectionStore.Populate(connectedAircraft, cancellationToken);
        logger.Information("Loaded {ConnectionCount} aircraft connection(s)", connectedAircraft.Length);

        // Load ACARS connected callsigns
        logger.Verbose("Loading ACARS connected callsigns");
        var acarsConnectedCallsigns = await plugin.ConnectionManager.GetAcarsConnectedCallsigns(cancellationToken);
        await acarsStationStore.Populate(acarsConnectedCallsigns, cancellationToken);
        logger.Information("Loaded {Count} ACARS connected callsign(s)", acarsConnectedCallsigns.Length);

        // Relay notification to UI
        WeakReferenceMessenger.Default.Send(notification);
    }
}
