using System.Windows.Controls;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Popups;

// Shows the levels that the controller can select. It is the same as the vatSys CFL menu.
public partial class LevelPopup : UserControl
{
    // Occurs when the controller selects a level
    public event EventHandler? ValueSelected;

    public LevelPopup()
    {
        InitializeComponent();
    }

    void ScrollList_OptionSelected(object sender, ScrollListOption option)
    {
        if (DataContext is LevelPopupViewModel viewModel)
            viewModel.Select(option);

        ValueSelected?.Invoke(this, EventArgs.Empty);
    }
}
