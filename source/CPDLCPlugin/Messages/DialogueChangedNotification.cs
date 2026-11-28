using CommunityToolkit.Mvvm.Messaging;
using CPDLCServer.Contracts;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record DialogueChangedNotification(DialogueDto Dialogue) : INotification;

public class DialogueChangedNotificationHandler(DialogueStore dialogueStore, IMediator mediator, ILogger logger)
    : INotificationHandler<DialogueChangedNotification>
{
    public async Task Handle(DialogueChangedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Upserting dialogue {DialogueId}", notification.Dialogue.Id);

        var previousDialogue = await dialogueStore.Find(notification.Dialogue.Id, cancellationToken);
        await dialogueStore.Upsert(notification.Dialogue, cancellationToken);

        WeakReferenceMessenger.Default.Send(notification);

        await PublishWilcos(previousDialogue, notification.Dialogue, cancellationToken);
    }

    // Publishes a WilcoReceivedNotification for each WILCO that was not in the previous version of the dialogue
    async Task PublishWilcos(DialogueDto? previousDialogue, DialogueDto dialogue, CancellationToken cancellationToken)
    {
        var previousDownlinkIds = previousDialogue?.Messages
            .OfType<DownlinkMessageDto>()
            .Select(d => d.MessageId)
            .ToArray() ?? [];

        var newWilcos = dialogue.Messages
            .OfType<DownlinkMessageDto>()
            .Where(d => !previousDownlinkIds.Contains(d.MessageId))
            .Where(d => d.MessageReference is not null && d.Elements.Any(e => e.Id == DownlinkElementIds.Wilco));

        foreach (var wilco in newWilcos)
        {
            var uplink = dialogue.Messages
                .OfType<UplinkMessageDto>()
                .FirstOrDefault(u => u.MessageId == wilco.MessageReference);
            if (uplink is null)
                continue;

            logger.Information("WILCO received for uplink {MessageId} in dialogue {DialogueId}", uplink.MessageId, dialogue.Id);
            await mediator.Publish(new WilcoReceivedNotification(dialogue, uplink), cancellationToken);
        }
    }
}
