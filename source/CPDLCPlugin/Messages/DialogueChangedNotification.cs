using CommunityToolkit.Mvvm.Messaging;
using CPDLCServer.Contracts;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record DialogueChangedNotification(DialogueDto Dialogue) : INotification;

public class DialogueChangedNotificationHandler(DialogueStore dialogueStore, ILogger logger)
    : INotificationHandler<DialogueChangedNotification>
{
    public async Task Handle(DialogueChangedNotification notification, CancellationToken cancellationToken)
    {
        logger.Verbose("Upserting dialogue {DialogueId}", notification.Dialogue.Id);

        await dialogueStore.Upsert(notification.Dialogue, cancellationToken);

        WeakReferenceMessenger.Default.Send(notification);
    }
}
