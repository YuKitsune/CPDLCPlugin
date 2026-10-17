using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record OpenADSContractWindowRequest(string? Callsign = null) : IRequest;

public class OpenADSContractWindowRequestHandler(
    WindowManager windowManager,
    Plugin plugin,
    AdscContractStore adscContractStore,
    AdscReportStore adscReportStore,
    AtsuSettingsStore atsuSettingsStore,
    IMediator mediator,
    IGuiInvoker guiInvoker,
    IErrorReporter errorReporter,
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

        ADSContractViewModel? viewModel = null;
        windowManager.FocusOrCreateForm(
            WindowKeys.ADSContract,
            "ADS CONTRACT WINDOW",
            windowHandle =>
            {
                viewModel = new ADSContractViewModel(
                    plugin,
                    adscContractStore,
                    adscReportStore,
                    atsuSettingsStore,
                    mediator,
                    guiInvoker,
                    errorReporter,
                    request.Callsign);

                return new ADSContractView(viewModel);
            },
            onClosed: () => viewModel?.Dispose());

        return Task.CompletedTask;
    }
}
