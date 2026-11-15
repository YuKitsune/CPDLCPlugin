using CPDLCServer.Cpdlc.Dialogues;
using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public class MarkUplinkTransmissionFailedCommandHandler(
    IDialogueRepository dialogueRepository,
    IPublisher publisher,
    ILogger logger)
    : IRequestHandler<MarkUplinkTransmissionFailedCommand>
{
    public async Task Handle(MarkUplinkTransmissionFailedCommand request, CancellationToken cancellationToken)
    {
        var dialogue = await dialogueRepository.FindById(request.DialogueId, cancellationToken);
        if (dialogue is null)
        {
            logger.Warning(
                "Cannot mark uplink {MessageId} as failed: dialogue {DialogueId} not found",
                request.UplinkMessageId,
                request.DialogueId);
            return;
        }

        dialogue.MarkTransmissionFailed(request.UplinkMessageId);

        logger.Information(
            "Marked uplink {MessageId} in dialogue {DialogueId} as failed",
            request.UplinkMessageId,
            request.DialogueId);

        await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);
    }
}
