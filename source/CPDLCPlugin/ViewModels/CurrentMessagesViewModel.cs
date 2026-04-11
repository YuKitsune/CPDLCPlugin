using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Configuration;
using CPDLCPlugin.Messages;
using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCPlugin.ViewModels;


public partial class CurrentMessagesViewModel : ObservableObject, IRecipient<DialogueChangedNotification>, IDisposable
{
    readonly PluginConfiguration _configuration;
    readonly DialogueStore _dialogueStore;
    readonly IMediator _mediator;
    readonly IGuiInvoker _guiInvoker;
    readonly IErrorReporter _errorReporter;
    readonly IWindowHandle _windowHandle;
    readonly IJurisdictionChecker _jurisdictionChecker;
    bool _disposed;

    public CurrentMessagesViewModel(
        PluginConfiguration configuration,
        DialogueStore dialogueStore,
        IMediator mediator,
        IGuiInvoker guiInvoker,
        IErrorReporter errorReporter,
        IWindowHandle windowHandle,
        IJurisdictionChecker jurisdictionChecker)
    {
        _configuration = configuration;
        _dialogueStore = dialogueStore;
        _mediator = mediator;
        _guiInvoker = guiInvoker;
        _errorReporter = errorReporter;
        _windowHandle = windowHandle;
        _jurisdictionChecker = jurisdictionChecker;

        WeakReferenceMessenger.Default.Register(this);

        // Initial load
        _ = LoadDialoguesAsync();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        WeakReferenceMessenger.Default.Unregister<DialogueChangedNotification>(this);
        _disposed = true;
    }

#if DEBUG
    // Test constructor for design-time data - shows all possible message states
    public CurrentMessagesViewModel()
    {
        var mexMessageDisplayLength = 40;

        var testGroups = new ObservableCollection<DialogueViewModel>();
        var messageId = 1;

        // Helper to create a dialogue DTO with messages
        DialogueDto CreateDialogue(string callsign, params CpdlcMessageDto[] messages)
        {
            return new DialogueDto(
                Id: Guid.NewGuid(),
                AircraftCallsign: callsign,
                Messages: messages,
                Opened: messages.Min(m => m.Time),
                Closed: messages.Any(m => m.IsClosed) ? messages.Where(m => m.IsClosed).Max(m => m.Closed) : null,
                Archived: null);
        }

        // Group 1: Regular Uplink/Downlink messages (not acknowledged)
        var group1Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Recipient = "TEST1",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.WilcoUnable,
            Content = "UPLINK REGULAR",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-48),
            IsPilotLate = false,
            IsTransmissionFailed = false,
            IsClosedManually = false
        };
        var group1Downlink = new DownlinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Sender = "TEST1",
            ResponseType = CpdlcDownlinkResponseType.ResponseRequired,
            Content = "DOWNLINK REGULAR",
            Received = DateTimeOffset.UtcNow.AddMinutes(-47),
            IsControllerLate = false
        };
        var group1Dialogue = CreateDialogue("TEST1", group1Uplink, group1Downlink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-48),
            Messages =
            [
                new(group1Dialogue, group1Uplink, mexMessageDisplayLength),
                new(group1Dialogue, group1Downlink, mexMessageDisplayLength)
            ]
        });

        // Group 3: Urgent messages (not acknowledged)
        var group3Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.High,
            Recipient = "TEST3",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.WilcoUnable,
            Content = "UPLINK URGENT",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-45),
            IsPilotLate = false,
            IsTransmissionFailed = false,
            IsClosedManually = false
        };
        var group3Downlink = new DownlinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.High,
            Sender = "TEST3",
            ResponseType = CpdlcDownlinkResponseType.ResponseRequired,
            Content = "DOWNLINK URGENT",
            Received = DateTimeOffset.UtcNow.AddMinutes(-44),
            IsControllerLate = false
        };
        var group3Dialogue = CreateDialogue("TEST3", group3Uplink, group3Downlink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-45),
            Messages =
            [
                new(group3Dialogue, group3Uplink, mexMessageDisplayLength),
                new(group3Dialogue, group3Downlink, mexMessageDisplayLength)
            ]
        });

        // Group 5: Closed messages (not acknowledged)
        var group5Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Recipient = "TEST5",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.Roger,
            Content = "UPLINK CLOSED",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-42),
            Closed = DateTimeOffset.UtcNow.AddMinutes(-42),
            IsPilotLate = false,
            IsTransmissionFailed = false,
            IsClosedManually = false
        };
        var group5Downlink = new DownlinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Sender = "TEST5",
            ResponseType = CpdlcDownlinkResponseType.NoResponse,
            Content = "DOWNLINK CLOSED",
            Received = DateTimeOffset.UtcNow.AddMinutes(-41),
            Closed = DateTimeOffset.UtcNow.AddMinutes(-41),
            IsControllerLate = false
        };
        var group5Dialogue = CreateDialogue("TEST5", group5Uplink, group5Downlink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-42),
            Messages =
            [
                new(group5Dialogue, group5Uplink, mexMessageDisplayLength),
                new(group5Dialogue, group5Downlink, mexMessageDisplayLength)
            ]
        });

        // Group 7: Special Closed messages (not acknowledged)
        var group7Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Recipient = "TEST7",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.Roger,
            Content = "STANDBY",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-39),
            Closed = DateTimeOffset.UtcNow.AddMinutes(-39),
            IsPilotLate = false,
            IsTransmissionFailed = false,
            IsClosedManually = false
        };
        var group7Dialogue = CreateDialogue("TEST7", group7Uplink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-39),
            Messages = [new(group7Dialogue, group7Uplink, mexMessageDisplayLength)]
        });

        // Group 9: Failed messages (not acknowledged)
        var group9Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Recipient = "TEST9",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.WilcoUnable,
            Content = "UPLINK FAILED",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-37),
            IsPilotLate = false,
            IsTransmissionFailed = true,
            IsClosedManually = false
        };
        var group9Dialogue = CreateDialogue("TEST9", group9Uplink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-37),
            Messages = [new(group9Dialogue, group9Uplink, mexMessageDisplayLength)]
        });

        // Group 11: Pilot Late messages (not acknowledged)
        var group11Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Recipient = "TEST11",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.WilcoUnable,
            Content = "UPLINK PILOT LATE",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-35),
            IsPilotLate = true,
            IsTransmissionFailed = false,
            IsClosedManually = false
        };
        var group11Dialogue = CreateDialogue("TEST11", group11Uplink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-35),
            Messages = [new(group11Dialogue, group11Uplink, mexMessageDisplayLength)]
        });

        // Group 13: Controller Late messages (not acknowledged)
        var group13Downlink = new DownlinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Sender = "TEST13",
            ResponseType = CpdlcDownlinkResponseType.ResponseRequired,
            Content = "DOWNLINK CONTROLLER LATE",
            Received = DateTimeOffset.UtcNow.AddMinutes(-33),
            IsControllerLate = true
        };
        var group13Dialogue = CreateDialogue("TEST13", group13Downlink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-33),
            Messages = [new(group13Dialogue, group13Downlink, mexMessageDisplayLength)]
        });

        // Group 15: Special Closed Timeout (pilot late special - Normal video)
        var group15Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Recipient = "TEST15",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.Roger,
            Content = "STANDBY",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-31),
            Closed = DateTimeOffset.UtcNow.AddMinutes(-31),
            IsPilotLate = true,
            IsTransmissionFailed = false,
            IsClosedManually = false
        };
        var group15Dialogue = CreateDialogue("TEST15", group15Uplink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-31),
            Messages = [new(group15Dialogue, group15Uplink, mexMessageDisplayLength)]
        });

        // Group 16: Overflow message (shows asterisk prefix)
        var group16Downlink = new DownlinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Sender = "TEST16",
            ResponseType = CpdlcDownlinkResponseType.ResponseRequired,
            Content = "DOWNLINK OVERFLOW MESSAGE SHOWING ASTERISK PREFIX NOT ACKNOWLEDGED VERY LONG TEXT THAT EXCEEDS MAX LENGTH",
            Received = DateTimeOffset.UtcNow.AddMinutes(-29),
            IsControllerLate = false
        };
        var group16Dialogue = CreateDialogue("TEST16", group16Downlink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-29),
            Messages = [new(group16Dialogue, group16Downlink, mexMessageDisplayLength)]
        });

        // Group 2: Regular messages (acknowledged)
        var group2Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Recipient = "TEST2",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.WilcoUnable,
            Content = "UPLINK REGULAR ACK",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-27),
            Acknowledged = DateTimeOffset.UtcNow.AddMinutes(-26),
            IsPilotLate = false,
            IsTransmissionFailed = false,
            IsClosedManually = false
        };
        var group2Downlink = new DownlinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Sender = "TEST2",
            ResponseType = CpdlcDownlinkResponseType.ResponseRequired,
            Content = "DOWNLINK REGULAR ACK",
            Received = DateTimeOffset.UtcNow.AddMinutes(-26),
            Acknowledged = DateTimeOffset.UtcNow.AddMinutes(-25),
            IsControllerLate = false
        };
        var group2Dialogue = CreateDialogue("TEST2", group2Uplink, group2Downlink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-27),
            Messages =
            [
                new(group2Dialogue, group2Uplink, mexMessageDisplayLength),
                new(group2Dialogue, group2Downlink, mexMessageDisplayLength)
            ]
        });

        // Group 4: Urgent messages (acknowledged)
        var group4Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.High,
            Recipient = "TEST4",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.WilcoUnable,
            Content = "UPLINK URGENT ACK",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-24),
            Acknowledged = DateTimeOffset.UtcNow.AddMinutes(-23),
            IsPilotLate = false,
            IsTransmissionFailed = false,
            IsClosedManually = false
        };
        var group4Downlink = new DownlinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.High,
            Sender = "TEST4",
            ResponseType = CpdlcDownlinkResponseType.ResponseRequired,
            Content = "DOWNLINK URGENT ACK",
            Received = DateTimeOffset.UtcNow.AddMinutes(-23),
            Acknowledged = DateTimeOffset.UtcNow.AddMinutes(-22),
            IsControllerLate = false
        };
        var group4Dialogue = CreateDialogue("TEST4", group4Uplink, group4Downlink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-24),
            Messages =
            [
                new(group4Dialogue, group4Uplink, mexMessageDisplayLength),
                new(group4Dialogue, group4Downlink, mexMessageDisplayLength)
            ]
        });

        // Group 6: Closed messages (acknowledged)
        var group6Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Recipient = "TEST6",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.Roger,
            Content = "UPLINK CLOSED ACK",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-21),
            Closed = DateTimeOffset.UtcNow.AddMinutes(-21),
            Acknowledged = DateTimeOffset.UtcNow.AddMinutes(-20),
            IsPilotLate = false,
            IsTransmissionFailed = false,
            IsClosedManually = false
        };
        var group6Downlink = new DownlinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Sender = "TEST6",
            ResponseType = CpdlcDownlinkResponseType.NoResponse,
            Content = "DOWNLINK CLOSED ACK",
            Received = DateTimeOffset.UtcNow.AddMinutes(-20),
            Closed = DateTimeOffset.UtcNow.AddMinutes(-20),
            Acknowledged = DateTimeOffset.UtcNow.AddMinutes(-19),
            IsControllerLate = false
        };
        var group6Dialogue = CreateDialogue("TEST6", group6Uplink, group6Downlink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-21),
            Messages =
            [
                new(group6Dialogue, group6Uplink, mexMessageDisplayLength),
                new(group6Dialogue, group6Downlink, mexMessageDisplayLength)
            ]
        });

        // Group 8: Special Closed messages (acknowledged)
        var group8Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Recipient = "TEST8",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.Roger,
            Content = "STANDBY",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-18),
            Closed = DateTimeOffset.UtcNow.AddMinutes(-18),
            Acknowledged = DateTimeOffset.UtcNow.AddMinutes(-17),
            IsPilotLate = false,
            IsTransmissionFailed = false,
            IsClosedManually = false
        };
        var group8Dialogue = CreateDialogue("TEST8", group8Uplink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-18),
            Messages = [new(group8Dialogue, group8Uplink, mexMessageDisplayLength)]
        });

        // Group 10: Failed messages (acknowledged)
        var group10Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Recipient = "TEST10",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.WilcoUnable,
            Content = "UPLINK FAILED ACK",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-16),
            Acknowledged = DateTimeOffset.UtcNow.AddMinutes(-15),
            IsPilotLate = false,
            IsTransmissionFailed = true,
            IsClosedManually = false
        };
        var group10Dialogue = CreateDialogue("TEST10", group10Uplink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-16),
            Messages = [new(group10Dialogue, group10Uplink, mexMessageDisplayLength)]
        });

        // Group 12: Pilot Late messages (acknowledged)
        var group12Uplink = new UplinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Recipient = "TEST12",
            SenderCallsign = "CONTROLLER",
            ResponseType = CpdlcUplinkResponseType.WilcoUnable,
            Content = "UPLINK PILOT LATE ACK",
            Sent = DateTimeOffset.UtcNow.AddMinutes(-14),
            Acknowledged = DateTimeOffset.UtcNow.AddMinutes(-13),
            IsPilotLate = true,
            IsTransmissionFailed = false,
            IsClosedManually = false
        };
        var group12Dialogue = CreateDialogue("TEST12", group12Uplink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-14),
            Messages = [new(group12Dialogue, group12Uplink, mexMessageDisplayLength)]
        });

        // Group 14: Controller Late messages (acknowledged)
        var group14Downlink = new DownlinkMessageDto
        {
            MessageId = messageId++,
            AlertType = AlertType.None,
            Sender = "TEST14",
            ResponseType = CpdlcDownlinkResponseType.ResponseRequired,
            Content = "DOWNLINK CONTROLLER LATE ACK",
            Received = DateTimeOffset.UtcNow.AddMinutes(-12),
            Acknowledged = DateTimeOffset.UtcNow.AddMinutes(-11),
            IsControllerLate = true
        };
        var group14Dialogue = CreateDialogue("TEST14", group14Downlink);
        testGroups.Add(new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-12),
            Messages = [new(group14Dialogue, group14Downlink, mexMessageDisplayLength)]
        });

        Dialogues = testGroups;
    }
