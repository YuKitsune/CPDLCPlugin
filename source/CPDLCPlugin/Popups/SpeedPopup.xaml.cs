using System.Windows;
using System.Windows.Controls.Primitives;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Popups;

// Shows the speeds that the controller can select.
// TODO: This is a stub. Add the values, and set the value of the parameter when the controller selects one.
public partial class SpeedPopup : Popup
{
    public SpeedPopup()
    {
        InitializeComponent();
    }

    public void Open(UplinkMessageTemplateElementComponentViewModel templatePart, UIElement placementTarget)
    {
        DataContext = templatePart;
        PlacementTarget = placementTarget;
        IsOpen = true;
    }
}
