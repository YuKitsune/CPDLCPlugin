using CPDLCPlugin.Configuration;
using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using MediatR;
using vatSysToolbox;
using vatSysToolbox.Windows;

namespace CPDLCPlugin.Messages;

public record OpenConnectionRequestWindowRequest(string Callsign) : IRequest;

public class OpenConnectionRequestWindowRequestHandler(
    WindowManager windowManager,
    PluginConfiguration pluginConfiguration,
    IMediator mediator,
    Plugin plugin,
    IErrorReporter errorReporter)
    : IRequestHandler<OpenConnectionRequestWindowRequest>
{
    public Task Handle(OpenConnectionRequestWindowRequest request, CancellationToken cancellationToken)
    {
        // Each window is specific to a callsign, so we always want a fresh window
        windowManager.Close(WindowKeys.ConnectionRequest);

        windowManager.OpenOrFocus(
            WindowKeys.ConnectionRequest,
            "CPDLC Connection Request",
            windowHandle =>
            {
                var currentStationIdentifier = plugin.ConnectionManager?.StationIdentifier;
                var viewModel = new ConnectionRequestViewModel(
                    mediator,
                    errorReporter,
                    windowHandle,
                    request.Callsign,
                    pluginConfiguration.Stations,
                    string.IsNullOrEmpty(currentStationIdentifier) ? pluginConfiguration.Stations.First() : currentStationIdentifier!);

                var control = new ConnectionRequestView(viewModel);
                return control;
            },
            new WindowOptions { CanMinimise = false, CanSendToBack = false });

        return Task.CompletedTask;
    }
}
