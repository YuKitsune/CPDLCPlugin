using CPDLCPlugin.Configuration;
using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using System.Drawing;
using MediatR;

namespace CPDLCPlugin.Messages;

public record OpenHistoryWindowRequest(string? Callsign = null) : IRequest;

public class OpenHistoryWindowRequestHandler(
    WindowManager windowManager,
    PluginConfiguration pluginConfiguration,
    DialogueStore dialogueStore,
    IErrorReporter errorReporter,
    IGuiInvoker guiInvoker)
    : IRequestHandler<OpenHistoryWindowRequest>
{
    public Task Handle(OpenHistoryWindowRequest request, CancellationToken cancellationToken)
    {
        windowManager.FocusOrCreateWindow(
            WindowKeys.History,
            "CPDLC History",
            windowHandle =>
            {
                var viewModel = new HistoryViewModel(
                    pluginConfiguration,
                    dialogueStore,
                    guiInvoker,
                    errorReporter,
                    request.Callsign);

                return new HistoryView(viewModel, windowHandle);
            },
            sizeToContent: false,
            configureForm: form =>
            {
                // Only the height can change
                form.Size = new Size(467, 320);
                form.MinimumSize = new Size(467, 24);
                form.MaximumSize = new Size(467, 0);
            });

        return Task.CompletedTask;
    }
}
