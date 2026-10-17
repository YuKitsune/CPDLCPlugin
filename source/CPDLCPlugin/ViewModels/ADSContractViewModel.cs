using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Messages;
using CPDLCServer.Contracts;
using MediatR;

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
    readonly IMediator _mediator;
    readonly IGuiInvoker _guiInvoker;
    readonly IErrorReporter _errorReporter;
    bool _disposed;

    public ADSContractViewModel(
        Plugin plugin,
        AdscContractStore adscContractStore,
        AdscReportStore adscReportStore,
        AtsuSettingsStore atsuSettingsStore,
        IMediator mediator,
        IGuiInvoker guiInvoker,
        IErrorReporter errorReporter,
        string? initialCallsign = null)
    {
        _plugin = plugin;
        _adscContractStore = adscContractStore;
        _adscReportStore = adscReportStore;
        _atsuSettingsStore = atsuSettingsStore;
        _mediator = mediator;
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

    // Example data for the XAML designer. Do not use at runtime.
    public ADSContractViewModel()
    {
        _plugin = null!;
        _adscContractStore = null!;
        _adscReportStore = null!;
        _atsuSettingsStore = null!;
        _mediator = null!;
        _guiInvoker = null!;
        _errorReporter = null!;

        _callsign = "QFA123AB";
        CurrentPeriodMinutes = 60;
        LastReport = DateTimeOffset.UtcNow.AddMinutes(-2);
        LastReportSource = AdscReportSource.VATSIM;

        DefaultPeriod = 10;
        DefaultPeriodTransmissionState = AdscContractState.Accepted;

        SelectedPeriod = 5;
        SelectedPeriodTransmissionState = AdscContractState.Sent;
        SelectedPeriodEndTime = DateTimeOffset.UtcNow.AddMinutes(60);

        DemandContractTransmissionState = AdscContractState.Rejected;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CallsignButtonText))]
    string _callsign;

    public string CallsignButtonText => string.IsNullOrWhiteSpace(Callsign) ? "ACID" : Callsign;

    [ObservableProperty] int? _currentPeriodMinutes;
    [ObservableProperty] DateTimeOffset? _lastReport;
    [ObservableProperty] AdscReportSource? _lastReportSource;

    [ObservableProperty] int? _defaultPeriod;
    [ObservableProperty] AdscContractState? _defaultPeriodTransmissionState;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedPeriodButtonText))]
    int? _selectedPeriod;

    public string SelectedPeriodButtonText => SelectedPeriod?.ToString() ?? "DEF";

    // The periods in minutes that a controller can select from the list. A controller can also type a different period.
    public int[] PeriodOptions { get; } = [3, 5, 10, 15, 20, 24, 30, 60];

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
                LastReportSource = null;
                return;
            }

            // Show the contract that this station holds
            var stationId = _plugin.ConnectionManager?.StationIdentifier;
            var contract = (await _adscContractStore.All(CancellationToken.None))
                .FirstOrDefault(c => c.Callsign == Callsign && c.StationId == stationId);
            ShowContract(contract);

            var report = await _adscReportStore.Find(Callsign, CancellationToken.None);
            LastReport = report?.Timestamp;
            LastReportSource = report?.Source;
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

    // A null period requests the default contract
    [RelayCommand]
    async Task SelectPeriod(int? minutes)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Callsign))
                return;

            var reportingInterval = minutes is null ? (TimeSpan?)null : TimeSpan.FromMinutes(minutes.Value);
            await _mediator.Send(new SelectAdscReportingIntervalRequest(Callsign, reportingInterval));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task SelectCustomPeriod(string text)
    {
        if (!int.TryParse(text, out var minutes) || minutes < 1)
        {
            _errorReporter.ReportError(new ArgumentException($"Period must be 1 to 99 minutes: {text}"));
            return;
        }

        await SelectPeriod(minutes);
    }

    [RelayCommand]
    async Task RequestDemandReport()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Callsign))
                return;

            await _mediator.Send(new RequestAdscDemandReportRequest(Callsign));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
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
