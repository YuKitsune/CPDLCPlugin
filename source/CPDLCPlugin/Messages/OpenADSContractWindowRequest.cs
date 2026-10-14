using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using MediatR;

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
    IErrorReporter errorReporter)
    : IRequestHandler<OpenADSContractWindowRequest>
{
    public Task Handle(OpenADSContractWindowRequest request, CancellationToken cancellationToken)
    {
        windowManager.FocusOrCreateWindow(
            WindowKeys.ADSContract,
            windowHandle =>
            {
                var viewModel = new ADSContractViewModel(
                    plugin,
                    adscContractStore,
                    adscReportStore,
                    atsuSettingsStore,
                    mediator,
                    guiInvoker,
                    errorReporter,
                    request.Callsign);

                return new ADSContractWindow(viewModel);
            });

        return Task.CompletedTask;
    }
}
