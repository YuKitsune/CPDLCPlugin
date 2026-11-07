using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.Cpdlc.Dialogues;

public class PurgeExpiredCpdlcDataRequestHandler(
    IDialogueRepository dialogueRepository,
    IClock clock,
    ILogger logger)
    : IRequestHandler<PurgeExpiredCpdlcDataRequest>
{
    readonly TimeSpan _purgeTimeout = TimeSpan.FromHours(24);

    public async Task Handle(PurgeExpiredCpdlcDataRequest request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow();

        // I don't care if it's closed or not. It's been long enough.
        var oldDialogues = await dialogueRepository.FindOpenedBefore(now - _purgeTimeout, cancellationToken);
        foreach (var dialogue in oldDialogues)
        {
            await dialogueRepository.Remove(dialogue, cancellationToken);
            logger.Information("Purging dialogue"); // TODO: Dialogue identifier
        }
    }
}
