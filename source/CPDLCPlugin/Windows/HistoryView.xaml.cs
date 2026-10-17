using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Windows;

public partial class HistoryView : UserControl
{
    FrameworkElement? _extendedMessageAnchor;
    readonly HistoryViewModel _viewModel;

    public HistoryView(HistoryViewModel viewModel, IWindowHandle windowHandle)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        windowHandle.Closed += (_, _) => _viewModel.Dispose();
        windowHandle.Moved += (_, _) => CollapseExtendedMessage();
        SizeChanged += (_, _) => CollapseExtendedMessage();
    }

    void CallsignButton_Click(object sender, RoutedEventArgs e)
    {
        CallsignButton.Visibility = Visibility.Collapsed;
        CallsignTextBox.Visibility = Visibility.Visible;
        CallsignTextBox.Focus();
        CallsignTextBox.SelectAll();
    }

    void CallsignTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        _viewModel.LoadMessagesCommand.Execute(null);
        CallsignTextBox.Visibility = Visibility.Collapsed;
        CallsignButton.Visibility = Visibility.Visible;

        e.Handled = true;
    }

    void Message_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: HistoryMessageViewModel messageViewModel } element ||
            DataContext is not HistoryViewModel viewModel)
            return;

        _extendedMessageAnchor = element;
        ExtendedMessagePopup.PlacementTarget = _extendedMessageAnchor;

        viewModel.ToggleExtendedDisplayCommand.Execute(messageViewModel);
        ExtendedMessagePopup.IsOpen = viewModel.CurrentlyExtendedMessage != null;

        e.Handled = true;
    }

    void ExtendedMessage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        CollapseExtendedMessage();
        e.Handled = true;
    }

    void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange == 0)
            return;

        CollapseExtendedMessage();
    }

    void CollapseExtendedMessage()
    {
        if (DataContext is not HistoryViewModel viewModel ||
            viewModel.CurrentlyExtendedMessage == null)
            return;

        viewModel.ToggleExtendedDisplayCommand.Execute(viewModel.CurrentlyExtendedMessage);
        ExtendedMessagePopup.IsOpen = false;
    }
}
