using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

// TODO: Tests

public class AcknowledgeUplinkCommandHandler(
    IDialogueRepository dialogueRepository,
    IClock clock,
    IPublisher publisher,
    ILogger logger)
    : IRequestHandler<AcknowledgeUplinkCommand>
{
    public async Task Handle(AcknowledgeUplinkCommand request, CancellationToken cancellationToken)
    {
        var dialogue = await dialogueRepository.FindById(request.DialogueId, cancellationToken);
        if (dialogue is null)
        {
            logger.Warning("Dialogue not found for uplink acknowledgment: {DialogueId}", request.DialogueId);
            throw new InvalidOperationException($"Dialogue not found: {request.DialogueId}");
        }

        var uplinkMessage = dialogue.Messages
            .OfType<UplinkMessage>()
            .FirstOrDefault(m => m.MessageId == request.UplinkMessageId);
        if (uplinkMessage is null)
        {
            logger.Warning("Uplink message not found: {MessageId}", request.UplinkMessageId);
            throw new InvalidOperationException($"Uplink message not found: {request.UplinkMessageId}");
        }
        
        // TODO: Find a nice way to encapsulate these two changes
        var now = clock.UtcNow();
        uplinkMessage.Close(now, manual: true);
        dialogue.TryClose(now);
        
        logger.Information(
            "Acknowledged uplink {MessageId} in dialogue {DialogueId} for {AircraftCallsign}",
            request.UplinkMessageId,
            request.DialogueId,
            dialogue.AircraftCallsign);

        await publisher.Publish(
            new DialogueChangedNotification(dialogue),
            cancellationToken);
    }
}