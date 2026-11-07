using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.Cpdlc.Dialogues;

public class ArchiveCompletedDialoguesRequestHandler(
    IDialogueRepository dialogueRepository,
    IClock clock,
    IPublisher publisher,
    ILogger logger)
    : IRequestHandler<ArchiveCompletedDialoguesRequest>
{
    readonly TimeSpan _archiveDelay = TimeSpan.FromSeconds(10);

    public async Task Handle(ArchiveCompletedDialoguesRequest request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow();
        var dialogues = await dialogueRepository.FindClosedAndNotArchived(cancellationToken);

        logger.Verbose("Checking for completed dialogues to archive");

        foreach (var dialogue in dialogues)
        {
            // Ensure all messages are acknowledged before archiving
            if (!dialogue.Messages.All(m => m.IsAcknowledged))
                continue;

            // Find the last acknowledged time across all messages
            var lastAckTime = dialogue.Messages.Max(m => m.Acknowledged);

            // Take the later of the last acknowledgement time or dialogue closed time
            var latest = lastAckTime.Value > dialogue.Closed
                ? lastAckTime.Value
                : dialogue.Closed;

            var archiveTime = latest.Value.Add(_archiveDelay);
            if (now < archiveTime)
                continue;

            logger.Verbose("Archiving dialogue for {Callsign}", dialogue.AircraftCallsign);
            dialogue.Archive(now);

            await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);
        }
    }
}
