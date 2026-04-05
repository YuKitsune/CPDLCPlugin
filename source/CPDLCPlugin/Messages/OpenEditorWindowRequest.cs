using CPDLCPlugin.Configuration;
using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record OpenEditorWindowRequest(string Callsign) : IRequest;

public class OpenEditorWindowRequestHandler(
    WindowManager windowManager,
    PluginConfiguration pluginConfiguration,
    IMediator mediator,
    DialogueStore dialogueStore,
    SuspendedMessageStore suspendedMessageStore,
    IErrorReporter errorReporter,
    IGuiInvoker guiInvoker,
    ILogger logger)
    : IRequestHandler<OpenEditorWindowRequest>
{
    public Task Handle(OpenEditorWindowRequest request, CancellationToken cancellationToken)
    {
        // Close any existing editor window before opening a new one
        // Each editor is specific to a callsign, so we always want a fresh window
        windowManager.TryRemoveWindow(WindowKeys.Editor);

        windowManager.FocusOrCreateWindow(
            WindowKeys.Editor,
            windowHandle =>
            {
                var viewModel = new EditorViewModel(
                    request.Callsign,
                    dialogueStore,
                    pluginConfiguration.UplinkMessages,
                    suspendedMessageStore,
                    mediator,
                    errorReporter,
                    guiInvoker,
                    windowHandle,
                    logger);

                var control = new EditorWindow(viewModel);
                return control;
            });

        return Task.CompletedTask;
    }
}
