using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CPDLCPlugin.Messages;
using MediatR;

namespace CPDLCPlugin.ViewModels;

public partial class ConnectionRequestViewModel : ObservableObject
{
    readonly IMediator _mediator;
    readonly IErrorReporter _errorReporter;
    readonly IWindowHandle _windowHandle;

    [ObservableProperty] string callsign;
    [ObservableProperty] string[] availableStationIdentifiers;
    [ObservableProperty] string selectedStationIdentifier;

    public ConnectionRequestViewModel(
        IMediator mediator,
        IErrorReporter errorReporter,
        IWindowHandle windowHandle,
        string callsign,
        string[] availableStationIdentifiers,
        string selectedStationIdentifier)
    {
        _mediator = mediator;
        _errorReporter = errorReporter;
        _windowHandle = windowHandle;
        Callsign = callsign;
        AvailableStationIdentifiers = availableStationIdentifiers;
        SelectedStationIdentifier = selectedStationIdentifier;
    }

    [RelayCommand]
    async Task Confirm()
    {
        try
        {
            await _mediator.Send(new SendConnectionRequestRequest(Callsign, SelectedStationIdentifier));
            _windowHandle.Close();
        }
        catch (Exception e)
        {
            _errorReporter.ReportError(e);
        }
    }

    [RelayCommand]
    void Cancel()
    {
        _windowHandle.Close();
    }
}
