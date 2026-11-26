using CPDLCServer.Cpdlc.Dialogues;
using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

// The plugin sends this command when it updated the FDR from an uplink.
public class MarkUplinkActionedCommandHandler(
    IDialogueRepository dialogueRepository,
    IPublisher publisher,
    ILogger logger)
    : IRequestHandler<MarkUplinkActionedCommand>
{
    public async Task Handle(MarkUplinkActionedCommand request, CancellationToken cancellationToken)
    {
        var dialogue = await dialogueRepository.FindById(request.DialogueId, cancellationToken);
        if (dialogue is null)
        {
            logger.Warning("Dialogue not found for actioned uplink: {DialogueId}", request.DialogueId);
            throw new InvalidOperationException($"Dialogue not found: {request.DialogueId}");
        }

        dialogue.MarkUplinkActioned(request.UplinkMessageId);

        logger.Information(
            "Marked uplink {MessageId} in dialogue {DialogueId} for {AircraftCallsign} as actioned",
            request.UplinkMessageId,
            request.DialogueId,
            dialogue.AircraftCallsign);

        await publisher.Publish(
            new DialogueChangedNotification(dialogue),
            cancellationToken);
    }
}
