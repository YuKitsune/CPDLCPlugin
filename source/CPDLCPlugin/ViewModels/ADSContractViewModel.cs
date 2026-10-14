using CommunityToolkit.Mvvm.ComponentModel;

namespace CPDLCPlugin.ViewModels;



enum TransmissionState
{
    None,
    Accepted,
    ContractSent,
    ContractRefused
}

public partial class ADSContractViewModel : ObservableObject
{
    [ObservableProperty] string _callsign;
    [ObservableProperty] int _currentPeriodMinutes;
    [ObservableProperty] DateTimeOffset _lastReport;

    [ObservableProperty] int _defaultPeriod;
    [ObservableProperty] TransmissionState _defaultPeriodTransmissionState;

    [ObservableProperty] int _selectedPeriod;
    [ObservableProperty] TransmissionState _selectedPeriodTransmissionState;
    [ObservableProperty] DateTimeOffset _selectedPeriodEndTime;

    [ObservableProperty] TransmissionState _demandContractTransmissionState;
}
