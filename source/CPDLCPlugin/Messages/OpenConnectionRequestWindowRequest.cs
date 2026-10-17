using CPDLCPlugin.Configuration;
using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using MediatR;

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
        windowManager.TryRemoveWindow(WindowKeys.ConnectionRequest);

        windowManager.FocusOrCreateWindow(
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
            configureForm: form =>
            {
                form.HasMinimizeButton = false;
                form.MinimizeBox = false;
                form.HasCloseButton = false;
                form.HasSendBackButton = false;
            });

        return Task.CompletedTask;
    }
}
