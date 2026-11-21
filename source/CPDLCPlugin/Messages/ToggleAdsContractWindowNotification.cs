using CPDLCPlugin.Windows;
using MediatR;
using vatsys;
using vatSysToolbox.BackgroundWork;
using vatSysToolbox.MediatR;
using vatSysToolbox.Windows;

namespace CPDLCPlugin.Messages;

// Other plugins send this message to the mailbox of the plugin. See CpdlcMessages.ToggleAdsContractWindow.
public record ToggleAdsContractWindowNotification : INotification;

// The messenger receives the messages on a background thread. The menu items open the windows on the work queue, thus
// this handler uses the work queue too.
[PluginMessageHandler(CpdlcMessages.ToggleAdsContractWindow)]
public class ToggleAdsContractWindowNotificationHandler(WindowManager windowManager, WorkQueue workQueue, IMediator mediator)
    : INotificationHandler<ToggleAdsContractWindowNotification>
{
    public Task Handle(ToggleAdsContractWindowNotification notification, CancellationToken cancellationToken)
    {
        workQueue.Enqueue(async workCancellationToken =>
        {
            if (windowManager.IsOpen(WindowKeys.ADSContract))
            {
                windowManager.Close(WindowKeys.ADSContract);
                return;
            }

            var selectedCallsign = MMI.SelectedTrack?.GetFDR()?.Callsign;
            await mediator.Send(new OpenADSContractWindowRequest(selectedCallsign), workCancellationToken);
        });

        return Task.CompletedTask;
    }
}
