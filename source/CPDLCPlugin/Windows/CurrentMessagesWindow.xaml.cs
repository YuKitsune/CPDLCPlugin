using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using CPDLCPlugin.ViewModels;
using CPDLCServer.Contracts;

namespace CPDLCPlugin.Windows;

public partial class CurrentMessagesWindow : Window
{
    CurrentMessageViewModel? _selectedMessage;
    FrameworkElement? _extendedMessageAnchor;
    readonly CurrentMessagesViewModel _viewModel;

    public CurrentMessagesWindow(CurrentMessagesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        Closed += (_, _) => _viewModel.Dispose();

        // Wire up button click events
        StandbyButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.SendStandbyCommand);
        DeferredButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.SendDeferredCommand);
        UnableButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.SendUnableCommand);
        UnableTrafficButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.SendUnableDueTrafficCommand);
        UnableAirspaceButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.SendUnableDueAirspaceCommand);
        ManualAckButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.AcknowledgeUplinkCommand);
        ReissueButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.ReissueMessageCommand);
        HistoryButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.MoveToHistoryCommand);

        // Find ScrollViewer and attach scroll handler to close extended popup when scrolling
        var scrollViewer = FindVisualChild<ScrollViewer>(this);
        if (scrollViewer != null)
        {
            scrollViewer.ScrollChanged += (sender, args) =>
            {
                if (!ExtendedMessagePopup.IsOpen || args.VerticalChange == 0)
                    return;

                ExtendedMessagePopup.IsOpen = false;
                if (DataContext is CurrentMessagesViewModel { CurrentlyExtendedMessage: not null } vm)
                {
                    vm.ToggleExtendedDisplayCommand.Execute(vm.CurrentlyExtendedMessage);
                }
            };
        }
    }

    void Message_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not CurrentMessageViewModel messageViewModel ||
            DataContext is not CurrentMessagesViewModel viewModel)
            return;

        // Acknowledge downlink message as read
        if (messageViewModel.IsDownlink)
            viewModel.AcknowledgeDownlinkCommand.Execute(messageViewModel);
    }

    void Callsign_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not CurrentMessageViewModel messageViewModel ||
            DataContext is not CurrentMessagesViewModel viewModel)
            return;

        // Store selected message
        _selectedMessage = messageViewModel;

        // Acknowledge downlink message as read
        if (messageViewModel.IsDownlink)
            viewModel.AcknowledgeDownlinkCommand.Execute(messageViewModel);

        // Show/hide buttons based on message state
        UpdateButtonVisibility(messageViewModel);

        // Show popup if any buttons are visible
        if (HasVisibleButtons())
        {
            ActionPopup.IsOpen = true;
        }

        // Prevent the click from bubbling to Message_MouseLeftButtonDown
        e.Handled = true;
    }

    void Message_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not CurrentMessageViewModel messageViewModel ||
            DataContext is not CurrentMessagesViewModel viewModel)
            return;

        // Store anchor and update popup placement
        _extendedMessageAnchor = element;
        ExtendedMessagePopup.PlacementTarget = _extendedMessageAnchor;

        // Toggle extended display via ViewModel
        viewModel.ToggleExtendedDisplayCommand.Execute(messageViewModel);

        // Control popup visibility
        ExtendedMessagePopup.IsOpen = viewModel.CurrentlyExtendedMessage != null;

        e.Handled = true;
    }

    void ExtendedMessage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not CurrentMessagesViewModel viewModel)
            return;

        if (viewModel.CurrentlyExtendedMessage != null)
        {
            viewModel.ToggleExtendedDisplayCommand.Execute(viewModel.CurrentlyExtendedMessage);
            ExtendedMessagePopup.IsOpen = false;
        }

        e.Handled = true;
    }

    void ExtendedCallsign_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not CurrentMessagesViewModel viewModel ||
            viewModel.CurrentlyExtendedMessage == null)
            return;

        var messageViewModel = viewModel.CurrentlyExtendedMessage;
        _selectedMessage = messageViewModel;

        // Acknowledge downlink if needed
        if (messageViewModel.IsDownlink)
            viewModel.AcknowledgeDownlinkCommand.Execute(messageViewModel);

        // Show action popup
        UpdateButtonVisibility(messageViewModel);
        if (HasVisibleButtons())
            ActionPopup.IsOpen = true;

        e.Handled = true;
    }

    void ExtendedContent_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not CurrentMessagesViewModel viewModel ||
            viewModel.CurrentlyExtendedMessage == null)
            return;

        var messageViewModel = viewModel.CurrentlyExtendedMessage;

        // Acknowledge downlink message
        if (messageViewModel.IsDownlink)
            viewModel.AcknowledgeDownlinkCommand.Execute(messageViewModel);

        e.Handled = true;
    }

    void UpdateButtonVisibility(CurrentMessageViewModel currentMessage)
    {
        // Hide all buttons first
        StandbyButton.Visibility = Visibility.Collapsed;
        DeferredButton.Visibility = Visibility.Collapsed;
        UnableButton.Visibility = Visibility.Collapsed;
        UnableTrafficButton.Visibility = Visibility.Collapsed;
        UnableAirspaceButton.Visibility = Visibility.Collapsed;
        ManualAckButton.Visibility = Visibility.Collapsed;
        ReissueButton.Visibility = Visibility.Collapsed;
        HistoryButton.Visibility = Visibility.Collapsed;

        // Show buttons based on message type and state
        if (currentMessage is { IsDownlink: true, Message: DownlinkMessageDto { ResponseType: CpdlcDownlinkResponseType.ResponseRequired, IsClosed: false } })
        {
            StandbyButton.Visibility = Visibility.Visible;
            DeferredButton.Visibility = Visibility.Visible;
            UnableButton.Visibility = Visibility.Visible;
            UnableTrafficButton.Visibility = Visibility.Visible;
            UnableAirspaceButton.Visibility = Visibility.Visible;
        }
        else if (currentMessage is { IsDownlink: false, Message: UplinkMessageDto { IsPilotLate: true, IsClosed: false } })
        {
            ManualAckButton.Visibility = Visibility.Visible;
            HistoryButton.Visibility = Visibility.Visible;
        }
        else if (currentMessage.Message is UplinkMessageDto { IsTransmissionFailed: true, IsClosed: false })
        {
            HistoryButton.Visibility = Visibility.Visible;
            ReissueButton.Visibility = Visibility.Visible;
        }
        else
        {
            HistoryButton.Visibility = Visibility.Visible;
        }
    }

    bool HasVisibleButtons()
    {
        return StandbyButton.Visibility == Visibility.Visible ||
               DeferredButton.Visibility == Visibility.Visible ||
               UnableButton.Visibility == Visibility.Visible ||
               UnableTrafficButton.Visibility == Visibility.Visible ||
               UnableAirspaceButton.Visibility == Visibility.Visible ||
               ManualAckButton.Visibility == Visibility.Visible ||
               ReissueButton.Visibility == Visibility.Visible ||
               HistoryButton.Visibility == Visibility.Visible;
    }

    void ExecuteCommandAndClosePopup(Func<CurrentMessagesViewModel, IRelayCommand<CurrentMessageViewModel>> commandSelector)
    {
        if (_selectedMessage != null && DataContext is CurrentMessagesViewModel viewModel)
        {
            var command = commandSelector(viewModel);
            command.Execute(_selectedMessage);
        }

        ActionPopup.IsOpen = false;
    }

    void Popup_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;

        ActionPopup.IsOpen = false;
        e.Handled = true;
    }

    static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;

            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }
}
