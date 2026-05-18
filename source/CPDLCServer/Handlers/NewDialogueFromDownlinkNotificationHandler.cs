using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class NewDialogueFromDownlinkNotificationHandler(
    IDialogueRepository dialogueRepository,
    IPublisher publisher,
    ILogger logger)
    : INotificationHandler<NewDialogueFromDownlinkNotification>
{
    public async Task Handle(NewDialogueFromDownlinkNotification notification, CancellationToken cancellationToken)
    {
        var dialogue = new Dialogue(notification.Downlink.Sender, notification.Downlink);
        await dialogueRepository.Add(dialogue, cancellationToken);
        logger.Information("Dialogue {DialogueId} created for downlink from {Callsign}",
            dialogue.Id, notification.Downlink.Sender);

        await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);
    }
}
