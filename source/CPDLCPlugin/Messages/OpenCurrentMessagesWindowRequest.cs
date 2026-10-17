using CPDLCPlugin.Configuration;
using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using System.Drawing;
using MediatR;

namespace CPDLCPlugin.Messages;

public record OpenCurrentMessagesWindowRequest : IRequest;

public class OpenCurrentMessagesWindowRequestHandler(
    WindowManager windowManager,
    PluginConfiguration pluginConfiguration,
    IMediator mediator,
    DialogueStore dialogueStore,
    IErrorReporter errorReporter,
    IGuiInvoker guiInvoker,
    IJurisdictionChecker jurisdictionChecker)
    : IRequestHandler<OpenCurrentMessagesWindowRequest>
{
    public Task Handle(OpenCurrentMessagesWindowRequest request, CancellationToken cancellationToken)
    {
        windowManager.FocusOrCreateWindow(
            WindowKeys.CurrentMessages,
            "Current Messages",
            windowHandle =>
            {
                var viewModel = new CurrentMessagesViewModel(
                    pluginConfiguration,
                    dialogueStore,
                    mediator,
                    guiInvoker,
                    errorReporter,
                    windowHandle,
                    jurisdictionChecker);

                return new CurrentMessagesView(viewModel, windowHandle);
            },
            sizeToContent: false,
            configureForm: form =>
            {
                form.Size = new Size(467, 320);
                form.MinimumSize = new Size(140, 24);
            });

        return Task.CompletedTask;
    }
}
