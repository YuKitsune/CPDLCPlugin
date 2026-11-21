using CPDLCServer.Infrastructure;
using CPDLCServer.Settings;
using MediatR;

namespace CPDLCServer.Cpdlc.Dialogues;

public class PurgeExpiredCpdlcDataRequestHandler(
    IDialogueRepository dialogueRepository,
    IServerSettingsProvider settingsProvider,
    IClock clock,
    ILogger logger)
    : IRequestHandler<PurgeExpiredCpdlcDataRequest>
{
    public async Task Handle(PurgeExpiredCpdlcDataRequest request, CancellationToken cancellationToken)
    {
        var settings = await settingsProvider.Load(cancellationToken);
        var retention = TimeSpan.FromHours(settings.DialogueRetentionHours);
        var now = clock.UtcNow();

        // The dialogue is purged when the retention period ends, also when it is open or the aircraft is connected
        var purgedCount = await dialogueRepository.RemoveOpenedBefore(now - retention, cancellationToken);
        if (purgedCount > 0)
            logger.Information("Purging {DialogueCount} dialogue(s) older than {Retention}", purgedCount, retention);
    }
}
