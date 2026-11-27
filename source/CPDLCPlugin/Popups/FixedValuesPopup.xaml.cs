using System.Windows;
using System.Windows.Controls;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Popups;

// Shows the fixed values of a parameter. The controller can select only one of these values.
public partial class FixedValuesPopup : UserControl
{
    // Occurs when the controller selects a value
    public event EventHandler? ValueSelected;

    public FixedValuesPopup()
    {
        InitializeComponent();
    }

    void ValueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: string value } &&
            DataContext is UplinkMessageTemplateElementComponentViewModel templatePart)
        {
            templatePart.Value = value;
        }

        ValueSelected?.Invoke(this, EventArgs.Empty);
    }
}
