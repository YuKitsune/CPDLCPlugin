using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class AircraftRepliedToUplinkNotificationHandler(
    IDialogueRepository dialogueRepository,
    IPublisher publisher,
    ILogger logger)
    : INotificationHandler<AircraftRepliedToUplinkNotification>
{
    public async Task Handle(AircraftRepliedToUplinkNotification notification, CancellationToken cancellationToken)
    {
        var downlink = notification.Downlink;
        var dialogue = await dialogueRepository.FindOpenDialogueByUplink(
            downlink.Sender,
            downlink.MessageReference!.Value,
            cancellationToken);

        if (dialogue is not null)
        {
            dialogue.AddMessage(downlink);
            logger.Information("Downlink from {Callsign} appended to dialogue {DialogueId}",
                downlink.Sender, dialogue.Id);
        }
        else
        {
            logger.Warning("No open dialogue found for uplink reference {MessageReference} from {Callsign} - starting new dialogue",
                downlink.MessageReference.Value, downlink.Sender);
            dialogue = new Dialogue(downlink.Sender, downlink);
            await dialogueRepository.Add(dialogue, cancellationToken);
        }

        await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);
    }
}
