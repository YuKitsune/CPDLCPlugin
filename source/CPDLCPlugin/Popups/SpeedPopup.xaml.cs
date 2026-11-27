using System.Windows.Controls;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Popups;

// Shows the speeds that the controller can select, in knots or in Mach
public partial class SpeedPopup : UserControl
{
    // Occurs when the controller selects a speed
    public event EventHandler? ValueSelected;

    public SpeedPopup()
    {
        InitializeComponent();
    }

    void ScrollList_OptionSelected(object sender, ScrollListOption option)
    {
        if (DataContext is SpeedPopupViewModel viewModel)
            viewModel.Select(option);

        ValueSelected?.Invoke(this, EventArgs.Empty);
    }
}
