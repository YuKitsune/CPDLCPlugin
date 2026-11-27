using System.Windows;
using System.Windows.Controls.Primitives;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Popups;

// Shows the times that the controller can select. It is the same as the vatSys time popup.
// TODO: This is a stub. Add the values, and set the value of the parameter when the controller selects one.
public partial class TimePopup : Popup
{
    public TimePopup()
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
