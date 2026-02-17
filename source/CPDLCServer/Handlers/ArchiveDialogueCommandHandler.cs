using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class ArchiveDialogueCommandHandler(
    IDialogueRepository dialogueRepository,
    IClock clock,
    IPublisher publisher,
    ILogger logger)
    : IRequestHandler<ArchiveDialogueCommand>
{
    public async Task Handle(ArchiveDialogueCommand request, CancellationToken cancellationToken)
    {
        var dialogue = await dialogueRepository.FindById(request.DialogueId, cancellationToken);

        if (dialogue is null)
        {
            logger.Warning("Dialogue not found for archival: {DialogueId}", request.DialogueId);
            return; // Silently succeed - already archived or doesn't exist
        }

        if (dialogue.IsArchived)
        {
            logger.Verbose("Dialogue {DialogueId} is already archived", request.DialogueId);
            return; // Idempotent - already archived
        }

        dialogue.Archive(clock.UtcNow());

        logger.Information(
            "Manually archived dialogue {DialogueId} for {AircraftCallsign}",
            request.DialogueId,
            dialogue.AircraftCallsign);

        await publisher.Publish(
            new DialogueChangedNotification(dialogue),
            cancellationToken);
    }
}
