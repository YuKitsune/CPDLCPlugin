using CPDLCPlugin.Windows;
using MediatR;
using vatSysToolbox.BackgroundWork;
using vatSysToolbox.MediatR;
using vatSysToolbox.Windows;

namespace CPDLCPlugin.Messages;

// Other plugins send this message to the mailbox of the plugin. See CpdlcMessages.ToggleSetupWindow.
public record ToggleSetupWindowNotification : INotification;

// The messenger receives the messages on a background thread. The menu items open the windows on the work queue, thus
// this handler uses the work queue too.
[PluginMessageHandler(CpdlcMessages.ToggleSetupWindow)]
public class ToggleSetupWindowNotificationHandler(WindowManager windowManager, WorkQueue workQueue, IMediator mediator)
    : INotificationHandler<ToggleSetupWindowNotification>
{
    public Task Handle(ToggleSetupWindowNotification notification, CancellationToken cancellationToken)
    {
        workQueue.Enqueue(async workCancellationToken =>
        {
            if (windowManager.IsOpen(WindowKeys.Setup))
            {
                windowManager.Close(WindowKeys.Setup);
                return;
            }

            await mediator.Send(new OpenSetupWindowRequest(), workCancellationToken);
        });

        return Task.CompletedTask;
    }
}
