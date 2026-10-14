using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Messages;
using CPDLCServer.Contracts;

namespace CPDLCPlugin.ViewModels;

public partial class ADSContractViewModel : ObservableObject,
    IRecipient<AdscContractUpdatedNotification>,
    IRecipient<AdscContractRemovedNotification>,
    IRecipient<AdscReportReceivedNotification>,
    IDisposable
{
    readonly Plugin _plugin;
    readonly AdscContractStore _adscContractStore;
    readonly AdscReportStore _adscReportStore;
    readonly AtsuSettingsStore _atsuSettingsStore;
    readonly IGuiInvoker _guiInvoker;
    readonly IErrorReporter _errorReporter;
    bool _disposed;

    public ADSContractViewModel(
        Plugin plugin,
        AdscContractStore adscContractStore,
        AdscReportStore adscReportStore,
        AtsuSettingsStore atsuSettingsStore,
        IGuiInvoker guiInvoker,
        IErrorReporter errorReporter,
        string? initialCallsign = null)
    {
        _plugin = plugin;
        _adscContractStore = adscContractStore;
        _adscReportStore = adscReportStore;
        _atsuSettingsStore = atsuSettingsStore;
        _guiInvoker = guiInvoker;
        _errorReporter = errorReporter;

        _callsign = initialCallsign ?? string.Empty;

        WeakReferenceMessenger.Default.Register<AdscContractUpdatedNotification>(this);
        WeakReferenceMessenger.Default.Register<AdscContractRemovedNotification>(this);
        WeakReferenceMessenger.Default.Register<AdscReportReceivedNotification>(this);

        if (!string.IsNullOrWhiteSpace(initialCallsign))
        {
            _ = LoadContractAsync();
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CallsignButtonText))]
    string _callsign;

    public string CallsignButtonText => string.IsNullOrWhiteSpace(Callsign) ? "ACID" : Callsign;

    [ObservableProperty] int? _currentPeriodMinutes;
    [ObservableProperty] DateTimeOffset? _lastReport;

    [ObservableProperty] int? _defaultPeriod;
    [ObservableProperty] AdscContractState? _defaultPeriodTransmissionState;

    [ObservableProperty] int? _selectedPeriod;
    [ObservableProperty] AdscContractState? _selectedPeriodTransmissionState;
    [ObservableProperty] DateTimeOffset? _selectedPeriodEndTime;

    [ObservableProperty] AdscContractState? _demandContractTransmissionState;

    async Task LoadContractAsync()
    {
        try
        {
            DefaultPeriod = (int?)_atsuSettingsStore.Settings?.DefaultAdscReportingInterval.TotalMinutes;

            if (string.IsNullOrWhiteSpace(Callsign))
            {
                ShowContract(null);
                LastReport = null;
                return;
            }

            // Show the contract that this station holds
            var stationId = _plugin.ConnectionManager?.StationIdentifier;
            var contract = (await _adscContractStore.All(CancellationToken.None))
                .FirstOrDefault(c => c.Callsign == Callsign && c.StationId == stationId);
            ShowContract(contract);

            var report = await _adscReportStore.Find(Callsign, CancellationToken.None);
            LastReport = report?.Timestamp;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    void ShowContract(AdscContractDto? contract)
    {
        CurrentPeriodMinutes = (int?)contract?.ReportingInterval?.TotalMinutes;

        DefaultPeriodTransmissionState = contract?.DefaultContractState;

        SelectedPeriod = (int?)contract?.SelectedReportingInterval?.TotalMinutes;
        SelectedPeriodTransmissionState = contract?.SelectedContractState;
        SelectedPeriodEndTime = contract?.SelectedContractEnds;

        DemandContractTransmissionState = contract?.DemandContractState;
    }

    [RelayCommand]
    async Task LoadContract()
    {
        try
        {
            Callsign = Callsign.Trim().ToUpperInvariant();
            await LoadContractAsync();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    public void Receive(AdscContractUpdatedNotification notification)
    {
        ReloadIfShown(notification.AdscContractDto.Callsign);
    }

    public void Receive(AdscContractRemovedNotification notification)
    {
        ReloadIfShown(notification.Callsign);
    }

    public void Receive(AdscReportReceivedNotification notification)
    {
        ReloadIfShown(notification.AdscReportDto.Callsign);
    }

    void ReloadIfShown(string callsign)
    {
        if (_disposed)
            return;

        _guiInvoker.InvokeOnGUI(mainForm =>
        {
            if (_disposed)
                return;

            try
            {
                if (callsign != Callsign)
                    return;

                _ = LoadContractAsync();
            }
            catch (Exception ex)
            {
                _errorReporter.ReportError(ex);
            }
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        WeakReferenceMessenger.Default.Unregister<AdscContractUpdatedNotification>(this);
        WeakReferenceMessenger.Default.Unregister<AdscContractRemovedNotification>(this);
        WeakReferenceMessenger.Default.Unregister<AdscReportReceivedNotification>(this);
        _disposed = true;
    }
}
