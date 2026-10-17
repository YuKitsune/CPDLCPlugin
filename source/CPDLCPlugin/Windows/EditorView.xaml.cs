using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Messages;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Windows;

public partial class EditorView : UserControl, IRecipient<DisconnectedNotification>
{
    readonly IWindowHandle _windowHandle;

    public EditorView(EditorViewModel viewModel, IWindowHandle windowHandle)
    {
        InitializeComponent();
        DataContext = viewModel;
        _windowHandle = windowHandle;

        WeakReferenceMessenger.Default.Register(this);
        _windowHandle.Closed += (_, _) => OnClosed();
    }

    public void Receive(DisconnectedNotification message)
    {
        _windowHandle.Close();
    }

    void OnClosed()
    {
        WeakReferenceMessenger.Default.Unregister<DisconnectedNotification>(this);

        if (DataContext is EditorViewModel viewModel)
        {
            viewModel.Dispose();
        }
    }

    void MessageClassElement_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageTemplateViewModel template ||
            DataContext is not EditorViewModel viewModel)
            return;

        viewModel.AddMessageElementCommand.Execute(template);
        e.Handled = true;
    }

    void LineNumberButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageElementViewModel messageElement ||
            DataContext is not EditorViewModel viewModel)
            return;

        switch (e.ChangedButton)
        {
            case MouseButton.Left:
                viewModel.ToggleMessageElementSelectionCommand.Execute(messageElement);
                break;

            case MouseButton.Right:
                viewModel.InsertMessageElementAboveCommand.Execute(messageElement);
                break;

            case MouseButton.Middle:
                viewModel.ClearMessageElementCommand.Execute(messageElement);
                break;
        }

        // Don't mark as handled so button gets visual feedback
    }

    void TemplatePartButton_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageTemplateElementComponentViewModel templatePart)
            return;

        switch (e.ChangedButton)
        {
            // Left click: Start editing
            // TODO: Type-specific popup
            case MouseButton.Left:
                templatePart.IsEditing = true;
                break;

            // Middle click: Clear the value
            case MouseButton.Middle:
                templatePart.Value = null;
                e.Handled = true;
                break;

            // Right-click for manual text entry
            case MouseButton.Right:
                templatePart.IsEditing = true;
                break;
        }
    }

    void TemplatePartTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        // Only focus when becoming visible
        if (e.NewValue is true)
        {
            textBox.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    textBox.Focus();
                    Keyboard.Focus(textBox);
                    textBox.SelectAll();
                }),
                System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    void TemplatePartTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: UplinkMessageTemplateElementComponentViewModel templatePart })
            return;

        switch (e.Key)
        {
            // Enter: Save and exit editing
            case Key.Enter:
                templatePart.IsEditing = false;
                e.Handled = true;
                break;
            // Escape: Cancel editing (revert to original value would require storing it)
            case Key.Escape:
                templatePart.IsEditing = false;
                e.Handled = true;
                break;
        }
    }

    void TemplatePartTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: UplinkMessageTemplateElementComponentViewModel templatePart })
            return;

        templatePart.IsEditing = false;
    }

    void DownlinkMessage_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: DownlinkMessageViewModel clickedMessage } ||
            DataContext is not EditorViewModel viewModel)
            return;

        // Toggle selection: if clicking on the already selected message, deselect it
        viewModel.SelectedDownlinkMessage = viewModel.SelectedDownlinkMessage == clickedMessage
            ? null
            : clickedMessage;

        e.Handled = true;
    }

    void DownlinkMessage_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: DownlinkMessageViewModel downlinkMessage } element ||
            DataContext is not EditorViewModel viewModel)
            return;

        ExtendedDownlinkMessagePopup.PlacementTarget = element;
        viewModel.CurrentlyExtendedDownlinkMessage = downlinkMessage;
        ExtendedDownlinkMessagePopup.IsOpen = true;

        e.Handled = true;
    }

    void ExtendedDownlinkMessagePopup_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Close the popup on any click (left or right)
        ExtendedDownlinkMessagePopup.IsOpen = false;
        e.Handled = true;
    }

    void ExtendedDownlinkMessagePopup_Closed(object? sender, EventArgs e)
    {
        // Clear the extended message in the ViewModel when the popup closes
        if (DataContext is not EditorViewModel viewModel)
            return;

        viewModel.CurrentlyExtendedDownlinkMessage = null;
    }
}
