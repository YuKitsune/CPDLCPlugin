using System.Windows;
using System.Windows.Controls.Primitives;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Popups;

// Shows the fixed values of a parameter. The controller can select only one of these values.
public partial class FixedValuesPopup : Popup
{
    public FixedValuesPopup()
    {
        InitializeComponent();
    }

    public void Open(UplinkMessageTemplateElementComponentViewModel templatePart, UIElement placementTarget)
    {
        DataContext = templatePart;
        PlacementTarget = placementTarget;
        IsOpen = true;
    }

    void ValueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: string value } &&
            DataContext is UplinkMessageTemplateElementComponentViewModel templatePart)
        {
            templatePart.Value = value;
        }

        IsOpen = false;
    }
}
