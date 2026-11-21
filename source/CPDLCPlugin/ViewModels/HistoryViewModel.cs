using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Configuration;
using CPDLCPlugin.Messages;

namespace CPDLCPlugin.ViewModels;

public partial class HistoryViewModel : ObservableObject,
    IRecipient<DialogueChangedNotification>,
    IDisposable
{
    readonly PluginConfiguration _configuration;
    readonly DialogueStore _dialogueStore;
    readonly IGuiInvoker _guiInvoker;
    readonly IErrorReporter _errorReporter;
    bool _disposed;

    public HistoryViewModel(
        PluginConfiguration configuration,
        DialogueStore dialogueStore,
        IGuiInvoker guiInvoker,
        IErrorReporter errorReporter,
        string? initialCallsign = null)
    {
        _configuration = configuration;
        _dialogueStore = dialogueStore;
        _guiInvoker = guiInvoker;
        _errorReporter = errorReporter;

        _callsign = initialCallsign ?? string.Empty;

        WeakReferenceMessenger.Default.Register(this);

        // Initial load if we have a callsign
        if (!string.IsNullOrWhiteSpace(initialCallsign))
        {
            _ = LoadDialoguesAsync();
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CallsignButtonText))]
    string? _callsign;

    public string CallsignButtonText => string.IsNullOrWhiteSpace(Callsign) ? "ACID" : Callsign;

    [ObservableProperty]
    DialogueHistoryViewModel[] _dialogues = [];

    [ObservableProperty]
    HistoryMessageViewModel? _currentlyExtendedMessage;

    async Task LoadDialoguesAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Callsign))
            {
                Dialogues = [];
                return;
            }

            var dialogues = (await _dialogueStore.All(CancellationToken.None))
                .Where(d => d.AircraftCallsign == Callsign && d.IsArchived);

            var dialogueViewModels = dialogues
                .Select(d => new DialogueHistoryViewModel
                {
                    Messages = new ObservableCollection<HistoryMessageViewModel>(
                        d.Messages.Select(m => new HistoryMessageViewModel(m, _configuration.MaxDisplayMessageLength))),
                    FirstMessageTime = d.Messages.OrderBy(m => m.Time).First().Time
                })
                .ToArray();

            Dialogues = dialogueViewModels;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task LoadMessages()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Callsign))
            {
                Callsign = null;
                Dialogues = [];
                return;
            }

            Callsign = Callsign.Trim().ToUpperInvariant();
            await LoadDialoguesAsync();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    public void Receive(DialogueChangedNotification notification)
    {
        // The dialogues change for all the aircraft. Do not post to the UI thread for an aircraft that this window does
        // not show. The UI thread checks again, because the callsign can change before the action runs.
        if (_disposed || notification.Dialogue.AircraftCallsign != Callsign)
            return;

        // LoadDialoguesAsync changes Dialogues, which the view binds to
        _guiInvoker.InvokeOnGUI(mainForm =>
        {
            if (_disposed)
                return;

            try
            {
                if (string.IsNullOrWhiteSpace(Callsign))
                    return;

                _ = LoadDialoguesAsync();
            }
            catch (Exception ex)
            {
                _errorReporter.ReportError(ex);
            }
        });
    }

    [RelayCommand]
    void ToggleExtendedDisplay(HistoryMessageViewModel messageViewModel)
    {
        try
        {
            // If this message is already extended, collapse it
            if (CurrentlyExtendedMessage == messageViewModel)
            {
                messageViewModel.IsExtended = false;
                CurrentlyExtendedMessage = null;
                return;
            }

            // Collapse previously extended message
            CurrentlyExtendedMessage?.IsExtended = false;

            // Extend this message
            messageViewModel.IsExtended = true;
            CurrentlyExtendedMessage = messageViewModel;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        WeakReferenceMessenger.Default.Unregister<DialogueChangedNotification>(this);
        _disposed = true;
    }
}
