using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CPDLC.Messages;
using CPDLCPlugin.Configuration;
using CPDLCPlugin.Messages;
using CPDLCServer.Contracts;
using MediatR;
using Serilog;
using Serilog.Core;
using vatSysToolbox;
using vatSysToolbox.Windows;

namespace CPDLCPlugin.ViewModels;

// TODO: Associate downlink messages with uplink classes.

public partial class EditorViewModel : ObservableObject, IRecipient<DialogueChangedNotification>, IDisposable
{
    // readonly PluginConfiguration _configuration;
    readonly UplinkMessagesConfiguration _uplinkMessagesConfiguration;
    readonly DialogueStore _dialogueStore;
    readonly SuspendedMessageStore _suspendedMessageStore;
    readonly IMediator _mediator;
    readonly IErrorReporter _errorReporter;
    readonly IGuiInvoker _guiInvoker;
    readonly IWindowHandle _windowHandle;
    readonly ILogger _logger;

#if DEBUG

    static DownlinkMessageViewModel[] _testDownlinkMessages =
    [
        new()
        {
            Received = DateTimeOffset.Now,
            Message = "DEFERRED DOWNLINK",
            Deferred = true
        },

        new()
        {
            Received = DateTimeOffset.Now,
            Message = "STANDBY DOWNLINK WITH VERY VERY VERY VERY VERY VERY VERY VERY VERY VERY VERY VERY VERY LONG MESSAGE",
            StandbySent = true
        }
    ];

    static UplinkMessagesConfiguration CreateTestConfiguration()
    {
        return new UplinkMessagesConfiguration
        {
            QuickAccessMessages =
            [
                new UplinkMessageReference { Element = "UM147" },
                new UplinkMessageReference { Element = "UM123" },
                new UplinkMessageReference { Element = "UM20" },
                new UplinkMessageReference
                {
                    Element = "UM117",
                    DefaultParameters = new Dictionary<string, string>
                    {
                        { "icaoUnitName", "MELBOURNE CTR" },
                        { "frequency", "122.4" }
                    }
                },
                new UplinkMessageReference
                {
                    Element = "UM169",
                    DefaultParameters = new Dictionary<string, string>
                    {
                        { "freeText", "REQUEST RECEIVED, RESPONSE WILL BE VIA VOICE" }
                    }
                },
                new UplinkMessageReference
                {
                    Element = "UM169",
                    DefaultParameters = new Dictionary<string, string>
                    {
                        { "freeText", "CRUISE CLIMB PROCEDURE NOT AVAILABLE IN AUSTRALIAN ADMINISTERED AIRSPACE" }
                    }
                }
            ],
            Groups =
            [
                new UplinkMessageGroup
                {
                    Name = "LEVEL",
                    Messages =
                    [
                        new UplinkMessageReference { Element = "UM20" }
                    ]
                }
            ]
        };
    }

    // For testing in the designer
    public EditorViewModel() : this("QFA1", null!,CreateTestConfiguration(), null!, null!, null!, null!, null!, Logger.None)
    {
        DownlinkMessages = _testDownlinkMessages;
    }

#endif

    public EditorViewModel(
        string callsign,
        DialogueStore dialogueStore,
        UplinkMessagesConfiguration uplinkMessagesConfiguration,
        SuspendedMessageStore suspendedMessageStore,
        IMediator mediator,
        IErrorReporter errorReporter,
        IGuiInvoker guiInvoker,
        IWindowHandle windowHandle,
        ILogger logger)
    {
        _uplinkMessagesConfiguration = uplinkMessagesConfiguration;
        _dialogueStore = dialogueStore;
        _suspendedMessageStore = suspendedMessageStore;
        _mediator = mediator;
        _errorReporter = errorReporter;
        _guiInvoker = guiInvoker;
        _windowHandle = windowHandle;
        _logger = logger;

        Callsign = callsign;

        MessageCategoryNames = _uplinkMessagesConfiguration.Groups
            .Select(g => g.Name)
            .ToArray();

        SelectedMessageCategory = null;
        DisplayMessageElements(_uplinkMessagesConfiguration.QuickAccessMessages);

        ClearUplinkMessage();

        WeakReferenceMessenger.Default.Register(this);

        _ = Task.Run(async () =>
        {
            await LoadDownlinkMessagesAsync();
            SelectedDownlinkMessage = DownlinkMessages.LastOrDefault(); // Select the last downlink by default
        });
    }

    [ObservableProperty] string _callsign;

