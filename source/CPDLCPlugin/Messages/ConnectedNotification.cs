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
    AdscContractStore adscContractStore,
    AdscReportStore adscReportStore,
    AdscTrackDisplay adscTrackDisplay,
    IJurisdictionChecker jurisdictionChecker,
    IMediator mediator,
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
        var acarsConnectedCallsigns = await plugin.ConnectionManager.GetAcarsStations(cancellationToken);
        await acarsStationStore.Populate(acarsConnectedCallsigns, cancellationToken);
        logger.Information("Loaded {Count} ACARS connected callsign(s)", acarsConnectedCallsigns.Length);

        // Load ADS-C contracts
        logger.Verbose("Loading all ADS-C contracts");
        var adscContracts = await plugin.ConnectionManager.GetAdscContracts(cancellationToken);
        await adscContractStore.Populate(adscContracts, cancellationToken);
        logger.Information("Loaded {ContractCount} ADS-C contract(s)", adscContracts.Length);

        // Load ADS-C reports
        logger.Verbose("Loading all ADS-C reports");
        var adscReports = await plugin.ConnectionManager.GetAdscReports(cancellationToken);
        await adscReportStore.Populate(adscReports, cancellationToken);
        foreach (var adscReport in adscReports)
        {
            adscTrackDisplay.Show(adscReport);
        }

        logger.Information("Loaded {ReportCount} ADS-C report(s)", adscReports.Length);

        // Relay notification to UI
        WeakReferenceMessenger.Default.Send(notification);

        // If there are dialogues relevant to the controller, open the current messages window
        var hasDialoguesToDisplay = dialogues.Any(d => !d.IsArchived && jurisdictionChecker.ShouldDisplayDialogue(d));
        if (hasDialoguesToDisplay)
        {
            await mediator.Send(new OpenCurrentMessagesWindowRequest(OpenedAutomatically: true), cancellationToken);
        }
    }
}
