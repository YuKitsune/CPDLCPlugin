using CPDLCPlugin.Configuration;
using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using MediatR;

namespace CPDLCPlugin.Messages;

public record OpenSetupWindowRequest : IRequest;

public class OpenSetupWindowRequestHandler(
    WindowManager windowManager,
    PluginConfiguration pluginConfiguration,
    IMediator mediator,
    Plugin plugin,
    IErrorReporter errorReporter)
    : IRequestHandler<OpenSetupWindowRequest>
{
    public Task Handle(OpenSetupWindowRequest request, CancellationToken cancellationToken)
    {
        windowManager.FocusOrCreateWindow(
            WindowKeys.Setup,
            windowHandle =>
            {
                // Create the view model with current configuration and connection state
                var isConnected = plugin.ConnectionManager?.IsConnected ?? false;
                var viewModel = new SetupViewModel(
                    mediator,
                    errorReporter,
                    windowHandle,
                    plugin.ConnectionManager?.ServerEndpoint ?? pluginConfiguration.ServerEndpoint,
                    pluginConfiguration.Stations,
                    isConnected ? plugin.ConnectionManager!.StationIdentifier : pluginConfiguration.Stations.First(),
                    isConnected);

                var control = new SetupWindow(viewModel);
                return control;
            });

        return Task.CompletedTask;
    }
}