    [ObservableProperty] DownlinkMessageViewModel[] _downlinkMessages = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(SendStandbyUplinkMessageCommand),
        nameof(DeferCommand),
        nameof(SendUnableDueTrafficUplinkMessageCommand),
        nameof(SendUnableDueAirspaceUplinkMessageCommand))]
    DownlinkMessageViewModel? _selectedDownlinkMessage;

    [ObservableProperty] DownlinkMessageViewModel? _currentlyExtendedDownlinkMessage;

    public bool ShowMessageCategories => !ShowHotButtons;
    [ObservableProperty] string[] _messageCategoryNames = [];

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowMessageCategories))]
    string? _selectedMessageCategory;

    [ObservableProperty] UplinkMessageTemplateViewModel[] _selectedMessageCategoryElements = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMessageCategories))]
    [NotifyCanExecuteChangedFor(nameof(SuspendCommand))]
    bool _showHotButtons;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(EscapeCommand),
        nameof(RestoreCommand),
        nameof(SuspendCommand))]
    UplinkMessageElementViewModel[] _uplinkMessageElements = [];
    [ObservableProperty] UplinkMessageElementViewModel? _selectedUplinkMessageElement;

    [ObservableProperty] string? _error;

    partial void OnSelectedDownlinkMessageChanged(DownlinkMessageViewModel? _, DownlinkMessageViewModel? newValue)
    {
        // Show the hot buttons if a message has been selected
        ShowHotButtons = newValue is not null;
    }

    bool DownlinkIsSelected()
    {
        return SelectedDownlinkMessage is not null;
    }

    partial void OnShowHotButtonsChanged(bool value)
    {
        SelectedMessageCategory = value
            ? null
            : MessageCategoryNames.First();
    }

    [RelayCommand]
    void SelectMessageCategory(string? messageClass)
    {
        SelectedMessageCategory = messageClass;
    }

    partial void OnSelectedMessageCategoryChanged(string? value)
    {
        try
        {
            // If no category is selected, show quick access messages
            if (string.IsNullOrEmpty(value))
            {
                DisplayMessageElements(_uplinkMessagesConfiguration.QuickAccessMessages);
            }
            else
            {
                // Find the group by name
                var group = _uplinkMessagesConfiguration.Groups
                    .FirstOrDefault(g => g.Name == value);

                if (group == null)
                {
                    SelectedMessageCategoryElements = [];
                    return;
                }

                DisplayMessageElements(group.Messages);
            }
        }
        catch (Exception exception)
        {
            _errorReporter.ReportError(exception, "Error");
        }
    }

    void DisplayMessageElements(IEnumerable<UplinkMessageReference> messageReferences)
    {
        SelectedMessageCategoryElements = messageReferences
            .Select(ResolveMessageReference)
            .ToArray();
    }

    [RelayCommand(CanExecute = nameof(DownlinkIsSelected))]
    async Task SendStandbyUplinkMessage()
    {
        try
        {
            // Send the "STANDBY" uplink message
            await _mediator.Send(new SendStandbyUplinkRequest(SelectedDownlinkMessage!.Dialogue.Id, SelectedDownlinkMessage!.OriginalMessage.MessageId, Callsign));
            SelectedDownlinkMessage = null;
            ClearUplinkMessage();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(DownlinkIsSelected))]
    async Task Defer()
    {
        try
        {
            // Send the "REQUEST DEFERRED" uplink message
            await _mediator.Send(new SendDeferredUplinkRequest(SelectedDownlinkMessage!.Dialogue.Id, SelectedDownlinkMessage!.OriginalMessage.MessageId, Callsign));
            SelectedDownlinkMessage = null;
            ClearUplinkMessage();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void Edit()
    {
        try
        {
            ShowHotButtons = false;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(DownlinkIsSelected))]
    async Task SendUnableDueTrafficUplinkMessage()
    {
        try
        {
            // Send the "UNABLE" and "DUE TO TRAFFIC" uplink messages
            await _mediator.Send(new SendUnableUplinkRequest(SelectedDownlinkMessage!.Dialogue.Id, SelectedDownlinkMessage!.OriginalMessage.MessageId, Callsign, UplinkElementIds.DueToTraffic));

            // TODO: Do we need to do this? DialogueChanged will kick-in and remove it anyway
            var newDownlinkMessages = DownlinkMessages.Where(m => m != SelectedDownlinkMessage);
            DownlinkMessages = newDownlinkMessages.ToArray();
            SelectedDownlinkMessage = null;

            ClearUplinkMessage();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(DownlinkIsSelected))]
    async Task SendUnableDueAirspaceUplinkMessage()
    {
        try
        {
            // Send the "UNABLE" and "DUE TO AIRSPACE RESTRICTION" uplink messages
            await _mediator.Send(new SendUnableUplinkRequest(SelectedDownlinkMessage!.Dialogue.Id, SelectedDownlinkMessage!.OriginalMessage.MessageId, Callsign, UplinkElementIds.DueToAirspaceRestriction));

            // TODO: Do we need to do this? DialogueChanged will kick-in and remove it anyway
            var newDownlinkMessages = DownlinkMessages.Where(m => m != SelectedDownlinkMessage);
            DownlinkMessages = newDownlinkMessages.ToArray();
            SelectedDownlinkMessage = null;

            ClearUplinkMessage();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void AddMessageElement(UplinkMessageTemplateViewModel template)
    {
        try
        {
            var parts = ConvertToViewModel(template.MessageReference);

            // If a message element is selected, replace it with this one
            if (SelectedUplinkMessageElement is not null)
            {
                SelectedUplinkMessageElement.Replace(template.ElementId, parts);
                UplinkMessageElements = UplinkMessageElements;

                SuspendCommand.NotifyCanExecuteChanged();
            }
            else if (UplinkMessageElements.Length < 5)
            {
                // If no element is selected, append this to the list
                var firstBlankElement = UplinkMessageElements.FirstOrDefault(e => e.Parts.Length == 0);
                if (firstBlankElement is not null)
                {
                    firstBlankElement.Replace(template.ElementId, parts);

                    // Trigger property change
                    // TODO: Find a better way to do this
                    UplinkMessageElements = UplinkMessageElements;

                    SuspendCommand.NotifyCanExecuteChanged();
                }
                else
                {
                    var newMessageElements = UplinkMessageElements.ToList();
                    newMessageElements.Add(new UplinkMessageElementViewModel(template.ElementId, parts));

                    UplinkMessageElements = newMessageElements.ToArray();

                    SuspendCommand.NotifyCanExecuteChanged();
                }
            }

            // TODO: Exceeded 5 elements, show an error
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void ToggleMessageElementSelection(UplinkMessageElementViewModel element)
    {
        try
        {
            if (SelectedUplinkMessageElement == element)
            {
                SelectedUplinkMessageElement = null;
            }
            else
            {
                SelectedUplinkMessageElement = element;
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void InsertMessageElementAbove(UplinkMessageElementViewModel element)
    {
        try
        {
            // Don't exceed 5 elements
            if (UplinkMessageElements.Length >= 5)
                return;

            var elements = UplinkMessageElements.ToList();
            var index = elements.IndexOf(element);

            if (index < 0)
                return;

            // Insert a new blank element above the clicked one
            var newElement = new UplinkMessageElementViewModel();
            elements.Insert(index, newElement);

            UplinkMessageElements = elements.ToArray();
            SelectedUplinkMessageElement = newElement;

            SuspendCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void ClearMessageElement(UplinkMessageElementViewModel element)
    {
        try
        {
            if (element.Parts.Any())
            {
                // If this element is not blank, clear it
                element.Clear();

                SuspendCommand.NotifyCanExecuteChanged();
            }
            else if (UplinkMessageElements.Length > 1)
            {
                // If this element is blank and there's more than one element, remove it
                var newMessages = UplinkMessageElements.ToList();
                newMessages.Remove(element);

                UplinkMessageElements = newMessages.ToArray();
                SelectedUplinkMessageElement = null;

                SuspendCommand.NotifyCanExecuteChanged();
            }

            // Do nothing if this is the last element, and it's already blank
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanEscape))]
    void Escape()
    {
        ClearUplinkMessage();
        SelectedMessageCategory = null;
    }

    bool CanEscape() => UplinkMessageElements.Any();

    [RelayCommand(CanExecute = nameof(CanRestore))]
    async Task Restore()
    {
        ClearUplinkMessage();

        if (!_suspendedMessageStore.TryRemove(Callsign, out var suspendedUplinkMessageElements))
            return;

        UplinkMessageElements = suspendedUplinkMessageElements;
        SelectedUplinkMessageElement = null;

        await _mediator.Send(new RebuildCpdlcStatusLabelItemsRequest());
    }

    bool CanRestore()
    {
        return _suspendedMessageStore.HasSuspendedMessage(Callsign);
    }

    [RelayCommand(CanExecute = nameof(CanSuspend))]
    async Task Suspend()
    {
        _suspendedMessageStore.Add(Callsign, UplinkMessageElements.ToArray());
        ClearUplinkMessage();

        // Select the most recent downlink message if none is already selected
        SelectedDownlinkMessage ??= DownlinkMessages.LastOrDefault();

        SelectedMessageCategory = null;

        await _mediator.Send(new RebuildCpdlcStatusLabelItemsRequest());
    }

    bool CanSuspend()
    {
        // Cannot suspend replies to downlinks
        if (SelectedDownlinkMessage is not null)
            return false;

        // Cannot suspend in Mode 2
        if (ShowHotButtons)
            return false;

        // Cannot suspend empty messages
        if (UplinkMessageElements.All(m => m.IsEmpty))
            return false;

        // Cannot suspend when there is already a suspended message
        if (_suspendedMessageStore.HasSuspendedMessage(Callsign))
            return false;

        return true;
    }

    [RelayCommand]
    async Task SendUplinkMessage()
    {
        try
        {
            var uplinkMessageElements = ConstructUplinkMessage();
            var canAction = uplinkMessageElements.Any(e => ActionableElements.Contains(e.Id));

            // Remove the selected downlink message and select the most recent one
            var downlinkMessage = SelectedDownlinkMessage;

            _logger.Debug("[{Callsign}] Sending uplink from editor - SelectedDownlinkMessage: {HasSelection}, MessageId: {MessageId}, Elements: {@Elements}",
                Callsign,
                downlinkMessage != null,
                downlinkMessage?.OriginalMessage?.MessageId,
                uplinkMessageElements);

            if (SelectedDownlinkMessage is not null)
            {
                _logger.Debug("[{Callsign}] Removing downlink {DownlinkId} from selection after sending reply",
                    Callsign, SelectedDownlinkMessage.OriginalMessage.MessageId);
                var newDownlinkMessages = new List<DownlinkMessageViewModel>();
                newDownlinkMessages.AddRange(DownlinkMessages.Where(d => d != SelectedDownlinkMessage));
                SelectedDownlinkMessage = newDownlinkMessages.LastOrDefault();
            }
            else
            {
                _logger.Debug("[{Callsign}] No downlink selected - sending uplink without MessageReference", Callsign);
            }

            if (downlinkMessage is not null)
            {
                await _mediator.Send(new ReplyToDownlinkRequest(
                    Callsign,
                    downlinkMessage.Dialogue.Id,
                    downlinkMessage.OriginalMessage.MessageId,
                    uplinkMessageElements,
                    canAction));
            }
            else
            {
                await _mediator.Send(new BeginDialogueRequest(
                    Callsign,
                    uplinkMessageElements,
                    canAction));
            }

            ClearUplinkMessage();

            if (SelectedDownlinkMessage is not null)
                return;

            // Close the window if there are no more downlink messages remaining
            _windowHandle.Close();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    void ClearUplinkMessage()
    {
        UplinkMessageElements = [new UplinkMessageElementViewModel()];
        SelectedUplinkMessageElement = null;
    }

    // Splits the template of the element into text parts and parameter parts.
    // The placeholders in the template have the same sequence as the parameters of the element.
    IUplinkMessageElementComponentViewModel[] ConvertToViewModel(UplinkMessageReference reference)
    {
        var definition = UplinkElements.Get(reference.ElementId);

        var parts = new List<IUplinkMessageElementComponentViewModel>();
        var currentText = new StringBuilder();
        var placeholder = new StringBuilder();
        var insideBrackets = false;
        var parameterIndex = 0;

        foreach (var c in definition.Template)
        {
            switch (c)
            {
                case '[' when !insideBrackets:
                    if (currentText.Length > 0)
                    {
                        parts.Add(new UplinkMessageTextElementComponentViewModel(currentText.ToString()));
                        currentText.Clear();
                    }

                    insideBrackets = true;
                    placeholder.Clear();
                    break;

                case ']' when insideBrackets:
                {
                    var parameter = definition.Parameters[parameterIndex];
                    var templateElement = new UplinkMessageTemplateElementComponentViewModel($"[{placeholder}]");

                    if (reference.DefaultParameters?.TryGetValue(parameter.Name, out var defaultValue) == true)
                    {
                        templateElement.Value = defaultValue;
                    }

                    parts.Add(templateElement);
                    parameterIndex++;
                    insideBrackets = false;
                    break;
                }

                default:
                    if (insideBrackets)
                    {
                        placeholder.Append(c);
                    }
                    else
                    {
                        currentText.Append(c);
                    }

                    break;
            }
        }

        if (currentText.Length > 0)
        {
            parts.Add(new UplinkMessageTextElementComponentViewModel(currentText.ToString()));
        }

        return parts.ToArray();
    }

    UplinkMessageElement[] ConstructUplinkMessage()
    {
        var elements = new List<UplinkMessageElement>();

        foreach (var uplinkMessageElement in UplinkMessageElements)
        {
            if (uplinkMessageElement.ElementId is null)
                continue;

            var values = new List<string>();
            foreach (var templatePart in uplinkMessageElement.Parts.OfType<UplinkMessageTemplateElementComponentViewModel>())
            {
                if (string.IsNullOrEmpty(templatePart.Value))
                    throw new Exception("Uplink message is invalid");

                values.Add(templatePart.Value!);
            }

            elements.Add(new UplinkMessageElement(uplinkMessageElement.ElementId.Value, values.ToArray()));
        }

        return elements.ToArray();
    }

    public void Receive(DialogueChangedNotification message)
    {
        // The dialogues change for all the aircraft. The editor shows the dialogues of one aircraft only. Do not post to
        // the UI thread for the other aircraft.
        if (message.Dialogue.AircraftCallsign != Callsign)
            return;

        // LoadDownlinkMessagesAsync changes the downlink messages, which the view binds to
        _guiInvoker.InvokeOnGUI(async _ => await LoadDownlinkMessagesAsync());
    }

    async Task LoadDownlinkMessagesAsync()
    {
        try
        {
            var openDialogues = (await _dialogueStore.All(CancellationToken.None))
                .Where(d => !d.IsClosed && d.AircraftCallsign == Callsign);

            var downlinkMessageViewModels = new List<DownlinkMessageViewModel>();
            foreach (var dialogue in openDialogues)
            {
                foreach (var message in dialogue.Messages)
                {
                    if (message is not DownlinkMessageDto downlinkMessage ||
                        downlinkMessage.IsClosed ||
                        downlinkMessage.ResponseType == CpdlcDownlinkResponseType.NoResponse)
                        continue;

                    var downlinkMessageViewModel = new DownlinkMessageViewModel(dialogue, downlinkMessage);
                    downlinkMessageViewModels.Add(downlinkMessageViewModel);
                }
            }

            _logger.Debug("[{Callsign}] Loaded {Count} open downlinks into editor: {@Downlinks}",
                Callsign,
                downlinkMessageViewModels.Count,
                downlinkMessageViewModels.Select(vm => new { vm.OriginalMessage.MessageId, vm.OriginalMessage.Elements, vm.OriginalMessage.IsClosed, vm.OriginalMessage.IsAcknowledged }));

            // Capture the selected downlink before updating the list so we can try to maintain the selection
            var selectedDownlinkMessage = SelectedDownlinkMessage;

            DownlinkMessages = downlinkMessageViewModels.ToArray();

            // Try to maintain the current selection if the message still exists
            if (selectedDownlinkMessage is not null)
            {
                SelectedDownlinkMessage = downlinkMessageViewModels.FirstOrDefault(vm =>
                    vm.Dialogue.Id == selectedDownlinkMessage.Dialogue.Id &&
                    vm.OriginalMessage.MessageId == selectedDownlinkMessage.OriginalMessage.MessageId);

                _logger.Debug("[{Callsign}] Selection maintained: {Maintained}, SelectedDownlinkMessage: {DialogueId}/{MessageId}",
                    Callsign,
                    SelectedDownlinkMessage is not null,
                    SelectedDownlinkMessage?.Dialogue.Id,
                    SelectedDownlinkMessage?.OriginalMessage.MessageId);
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<DialogueChangedNotification>(this);
    }

    UplinkMessageTemplateViewModel ResolveMessageReference(UplinkMessageReference reference)
    {
        var definition = UplinkElements.Get(reference.ElementId);

        // Show the default values in place of the placeholders.
        // The placeholders have the same sequence as the parameters of the element.
        var parameterIndex = 0;
        var content = Regex.Replace(definition.Template, @"\[[^\]]+\]", placeholder =>
        {
            var parameter = definition.Parameters[parameterIndex++];
            return reference.DefaultParameters?.TryGetValue(parameter.Name, out var defaultValue) == true
                ? defaultValue
                : placeholder.Value;
        });

        return new UplinkMessageTemplateViewModel(content, definition.Id, reference);
    }
}