#endif

    [ObservableProperty]
    ObservableCollection<DialogueViewModel> dialogues = [];

    [ObservableProperty]
    CurrentMessageViewModel? currentlyExtendedMessage;

    async Task LoadDialoguesAsync()
    {
        try
        {
            var dialogues = (await _dialogueStore.All(CancellationToken.None))
                .Where(d => !d.IsArchived && _jurisdictionChecker.ShouldDisplayDialogue(d))
                .ToArray();

            Dialogues.Clear();
            var dialogueViewModels = dialogues
                .OrderBy(d => d.Opened)
                .Select(d => new DialogueViewModel
                {
                    Messages = new ObservableCollection<CurrentMessageViewModel>(d.Messages.Select(m =>
                        new CurrentMessageViewModel(d, m, _configuration.MaxDisplayMessageLength))),
                    FirstMessageTime = d.Messages.OrderBy(m => m.Time).First().Time
                });

            foreach (var dialogueViewModel in dialogueViewModels)
            {
                Dialogues.Add(dialogueViewModel);
            }

            // Close the window if there are no messages left
            if (Dialogues.Count == 0)
            {
                _windowHandle.Close();
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    public void Receive(DialogueChangedNotification message)
    {
        if (_disposed)
            return;

        _guiInvoker.InvokeOnGUI(async _ =>
        {
            if (_disposed)
                return;

            await LoadDialoguesAsync();
        });
    }

    [RelayCommand]
    async Task SendStandby(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.Message is not DownlinkMessageDto downlink)
                return;

            await _mediator.Send(new SendStandbyUplinkRequest(downlink.MessageId, downlink.Sender));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task SendDeferred(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.Message is not DownlinkMessageDto downlink)
                return;

            await _mediator.Send(new SendDeferredUplinkRequest(downlink.MessageId, downlink.Sender));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task SendUnable(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.Message is not DownlinkMessageDto downlink)
                return;

            await _mediator.Send(new SendUnableUplinkRequest(downlink.MessageId, downlink.Sender));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task SendUnableDueTraffic(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.Message is not DownlinkMessageDto downlink)
                return;

            await _mediator.Send(new SendUnableUplinkRequest(downlink.MessageId, downlink.Sender, Reason: "DUE TO TRAFFIC"));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task SendUnableDueAirspace(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.Message is not DownlinkMessageDto downlink)
                return;

            await _mediator.Send(new SendUnableUplinkRequest(downlink.MessageId, downlink.Sender, Reason: "DUE TO AIRSPACE RESTRICTION"));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task AcknowledgeDownlink(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            await _mediator.Send(new AcknowledgeDownlinkMessageRequest(
                currentMessageViewModel.Dialogue.Id,
                currentMessageViewModel.Message.MessageId));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task AcknowledgeUplink(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            await _mediator.Send(new AcknowledgeUplinkMessageRequest(
                currentMessageViewModel.Dialogue.Id,
                currentMessageViewModel.Message.MessageId));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task MoveToHistory(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            await _mediator.Send(new ArchiveRequest(currentMessageViewModel.Dialogue.Id));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task ReissueMessage(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.Message is UplinkMessageDto uplink)
            {
                // TODO: await _mediator.Send(new ReissueMessageRequest(uplink.Id));
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void ToggleExtendedDisplay(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            // If this message is already extended, collapse it
            if (CurrentlyExtendedMessage == currentMessageViewModel)
            {
                currentMessageViewModel.IsExtended = false;
                CurrentlyExtendedMessage = null;
                return;
            }

            // Collapse previously extended message
            CurrentlyExtendedMessage?.IsExtended = false;

            // Extend this message
            currentMessageViewModel.IsExtended = true;
            CurrentlyExtendedMessage = currentMessageViewModel;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, $"Error extending message display for {currentMessageViewModel.Message.MessageId}");
        }
    }
}
