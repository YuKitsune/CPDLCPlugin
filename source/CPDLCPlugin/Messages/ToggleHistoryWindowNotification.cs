using CPDLCPlugin.Windows;
using MediatR;
using vatsys;
using vatSysToolbox.BackgroundWork;
using vatSysToolbox.MediatR;
using vatSysToolbox.Windows;

namespace CPDLCPlugin.Messages;

// Other plugins send this message to the mailbox of the plugin. See CpdlcMessages.ToggleHistoryWindow.
public record ToggleHistoryWindowNotification : INotification;

// The messenger receives the messages on a background thread. The menu items open the windows on the work queue, thus
// this handler uses the work queue too.
[PluginMessageHandler(CpdlcMessages.ToggleHistoryWindow)]
public class ToggleHistoryWindowNotificationHandler(WindowManager windowManager, WorkQueue workQueue, IMediator mediator)
    : INotificationHandler<ToggleHistoryWindowNotification>
{
    public Task Handle(ToggleHistoryWindowNotification notification, CancellationToken cancellationToken)
    {
        workQueue.Enqueue(async workCancellationToken =>
        {
            if (windowManager.IsOpen(WindowKeys.History))
            {
                windowManager.Close(WindowKeys.History);
                return;
            }

            var selectedCallsign = MMI.SelectedTrack?.GetFDR()?.Callsign;
            await mediator.Send(new OpenHistoryWindowRequest(selectedCallsign), workCancellationToken);
        });

        return Task.CompletedTask;
    }
}
