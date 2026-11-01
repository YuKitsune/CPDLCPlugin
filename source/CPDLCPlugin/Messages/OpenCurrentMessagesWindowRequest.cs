using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Configuration;
using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using System.Drawing;
using MediatR;

namespace CPDLCPlugin.Messages;

// When OpenedAutomatically is true, the window closes automatically when it has no messages.
public record OpenCurrentMessagesWindowRequest(bool OpenedAutomatically) : IRequest;

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
        // If the window is already open, tell it how it was opened
        WeakReferenceMessenger.Default.Send(request);

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
                    jurisdictionChecker,
                    closeWhenEmpty: request.OpenedAutomatically);

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
