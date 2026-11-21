using CPDLCPlugin.Configuration;
using CPDLCPlugin.Server;
using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using MediatR;
using vatSysToolbox;
using vatSysToolbox.Windows;

namespace CPDLCPlugin.Messages;

public record OpenSetupWindowRequest : IRequest;

public class OpenSetupWindowRequestHandler(
    WindowManager windowManager,
    PluginConfiguration pluginConfiguration,
    IMediator mediator,
    Plugin plugin,
    IErrorReporter errorReporter,
    AuthenticationState authenticationState,
    IGuiInvoker guiInvoker,
    WindowEvents windowEvents)
    : IRequestHandler<OpenSetupWindowRequest>
{
    public Task Handle(OpenSetupWindowRequest request, CancellationToken cancellationToken)
    {
        windowManager.OpenOrFocus(
            WindowKeys.Setup,
            "CPDLC Setup",
            windowHandle =>
            {
                windowEvents.Broadcast(windowHandle, CpdlcMessages.SetupWindowOpened, CpdlcMessages.SetupWindowClosed);

                // Create the view model with current configuration and connection state
                var isConnected = plugin.ConnectionManager?.IsConnected ?? false;
                var viewModel = new SetupViewModel(
                    mediator,
                    errorReporter,
                    windowHandle,
                    authenticationState,
                    guiInvoker,
                    plugin.ConnectionManager?.ServerEndpoint ?? pluginConfiguration.ServerEndpoint,
                    pluginConfiguration.Stations,
                    isConnected ? plugin.ConnectionManager!.StationIdentifier : pluginConfiguration.Stations.First(),
                    isConnected);

                var control = new SetupView(viewModel);
                return control;
            });

        return Task.CompletedTask;
    }
}
