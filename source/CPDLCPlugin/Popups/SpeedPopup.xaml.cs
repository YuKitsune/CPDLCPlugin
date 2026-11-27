using System.Windows;
using System.Windows.Controls.Primitives;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Popups;

// Shows the speeds that the controller can select, in knots or in Mach
public partial class SpeedPopup : Popup
{
    public SpeedPopup()
    {
        InitializeComponent();
    }

    public void Open(SpeedPopupViewModel viewModel, UIElement placementTarget)
    {
        DataContext = viewModel;
        PlacementTarget = placementTarget;
        IsOpen = true;
    }

    void ScrollList_OptionSelected(object sender, ScrollListOption option)
    {
        if (DataContext is SpeedPopupViewModel viewModel)
            viewModel.Select(option);

        IsOpen = false;
    }
}
