using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using MediatR;
using Serilog;
using vatSysToolbox;
using vatSysToolbox.Windows;

namespace CPDLCPlugin.Messages;

public record OpenADSContractWindowRequest(string? Callsign = null) : IRequest;

public class OpenADSContractWindowRequestHandler(
    WindowManager windowManager,
    Plugin plugin,
    AdscContractStore adscContractStore,
    AdscReportStore adscReportStore,
    IMediator mediator,
    IGuiInvoker guiInvoker,
    IErrorReporter errorReporter,
    WindowEvents windowEvents,
    ILogger logger)
    : IRequestHandler<OpenADSContractWindowRequest>
{
    public Task Handle(OpenADSContractWindowRequest request, CancellationToken cancellationToken)
    {
        // The window shows and changes contracts that the server holds
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Cannot open the ADS Contract window: not connected to server");
            throw new Exception("Cannot open the ADS Contract window: not connected to the CPDLC server");
        }

        windowManager.OpenOrFocus(
            WindowKeys.ADSContract,
            "ADS Contract",
            windowHandle =>
            {
                windowEvents.Broadcast(windowHandle, CpdlcMessages.AdsContractWindowOpened, CpdlcMessages.AdsContractWindowClosed);

                var viewModel = new ADSContractViewModel(
                    plugin,
                    adscContractStore,
                    adscReportStore,
                    mediator,
                    guiInvoker,
                    errorReporter,
                    request.Callsign);

                return new ADSContractView(viewModel);
            },
            new WindowOptions { CanMinimise = false });

        return Task.CompletedTask;
    }
}
