using System.Windows;
using System.Windows.Controls.Primitives;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Popups;

// Shows the levels that the controller can select. It is the same as the vatSys CFL menu.
public partial class LevelPopup : Popup
{
    public LevelPopup()
    {
        InitializeComponent();
    }

    public void Open(LevelPopupViewModel viewModel, UIElement placementTarget)
    {
        DataContext = viewModel;
        PlacementTarget = placementTarget;
        IsOpen = true;
    }

    void ScrollList_OptionSelected(object sender, ScrollListOption option)
    {
        if (DataContext is LevelPopupViewModel viewModel)
            viewModel.Select(option);

        IsOpen = false;
    }
}
