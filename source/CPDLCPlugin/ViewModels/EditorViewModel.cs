using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Configuration;
using CPDLCPlugin.Messages;
using CPDLCServer.Contracts;
using MediatR;
using Serilog;
using Serilog.Core;

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
            MasterMessages =
            [
                new UplinkMessageTemplate
                {
                    Id = 147, Template = "REQUEST POSITION REPORT", Parameters = [],
                    ResponseType = UplinkResponseType.NoResponse
                },
                new UplinkMessageTemplate
                {
                    Id = 123, Template = "SQUAWK [code]",
                    Parameters = [new UplinkMessageParameter { Name = "code", Type = ParameterType.Code }],
                    ResponseType = UplinkResponseType.WilcoUnable
                },
                new UplinkMessageTemplate
                {
                    Id = 20, Template = "CLIMB TO [lev]",
                    Parameters = [new UplinkMessageParameter { Name = "lev", Type = ParameterType.Level }],
                    ResponseType = UplinkResponseType.WilcoUnable
                },
                new UplinkMessageTemplate
                {
                    Id = 117, Template = "CONTACT [unit name] [freq]",
                    Parameters =
                    [
                        new UplinkMessageParameter { Name = "unit name", Type = ParameterType.UnitName },
                        new UplinkMessageParameter { Name = "freq", Type = ParameterType.Frequency }
                    ],
                    ResponseType = UplinkResponseType.WilcoUnable
                },
                new UplinkMessageTemplate
                {
                    Id = 169, Template = "[freetext]",
                    Parameters = [new UplinkMessageParameter { Name = "freetext", Type = ParameterType.FreeText }],
                    ResponseType = UplinkResponseType.Roger
                }
            ],
            QuickAccessMessages =
            [
                new UplinkMessageReference { MessageId = 147 },
                new UplinkMessageReference { MessageId = 123 },
                new UplinkMessageReference { MessageId = 20 },
                new UplinkMessageReference
                {
                    MessageId = 117,
                    DefaultParameters = new Dictionary<string, string>
                    {
                        { "unit name", "MELBOURNE CTR" },
                        { "freq", "122.4" }
                    }
                },
                new UplinkMessageReference
                {
                    MessageId = 169,
                    DefaultParameters = new Dictionary<string, string>
                    {
                        { "freetext", "REQUEST RECEIVED, RESPONSE WILL BE VIA VOICE" }
                    },
                    ResponseType = UplinkResponseType.Roger
                },
                new UplinkMessageReference
                {
                    MessageId = 169,
                    DefaultParameters = new Dictionary<string, string>
                    {
                        { "freetext", "CRUISE CLIMB PROCEDURE NOT AVAILABLE IN AUSTRALIAN ADMINISTERED AIRSPACE" }
                    },
                    ResponseType = UplinkResponseType.Roger
                }
            ],
            Groups =
            [
                new UplinkMessageGroup
                {
                    Name = "LEVEL",
                    Messages =
                    [
                        new UplinkMessageReference { MessageId = 20 }
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
            await _mediator.Send(new SendStandbyUplinkRequest(SelectedDownlinkMessage!.OriginalMessage.MessageId, Callsign));
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
            await _mediator.Send(new SendDeferredUplinkRequest(SelectedDownlinkMessage!.OriginalMessage.MessageId, Callsign));
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
            await _mediator.Send(new SendUnableUplinkRequest(SelectedDownlinkMessage!.OriginalMessage.MessageId, Callsign, "DUE TO TRAFFIC."));

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
            await _mediator.Send(new SendUnableUplinkRequest(SelectedDownlinkMessage!.OriginalMessage.MessageId, Callsign, "DUE TO AIRSPACE RESTRICTION."));

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
                SelectedUplinkMessageElement.Replace(parts, template.ResponseType);
                UplinkMessageElements = UplinkMessageElements;

                SuspendCommand.NotifyCanExecuteChanged();
            }
            else if (UplinkMessageElements.Length < 5)
            {
                // If no element is selected, append this to the list
                var firstBlankElement = UplinkMessageElements.FirstOrDefault(e => e.Parts.Length == 0);
                if (firstBlankElement is not null)
                {
                    firstBlankElement.Replace(parts, template.ResponseType);

                    // Trigger property change
                    // TODO: Find a better way to do this
                    UplinkMessageElements = UplinkMessageElements;

                    SuspendCommand.NotifyCanExecuteChanged();
                }
                else
                {
                    var newMessageElements = UplinkMessageElements.ToList();
                    newMessageElements.Add(new UplinkMessageElementViewModel(parts, template.ResponseType));

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
            var (uplinkMessageContent, uplinkMessageResponseType) = ConstructUplinkMessage();

            // Remove the selected downlink message and select the most recent one
            var downlinkMessage = SelectedDownlinkMessage;

            _logger.Debug("[{Callsign}] Sending uplink from editor - SelectedDownlinkMessage: {HasSelection}, MessageId: {MessageId}, Content: {Content}",
                Callsign,
                downlinkMessage != null,
                downlinkMessage?.OriginalMessage?.MessageId,
                uplinkMessageContent);

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

            await _mediator.Send(new SendUplinkRequest(
                Callsign,
                downlinkMessage?.OriginalMessage.MessageId,
                uplinkMessageResponseType,
                uplinkMessageContent));

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

    IUplinkMessageElementComponentViewModel[] ConvertToViewModel(UplinkMessageReference reference)
    {
        // Get the master message template
        var masterMessage = _uplinkMessagesConfiguration.MasterMessages
            .FirstOrDefault(m => m.Id == reference.MessageId);

        if (masterMessage == null)
            throw new InvalidOperationException($"Master message with ID {reference.MessageId} not found");

        var template = masterMessage.Template;
        var parts = new List<IUplinkMessageElementComponentViewModel>();
        var currentText = new StringBuilder();
        var insideBrackets = false;
        var parameterName = new StringBuilder();

        foreach (var c in template)
        {
            switch (c)
            {
                case '[' when !insideBrackets:
                {
                    // Transition from outside to inside brackets
                    if (currentText.Length > 0)
                    {
                        // Save the text part
                        parts.Add(new UplinkMessageTextElementComponentViewModel(currentText.ToString()));
                        currentText.Clear();
                    }
                    insideBrackets = true;
                    parameterName.Clear();
                    break;
                }

                case ']' when insideBrackets:
                {
                    // Transition from inside to outside brackets
                    var paramName = parameterName.ToString();
                    var templateElement = new UplinkMessageTemplateElementComponentViewModel($"[{paramName}]");

                    // Check if there's a default value for this parameter
                    if (reference.DefaultParameters?.TryGetValue(paramName, out var defaultValue) == true)
                    {
                        // Pre-fill the template element with the default value
                        templateElement.Value = defaultValue;
                    }

                    parts.Add(templateElement);
                    insideBrackets = false;
                    break;
                }

                default:
                    if (insideBrackets)
                    {
                        parameterName.Append(c);
                    }
                    else
                    {
                        currentText.Append(c);
                    }
                    break;
            }
        }

        // Handle any remaining text
        if (currentText.Length > 0)
        {
            if (insideBrackets)
            {
                // Unclosed bracket - treat as template part with what we have
                var templateElement = new UplinkMessageTemplateElementComponentViewModel($"[{parameterName}]");
                parts.Add(templateElement);
            }
            else
            {
                // Normal text
                parts.Add(new UplinkMessageTextElementComponentViewModel(currentText.ToString()));
            }
        }

        return parts.ToArray();
    }

    (string, CpdlcUplinkResponseType) ConstructUplinkMessage()
    {
        var content = string.Empty;
        var responseType = CpdlcUplinkResponseType.NoResponse;

        foreach (var uplinkMessageElement in UplinkMessageElements)
        {
            if (!string.IsNullOrEmpty(content))
            {
                content += ". ";
            }

            foreach (var uplinkMessageElementPart in uplinkMessageElement.Parts)
            {
                if (uplinkMessageElementPart is UplinkMessageTextElementComponentViewModel textPart)
                {
                    content += textPart.Value;
                    continue;
                }

                if (uplinkMessageElementPart is UplinkMessageTemplateElementComponentViewModel templatePart)
                {
                    if (string.IsNullOrEmpty(templatePart.Value))
                        throw new Exception("Uplink message is invalid");

                    content += $"@{templatePart.Value}@";
                }

                // TODO: Error?
            }

            var currentResponseRank = _responseTypeRank[responseType];
            var newResponseRank = _responseTypeRank[_responseTypeMap[uplinkMessageElement.ResponseType]];
            if (newResponseRank > currentResponseRank)
                responseType = _responseTypeMap[uplinkMessageElement.ResponseType];
        }

        return (content.Trim(), responseType);
    }

    public void Receive(DialogueChangedNotification message)
    {
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
                downlinkMessageViewModels.Select(vm => new { vm.OriginalMessage.MessageId, vm.OriginalMessage.Content, vm.OriginalMessage.IsClosed, vm.OriginalMessage.IsAcknowledged }));

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
        var masterMessage = _uplinkMessagesConfiguration.MasterMessages
            .FirstOrDefault(m => m.Id == reference.MessageId);

        if (masterMessage == null)
            throw new InvalidOperationException($"Master message with ID {reference.MessageId} not found");

        var template = masterMessage.Template;

        // Replace template parameters with default values for display purposes
        if (reference.DefaultParameters != null)
        {
            foreach (var kvp in reference.DefaultParameters)
            {
                var paramName = kvp.Key;
                var paramValue = kvp.Value;

                template = template.Replace($"[{paramName}]", paramValue);
            }
        }

        // Use the reference response type if specified, otherwise use the master message response type
        var responseType = reference.ResponseType ?? masterMessage.ResponseType;

        var isFreeText = masterMessage.Id == 169;
        var isRevision = masterMessage.Id == 170;

        var viewModel = new UplinkMessageTemplateViewModel(
            template,
            responseType,
            isFreeText,
            isRevision,
            reference);

        return viewModel;
    }

    readonly IDictionary<UplinkResponseType, CpdlcUplinkResponseType> _responseTypeMap = new Dictionary<UplinkResponseType, CpdlcUplinkResponseType>
    {
        { UplinkResponseType.WilcoUnable, CpdlcUplinkResponseType.WilcoUnable },
        { UplinkResponseType.AffirmativeNegative, CpdlcUplinkResponseType.AffirmativeNegative },
        { UplinkResponseType.Roger, CpdlcUplinkResponseType.Roger },
        { UplinkResponseType.NoResponse, CpdlcUplinkResponseType.NoResponse },
    };

    readonly IDictionary<CpdlcUplinkResponseType, int> _responseTypeRank = new Dictionary<CpdlcUplinkResponseType, int>
    {
        { CpdlcUplinkResponseType.WilcoUnable, 3 },
        { CpdlcUplinkResponseType.AffirmativeNegative, 2 },
        { CpdlcUplinkResponseType.Roger, 1 },
        { CpdlcUplinkResponseType.NoResponse, 0 },
    };
}
