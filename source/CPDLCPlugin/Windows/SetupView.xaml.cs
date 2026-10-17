using System.Windows.Controls;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Windows;

public partial class SetupView : UserControl
{
    public SetupView(SetupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
