using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.Cpdlc.Downlinks;

public class AcknowledgeDownlinkCommandHandler(
    IDialogueRepository dialogueRepository,
    IClock clock,
    IPublisher publisher,
    ILogger logger)
    : IRequestHandler<AcknowledgeDownlinkCommand>
{
    public async Task Handle(AcknowledgeDownlinkCommand request, CancellationToken cancellationToken)
    {
        var dialogue = await dialogueRepository.FindById(request.DialogueId, cancellationToken);
        if (dialogue is null)
        {
            logger.Warning("Dialogue not found for downlink acknowledgment: {DialogueId}", request.DialogueId);
            throw new InvalidOperationException($"Dialogue not found: {request.DialogueId}");
        }

        dialogue.AcknowledgeDownlink(request.DownlinkMessageId, clock.UtcNow());
        logger.Information(
            "Acknowledged downlink {MessageId} in dialogue {DialogueId} for {AircraftCallsign}",
            request.DownlinkMessageId,
            request.DialogueId,
            dialogue.AircraftCallsign);

        await publisher.Publish(
            new DialogueChangedNotification(dialogue),
            cancellationToken);
    }
}
