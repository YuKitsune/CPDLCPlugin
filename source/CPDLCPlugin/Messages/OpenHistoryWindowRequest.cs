using CPDLCPlugin.Configuration;
using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using System.Windows;
using System.Windows.Forms;
using MediatR;
using vatSysToolbox;
using vatSysToolbox.Windows;

namespace CPDLCPlugin.Messages;

public record OpenHistoryWindowRequest(string? Callsign = null) : IRequest;

public class OpenHistoryWindowRequestHandler(
    WindowManager windowManager,
    PluginConfiguration pluginConfiguration,
    DialogueStore dialogueStore,
    IErrorReporter errorReporter,
    IGuiInvoker guiInvoker,
    WindowEvents windowEvents)
    : IRequestHandler<OpenHistoryWindowRequest>
{
    public Task Handle(OpenHistoryWindowRequest request, CancellationToken cancellationToken)
    {
        windowManager.OpenOrFocus(
            WindowKeys.History,
            "CPDLC History",
            windowHandle =>
            {
                windowEvents.Broadcast(windowHandle, CpdlcMessages.HistoryWindowOpened, CpdlcMessages.HistoryWindowClosed);

                var viewModel = new HistoryViewModel(
                    pluginConfiguration,
                    dialogueStore,
                    guiInvoker,
                    errorReporter,
                    request.Callsign);

                return new HistoryView(viewModel, windowHandle);
            },
            new WindowOptions
            {
                // Only the height can change
                Size = WindowSize.Absolute(467, 320),
                MinimumSize = new Size(467, 24),
                MaximumSize = new Size(467, SystemInformation.VirtualScreen.Height)
            });

        return Task.CompletedTask;
    }
}
